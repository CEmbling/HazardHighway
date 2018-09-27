using ASPNETCore_SignalR_Angular_TypeScript.App;
using ASPNETCore_SignalR_Angular_TypeScript.Hubs;
using ASPNETCore_SignalR_Angular_TypeScript.Models;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace ASPNETCore_SignalR_Angular_TypeScript
{
    /// <summary>
    /// "Hazard Highway" Game Concept 
    /// Copyright: Charles Embling 9/26/2018
    /// Technology Credits: 
    /// Nemi Chand - Getting Started With SignalR Using ASP.NET Core And Angular
    /// https://www.c-sharpcorner.com/article/getting-started-with-signalr-using-aspnet-co-using-angular-5/
    /// </summary>
	public class Game : IGame
	{
        #region private members

        private readonly SemaphoreSlim _GameStateLock = new SemaphoreSlim(1, 1);
		private readonly SemaphoreSlim _VehiclePositionsLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _VehicleActionsLock = new SemaphoreSlim(1, 1);

        private readonly ConcurrentDictionary<string, Vehicle> _vehicles = new ConcurrentDictionary<string, Vehicle>();
        private readonly ConcurrentDictionary<string, QueuedVehicleAction> _queuedVehicleActions = new ConcurrentDictionary<string, QueuedVehicleAction>();
        private readonly List<string> _temporaryDrivingStatuses = new List<string> {
            DrivingStatus.TurningLeft.ToString(), DrivingStatus.TurningRight.ToString(),
            DrivingStatus.Accelerating.ToString(), DrivingStatus.Braking.ToString(),
            DrivingStatus.AutoBraking.ToString(), DrivingStatus.Resuming.ToString() };
        private readonly Subject<Vehicle> _subject = new Subject<Vehicle>();
        private const int UPDATE_INTERVAL_MILLISECONDS = 250;
		private readonly TimeSpan _updateInterval = TimeSpan.FromMilliseconds(UPDATE_INTERVAL_MILLISECONDS);
        private readonly Random _updateOrNotRandom = new Random();

		private Timer _timer;
		private volatile bool _updatingVehiclePositions;
        private volatile bool _updatingVehicleActions;
        private volatile GameState _GameState;

        #region constants

        private const bool allowSubjectNextInsideGameLoop = true;
        private const string PLAYER1 = "Player 1";
        private const int FEETPERMILE = 5280;
        private const double FEETPERCELL = 1;
        private const int MILLISECONDSPERHOUR = 3600000;
        private const int VEHICLECELLLENGTH = 5;
        private const int RADARINDICATORRANGE = 10;
        // accelerating
        private const int VEHICLE_MPH_ACCELERATION_RATE = 5;
        // braking
        private const int RADARBRAKERANGE = 8;
        private const int POINTSPERVEHICLESAVED = 1000;
        private const int DANGEROUSESPEEDDIFF = 50;
        private const int CAUTIONSPEEDDIFF = 35;
        private const int VEHICLE_MAX_MPH_BRAKE_RATE = 20;
        private const int VEHICLE_CAUTIOUS_MPH_BRAKE_RATE = 10;
        private const int VEHICLE_GRADUAL_MPH_BRAKE_RATE = 5;

        #endregion

        #region terminology

        private readonly List<string> _crashVerbs = new List<string>()
        {
            "impacted","smashed","demolished","wrecked","rammed","pealed","cracked","burst",
            "dented","blasted","clocked","speared","t-boned","sliced","split","side-swiped",
            "ended","ruined","knocked","struck","thumped","whacked","slammed","bumped",
            "hammered","pounded","pummeled","discharged","blew-up","detonated"
        };
        private readonly List<string> _tamedVerbs = new List<string>()
        {
            "owned","wrangled","slapped","disciplined", "tamed"
        };
        private readonly List<string> _safeAdjectives = new List<string>()
        {
            "studious","attentive","disciplined", "alert", "awake", "responsible"
        };
        private readonly List<string> _unsafeAdjectives = new List<string>()
        {
            "careless","sleepy","hazy", "distracted", "inebriated", "foggy", "moody", "dangerous", "wreckless", "mean", "buzzed"
        };

        #endregion


        #endregion

        #region constructors

        public Game(IHubContext<GameHub> hub)
		{
			Hub = hub;
			LoadDefaultVehicles();
		}

        #endregion

        #region private methods

        private void LoadDefaultVehicles()
        {
            _vehicles.Clear();

            var vehicles = new List<Vehicle>
            {
                // introduce hazards down the highway
                new Vehicle { Name = "Gauker 1",            Mph = 0, X = 3170, Y = 3, AdaptiveCruiseOn = true },
                new Vehicle { Name = "Disabled Vehicle",    Mph = 0, X = 3200, Y = 5, AdaptiveCruiseOn = true },
                new Vehicle { Name = "Gauker 2",            Mph = 0, X = 3170, Y = 7, AdaptiveCruiseOn = true },

                // left lane
                new Vehicle { Name = "Toyota Prius",        Mph = 30, X = 5,  Y = 3, AdaptiveCruiseOn = false },
                new Vehicle { Name = "Toyota Camry",        Mph = 30, X = 50,  Y = 3, AdaptiveCruiseOn = false },
                new Vehicle { Name = "Lamborgini",          Mph = 30, X = 100, Y = 3, AdaptiveCruiseOn = false },
                new Vehicle { Name = "Ferrari 430",         Mph = 30, X = 250, Y = 3, AdaptiveCruiseOn = false },               

                // middle lane
                new Vehicle { Name = PLAYER1,               Mph = 30, X = 0,    Y = 5, AdaptiveCruiseOn = true },
                new Vehicle { Name = "Chevy Traverse",      Mph = 30, X = 21,  Y = 5, AdaptiveCruiseOn = false },
                new Vehicle { Name = "Ford Explorer",       Mph = 30, X = 40,  Y = 5, AdaptiveCruiseOn = false },
                new Vehicle { Name = "Toyota Highlander",   Mph = 30, X = 160, Y = 5, AdaptiveCruiseOn = false },

                // right lane
                new Vehicle { Name = "Ford Escape",         Mph = 30, X = 7,  Y = 7, AdaptiveCruiseOn = false },
                new Vehicle { Name = "Chevy Malibu",        Mph = 30, X = 30,  Y = 7, AdaptiveCruiseOn = false },
                new Vehicle { Name = "Subaru Forester",     Mph = 30, X = 50,  Y = 7, AdaptiveCruiseOn = false },
                new Vehicle { Name = "Cal's Pigeon",        Mph = 30, X = 70, Y = 7, AdaptiveCruiseOn = false },
            };

            vehicles.ForEach(v => v.DrivingAdjective = v.AdaptiveCruiseOn? GetRandomTerm(TermList.Safe) : GetRandomTerm(TermList.Unsafe));
            vehicles.ForEach(v => v.DrivingStatus = v.AdaptiveCruiseOn ? DrivingStatus.Cruising.ToString() : DrivingStatus.Driving.ToString());
            vehicles.ForEach(v => _vehicles.TryAdd(v.Name, v));
        }

        // THIS IS THE GAME LOOP CYCLE
        private async void UpdateVehicles(object state)
        {
            // This function must be re-entrant as it's running as a timer interval handler
            Stopwatch s = new Stopwatch();
            s.Start();
            await ClearTemporaryVehicleDrivingStatuses();
            bool didUserInitiateAction = _queuedVehicleActions.Any();
            await UpdateVehiclesPositions();                    // physics changes
            if (didUserInitiateAction)
            {
                await UpdateVehicleActions();                   // user-initiated actions that we queued up for game loop
                //await CheckVehiclesForCollisions();             // call this again after (lane changes)
            }
            await CheckVehiclesForCollisions();            
            await CheckVehiclesForApproachingVehicles();
            await CheckVehiclesForBlindSpots();
            await BroadcastAllVehiclesSubjects();
            s.Stop();
            await BroadcastGameLoopBenchmark(s.ElapsedMilliseconds);
        }

        private async Task UpdateVehiclesPositions()
        {
            // This function must be re-entrant as it's running as a timer interval handler
            await _VehiclePositionsLock.WaitAsync();
            try
            {
                if (!_updatingVehiclePositions)
                {
                    _updatingVehiclePositions = true;

                    foreach (var vehicle in _vehicles.Values
                        .Where(v => v.DrivingStatus != DrivingStatus.Crashed.ToString()))
                    {
                        if (TryUpdateVehiclePosition(vehicle))
                        {
                            if(allowSubjectNextInsideGameLoop) _subject.OnNext(vehicle);
                        }
                    }

                    _updatingVehiclePositions = false;
                }
            }
            finally
            {
                _VehiclePositionsLock.Release();
            }
        }
        private async Task ClearTemporaryVehicleDrivingStatuses()
        {
            // This function must be re-entrant as it's running as a timer interval handler
            await _VehiclePositionsLock.WaitAsync();
            try
            {
                if (!_updatingVehiclePositions)
                {
                    _updatingVehiclePositions = true;

                    foreach (var vehicle in _vehicles.Values)
                    {
                        if (_temporaryDrivingStatuses.Contains(vehicle.DrivingStatus) && vehicle.Mph > 0)
                        {
                            TryUpdateVehicleDrivingStatus(vehicle, vehicle.AdaptiveCruiseOn? DrivingStatus.Cruising : DrivingStatus.Driving);
                        }
                        else if(_temporaryDrivingStatuses.Contains(vehicle.DrivingStatus) && vehicle.Mph == 0)
                        {
                            TryUpdateVehicleDrivingStatus(vehicle, DrivingStatus.Stopped);
                        }

                        if(allowSubjectNextInsideGameLoop) _subject.OnNext(vehicle);
                    }

                    _updatingVehiclePositions = false;
                }
            }
            finally
            {
                _VehiclePositionsLock.Release();
            }
        }
        private async Task CheckVehiclesForCollisions()
        {
            // This function must be re-entrant as it's running as a timer interval handler
            await _VehiclePositionsLock.WaitAsync();
            try
            {
                if (!_updatingVehiclePositions)
                {
                    _updatingVehiclePositions = true;

                    foreach (var vehicle in _vehicles.Values)
                    {
                        await CheckVehicleForCollisions(vehicle);

                        if(allowSubjectNextInsideGameLoop) _subject.OnNext(vehicle);
                    }

                    _updatingVehiclePositions = false;
                }
            }
            finally
            {
                _VehiclePositionsLock.Release();
            }
        }
        private async Task CheckVehiclesForApproachingVehicles()
        {
            // This function must be re-entrant as it's running as a timer interval handler
            await _VehiclePositionsLock.WaitAsync();
            try
            {
                if (!_updatingVehiclePositions)
                {
                    _updatingVehiclePositions = true;

                    foreach (var vehicle in _vehicles.Values
                        .Where(v => v.AdaptiveCruiseOn
                            && v.DrivingStatus != DrivingStatus.Crashed.ToString()))
                    {
                        await CheckVehicleForApproachingVehicles(vehicle);

                        if(allowSubjectNextInsideGameLoop) _subject.OnNext(vehicle);
                    }

                    _updatingVehiclePositions = false;
                }
            }
            finally
            {
                _VehiclePositionsLock.Release();
            }
        }
        private async Task CheckVehiclesForBlindSpots()
        {
            // This function must be re-entrant as it's running as a timer interval handler
            await _VehiclePositionsLock.WaitAsync();
            try
            {
                if (!_updatingVehiclePositions)
                {
                    _updatingVehiclePositions = true;

                    var tasks = _vehicles
                                    .Where(v => v.Value.DrivingStatus != DrivingStatus.Crashed.ToString())
                                    .Select(v => CheckVehicleForBlindSpots(v.Value));
                    await Task.WhenAll(tasks);

                    _updatingVehiclePositions = false;
                }
            }
            finally
            {
                _VehiclePositionsLock.Release();
            }
        }
        private async Task UpdateVehicleActions()
        {
            // This function must be re-entrant as it's running as a timer interval handler
            await _VehicleActionsLock.WaitAsync();
            try
            {
                if (!_updatingVehicleActions)
                {
                    _updatingVehicleActions = true;

                    Stopwatch s = new Stopwatch();
                    s.Start();

                    var tasks = _queuedVehicleActions
                                    .Select(va => UpdateVehicleAction(va.Value));
                    await Task.WhenAll(tasks);

                    s.Stop();
                    var milliseconds = s.ElapsedMilliseconds;
                    // very important!~ clear all of these
                    _queuedVehicleActions.Clear();

                    _updatingVehicleActions = false;
                }
            }
            finally
            {
                //_updatingVehicleActions = false; <-- here?
                _VehicleActionsLock.Release();
            }
        }
        private async Task AddVehicleAction()
        {

        }

        #region task functions

        private bool TryUpdateVehiclePosition(Vehicle vehicle)
        {
            if (vehicle.DrivingStatus == DrivingStatus.Crashed.ToString())
            {
                return false;
            }
            // Randomly choose whether to udpate this Vehicle or not
            //var r = _updateOrNotRandom.NextDouble();
            //if (r > 0.1)
            //{
            //	return false;
            //}

            //// Update the Vehicle position by a random factor of the range percent
            //var random = new Random((int)Math.Floor(Vehicle.X));
            //var percentChange = random.NextDouble() * _rangePercent;
            //var pos = random.NextDouble() > 0.51;
            //var change = Math.Round(Vehicle.Price * (decimal)percentChange, 2);
            //change = pos ? change : -change;

            // 158,400 = 30 mph * 5280 ftpm
            // 184,000 = 35 mph * 5280 ftpm
            var ftphr = vehicle.Mph * FEETPERMILE;

            // .044 = feet per millisecond  (30 mph)
            // .051 = feet per millisecond  (35 mph)
            Double ftpms = Convert.ToDouble(Convert.ToDouble(ftphr) / Convert.ToDouble(MILLISECONDSPERHOUR));
            
            // 11 = feetTraveledPerInterval  (30 mph)
            // 12.75 = feetTraveledPerInterval  (35 mph)
            Double feetTraveledPerInterval = Convert.ToDouble(ftpms * _updateInterval.TotalMilliseconds);

            // 0        = cellsTravelledPerInterval  (00 mph)
            // 6.375    = cellsTravelledPerInterval  (05 mph)
            // 5.5      = cellsTravelledPerInterval  (10 mph)
            // 6.375    = cellsTravelledPerInterval  (15 mph)
            // 5.5      = cellsTravelledPerInterval  (20 mph)
            // 6.375    = cellsTravelledPerInterval  (25 mph)
            // 5.5      = cellsTravelledPerInterval  (30 mph)
            // 6.375    = cellsTravelledPerInterval  (35 mph)
            // 5.5      = cellsTravelledPerInterval  (40 mph)
            // 6.375    = cellsTravelledPerInterval  (45 mph)
            // 5.5      = cellsTravelledPerInterval  (50 mph)
            // 6.375    = cellsTravelledPerInterval  (55 mph)
            // 5.5      = cellsTravelledPerInterval  (60 mph)
            // 6.375    = cellsTravelledPerInterval  (75 mph)
            int cellsTravelledPerInterval = Convert.ToInt32(feetTraveledPerInterval / FEETPERCELL);
            vehicle.X += cellsTravelledPerInterval;

            return true;
        }
        private bool TryUpdateVehicleAction(Vehicle vehicle, QueuedVehicleAction action)
        {
            if (vehicle.DrivingStatus == DrivingStatus.Crashed.ToString())
            {
                return false;
            }
            switch (action.VehicleAction)
            {
                case VehicleAction.IncreaseSpeed:
                    if (TryUpdateVehicleMph(vehicle, vehicle.Mph + 5))
                    {
                        vehicle.DrivingStatus = DrivingStatus.Accelerating.ToString();
                        if (vehicle.AdaptiveCruiseOn)
                        {
                            // keep in AdaptiveCruiseMph insync w/ human accelerating
                            vehicle.AdaptiveCruiseMph = vehicle.Mph;
                        }
                    }
                    break;
                case VehicleAction.DecreaseSpeed:
                    if (TryUpdateVehicleMph(vehicle, vehicle.Mph - 5))
                    {
                        if (vehicle.Mph == 0)
                        {
                            TryUpdateVehicleDrivingStatus(vehicle, DrivingStatus.Stopped);
                        }
                        else
                        {
                            TryUpdateVehicleDrivingStatus(vehicle, DrivingStatus.Braking);
                        }
                        
                        if (vehicle.AdaptiveCruiseOn)
                        {
                            // keep in AdaptiveCruiseMph insync w/ human deaccelaration
                            vehicle.AdaptiveCruiseMph = vehicle.Mph;
                        }
                    }
                    break;
                case VehicleAction.TurnLeft:
                    if (vehicle.Mph > 0 && vehicle.Y >= 3)
                    {
                        vehicle.Y -= 1;
                        TryUpdateVehicleDrivingStatus(vehicle, DrivingStatus.TurningLeft);
                        CheckVehicleForUnsafeVehiclesToSave(vehicle);
                    }
                    break;
                case VehicleAction.TurnRight:
                    if (vehicle.Mph > 0 && vehicle.Y < 8)
                    {
                        vehicle.Y += 1;
                        TryUpdateVehicleDrivingStatus(vehicle, DrivingStatus.TurningRight);
                        CheckVehicleForUnsafeVehiclesToSave(vehicle);
                    }
                    break;
                case VehicleAction.ToggleAdaptiveCruise:
                    vehicle.AdaptiveCruiseOn = !vehicle.AdaptiveCruiseOn;
                    if (vehicle.AdaptiveCruiseOn)
                    {
                        TryUpdateVehicleDrivingStatus(vehicle, DrivingStatus.Cruising);
                        // keep AdaptiveCruiseMph insync w/ human initiated events, like enabled AC
                        vehicle.AdaptiveCruiseMph = vehicle.Mph;
                    }
                    else
                    {
                        TryUpdateVehicleDrivingStatus(vehicle, DrivingStatus.Driving);
                        vehicle.AdaptiveCruiseMph = 0;
                    }
                    vehicle.Points += vehicle.AdaptiveCruiseOn ? 10 : 0;
                    break;
            }
            return true;
        }
        private bool TryUpdateVehicleStatus(Vehicle vehicle, string status)
        {
            if (vehicle.DrivingStatus == DrivingStatus.Crashed.ToString())
            {
                return false;
            }
            vehicle.Status = status;
            return true;
        }
        private bool TryUpdateVehicleDrivingStatus(Vehicle vehicle, DrivingStatus drivingStatus)
        {
            if (vehicle.DrivingStatus == DrivingStatus.Crashed.ToString())
            {
                return false;
            }
            vehicle.DrivingStatus = drivingStatus.ToString();
            return true;
        }
        private bool TryUpdateVehicleMph(Vehicle vehicle, int mph)
        {
            if (mph >= 0)
            {
                vehicle.Mph = mph;
            }
            return true;
        }
        private async Task BroadcastAllVehiclesSubjects()
        {
            foreach (var Vehicle in _vehicles.Values)
            {
                _subject.OnNext(Vehicle);
            }
            // signal to ui to re-render highway
            _subject.OnNext(new Vehicle { Name = "RENDER_HWY" });
        }

        #endregion


        #region User Initiated private methods

        private async Task AddVehicleActionToQueue(VehicleAction action)
        {
            await _VehicleActionsLock.WaitAsync();
            try
            {
                if (!_updatingVehicleActions)
                {
                    _updatingVehicleActions = true;

                    var playerVehicle = _vehicles.Where(v => v.Key == PLAYER1).Select(x => x.Value).Single();
                    TryAddVehicleActionToQueue(playerVehicle.Name, action);

                    _updatingVehicleActions = false;
                }
            }
            finally
            {
                _VehicleActionsLock.Release();
            }
        }

        private bool TryAddVehicleActionToQueue(string vehicleKey, VehicleAction vehicleAction)
        {
            return _queuedVehicleActions.TryAdd(vehicleKey, QueuedVehicleAction.Factory.Create(vehicleKey, vehicleAction));
        }

        #endregion


        #region private vehicle checks (automatically called per game loop cycle)

        private async Task UpdateVehicleAction(QueuedVehicleAction queuedVehicleAction)
        {
            if (queuedVehicleAction.Handled)
            {
                // this queuedVehicleAction has already been handled
                return;
            }
            // This function must be re-entrant as it's running as a timer interval handler
            await _VehiclePositionsLock.WaitAsync();
            try
            {
                if (!_updatingVehiclePositions)
                {
                    _updatingVehiclePositions = true;

                    foreach (var vehicle in _vehicles
                        .Where(v => v.Key == queuedVehicleAction.Name)
                        .Select(x => x.Value))
                    {
                        if(TryUpdateVehicleAction(vehicle, queuedVehicleAction))
                        {
                            if(allowSubjectNextInsideGameLoop) _subject.OnNext(vehicle);
                            if (queuedVehicleAction.VehicleAction == VehicleAction.TurnLeft || queuedVehicleAction.VehicleAction == VehicleAction.TurnRight)
                            {
                                await CheckVehicleForUnsafeVehiclesToSave(vehicle);
                            }
                        }
                        
                    }
                    // very important!! don't forget to mark this as handled
                    queuedVehicleAction.Handled = true;
                    _updatingVehiclePositions = false;
                }
            }
            finally
            {
                _VehiclePositionsLock.Release();
            }
        }
        private async Task UpdateVehicleDrivingStatus(Vehicle vehicle, DrivingStatus drivingStatus)
        {
            // This function must be re-entrant as it's running as a timer interval handler
            await _VehiclePositionsLock.WaitAsync();
            try
            {
                if (!_updatingVehiclePositions)
                {
                    _updatingVehiclePositions = true;

                    foreach (var Vehicle in _vehicles.Where(v => v.Key == vehicle.Name).Select(x => x.Value))
                    {
                        TryUpdateVehicleDrivingStatus(Vehicle, drivingStatus);

                        //if(allowSubjectNextInsideGameLoop) _subject.OnNext(vehicle);
                    }

                    _updatingVehiclePositions = false;
                }
            }
            finally
            {
                _VehiclePositionsLock.Release();
            }
        }
        private async Task UpdateVehicleStatus(string vehicleKey, string status)
        {
            // This function must be re-entrant as it's running as a timer interval handler
            await _VehiclePositionsLock.WaitAsync();
            try
            {
                if (!_updatingVehiclePositions)
                {
                    _updatingVehiclePositions = true;

                    foreach (var vehicle in _vehicles.Where(v => v.Key == vehicleKey).Select(x => x.Value))
                    {
                        TryUpdateVehicleStatus(vehicle, status);

                        if(allowSubjectNextInsideGameLoop) _subject.OnNext(vehicle);
                    }

                    _updatingVehiclePositions = false;
                }
            }
            finally
            {
                _VehiclePositionsLock.Release();
            }
        }
        private async Task CheckVehicleForCollisions(Vehicle vehicle)
        {
            // any other vehicals within collision range?
            var collidedWithList = this._vehicles
                .Where(otherVehicle => otherVehicle.Key != vehicle.Name
                && vehicle.DrivingStatus != DrivingStatus.Crashed.ToString()    // ignore cars that already crashed
                && otherVehicle.Value.Y == vehicle.Y                            // in same lane
                && otherVehicle.Value.X >= (vehicle.X - VEHICLECELLLENGTH)      // within collision range
                && otherVehicle.Value.X <= (vehicle.X + VEHICLECELLLENGTH)
                ).ToList();

            bool vehicleCrashed = collidedWithList.Any();
            if (vehicleCrashed)
            {
                var crashedIntoVehicle = collidedWithList.First().Value;
                var isAtFault = vehicle.X < crashedIntoVehicle.X;
                
                var crashAdverb = GetRandomTerm(TermList.Crash);
                if (isAtFault)
                {
                    vehicle.Status = $"{crashAdverb} {crashedIntoVehicle.Name}";
                    crashedIntoVehicle.Status = $"{crashAdverb} by {vehicle.Name}";
                }
                else
                {
                    vehicle.Status = $"{crashAdverb} by {crashedIntoVehicle.Name}";
                    crashedIntoVehicle.Status = $"{crashAdverb} {vehicle.Name}";
                }
                if(crashedIntoVehicle.DrivingStatus != DrivingStatus.Crashed.ToString())
                {
                    TryUpdateVehicleStatus(crashedIntoVehicle, crashedIntoVehicle.Status);
                    if (TryUpdateVehicleDrivingStatus(crashedIntoVehicle, DrivingStatus.Crashed))
                    {
                        TryUpdateVehicleMph(crashedIntoVehicle, 0);
                        _subject.OnNext(crashedIntoVehicle);
                    }
                }

                TryUpdateVehicleStatus(vehicle, vehicle.Status);
                if (TryUpdateVehicleDrivingStatus(vehicle, DrivingStatus.Crashed))
                {
                    TryUpdateVehicleMph(vehicle, 0);
                    if(allowSubjectNextInsideGameLoop) _subject.OnNext(vehicle);
                }

                if (vehicle.Name == PLAYER1 || crashedIntoVehicle.Name == PLAYER1)
                {
                    // game over; player1 crashed!
                    await this.CloseGame();
                }
            }
        }
        private async Task CheckVehicleForApproachingVehicles(Vehicle vehicle)
        {
            if (!vehicle.AdaptiveCruiseOn)
                return;

            // check for vehicals within range in front of vehicle
            var carsInRadarRange = await this._vehicles
                .Where(otherVehicle => otherVehicle.Key != vehicle.Name
                    && otherVehicle.Value.Y == vehicle.Y                                                // in same lane
                    && otherVehicle.Value.RearBumper <= vehicle.FrontBumper + RADARINDICATORRANGE       // within radar range
                    && otherVehicle.Value.RearBumper > vehicle.FrontBumper                              // within radar range
                ).ToAsyncEnumerable().ToList();

            vehicle.AdaptiveCruiseFrontRadarIndicator = carsInRadarRange.Any();

            if (vehicle.AdaptiveCruiseFrontRadarIndicator)
            {
                // if vehicle is traveling faster than oncoming car, decrease speed until preferred car length is achieved, then match speed
                var nearestCar = carsInRadarRange.OrderBy(x => x.Value.X).First().Value;
                var speedDifference = nearestCar.Mph < vehicle.Mph;
                if (speedDifference)
                {
                    // we may need to decrease speed...check distance first
                    // calculate breaking force
                    var brakeForce = CalculateVehicleBrakingForceToMaintainLeadPreference(vehicle, nearestCar);
                    if (brakeForce > 0)
                    {
                        // breaking is necessary
                        if (vehicle.AdaptiveCruiseMph == 0)
                        {
                            // store original speed (so we can return to it when the slow car moves)
                            vehicle.AdaptiveCruiseMph = vehicle.Mph;
                        }

                        vehicle.Mph = vehicle.Mph - brakeForce >= 0? vehicle.Mph - brakeForce : 0;
                        if(vehicle.Mph == 0)
                        {
                            vehicle.Status = $"stopped for {nearestCar.Name}";
                            vehicle.DrivingStatus = DrivingStatus.Stopped.ToString();
                            _vehicles[vehicle.Name] = vehicle;
                            if (allowSubjectNextInsideGameLoop) _subject.OnNext(vehicle);
                        }
                        else
                        {
                            vehicle.Status = $"autobraking for {nearestCar.Name}";
                            vehicle.DrivingStatus = DrivingStatus.AutoBraking.ToString();
                            _vehicles[vehicle.Name] = vehicle;
                            if (allowSubjectNextInsideGameLoop) _subject.OnNext(vehicle);
                        }
                    }
                    else
                    {
                        if(vehicle.DrivingStatus != DrivingStatus.Approaching.ToString())
                        {
                            vehicle.DrivingStatus = DrivingStatus.Approaching.ToString();
                            vehicle.Status = $"approaching {nearestCar.Name}";
                            _vehicles[vehicle.Name] = vehicle;
                            if (allowSubjectNextInsideGameLoop) _subject.OnNext(vehicle);
                        }
                    }
                }
                else
                {
                    if(vehicle.Mph == 0 && vehicle.DrivingStatus != DrivingStatus.Stopped.ToString())
                    {
                        vehicle.DrivingStatus = DrivingStatus.Stopped.ToString();
                        vehicle.Status = $"stopped {nearestCar.Name}";
                        _vehicles[vehicle.Name] = vehicle;
                        if (allowSubjectNextInsideGameLoop) _subject.OnNext(vehicle);
                    }
                    // matched speed, 
                    if (vehicle.DrivingStatus != DrivingStatus.Tailing.ToString())
                    {
                        vehicle.DrivingStatus = DrivingStatus.Tailing.ToString();
                        vehicle.Status = $"tailing {nearestCar.Name}";
                        _vehicles[vehicle.Name] = vehicle;
                        if (allowSubjectNextInsideGameLoop) _subject.OnNext(vehicle);
                    }
                }
            }
            else
            {
                if (vehicle.AdaptiveCruiseMph != 0
                    && vehicle.Mph < vehicle.AdaptiveCruiseMph)
                {
                    // accelerating to normal speed
                    vehicle.Mph += VEHICLE_MPH_ACCELERATION_RATE;
                    vehicle.Status = $"resuming to {vehicle.AdaptiveCruiseMph}";
                    vehicle.DrivingStatus = DrivingStatus.Resuming.ToString();
                    _vehicles[vehicle.Name] = vehicle;
                    if (allowSubjectNextInsideGameLoop) _subject.OnNext(vehicle);
                }
                else if (vehicle.AdaptiveCruiseMph != 0
                    && vehicle.Mph == vehicle.AdaptiveCruiseMph)
                {
                    // vehicle returned to normal cruise speed
                    if (vehicle.DrivingStatus != DrivingStatus.Cruising.ToString())
                    {
                        vehicle.Status = $"cruising at {vehicle.AdaptiveCruiseMph}";
                        vehicle.DrivingStatus = DrivingStatus.Cruising.ToString();
                        _vehicles[vehicle.Name] = vehicle;
                        if (allowSubjectNextInsideGameLoop) _subject.OnNext(vehicle);
                    }
                }
            }
        }       
        private async Task CheckVehicleForBlindSpots(Vehicle vehicle)
        {
            // check for vehicals within blindspot range of vehicle
            var carsInBlindSpot = await this._vehicles
                .Where(otherVehicle => otherVehicle.Key != vehicle.Name
                && (otherVehicle.Value.Y == vehicle.Y - 1                                 // in neighboring lanes
                || otherVehicle.Value.Y == vehicle.Y + 1)
                && otherVehicle.Value.FrontBumper >= vehicle.RearBumper      // within car length range
                && otherVehicle.Value.RearBumper <= vehicle.FrontBumper
                ).ToAsyncEnumerable().ToList();

            if (carsInBlindSpot.Any())
            {
                vehicle.LeftBlindSpotIndicator = carsInBlindSpot.Where(v => v.Value.Y == vehicle.Y - 1).Any();
                vehicle.RightBlindSpotIndicator = carsInBlindSpot.Where(v => v.Value.Y == vehicle.Y + 1).Any();
            }
            else
            {
                vehicle.LeftBlindSpotIndicator = false;
                vehicle.RightBlindSpotIndicator = false;
            }
            _vehicles[vehicle.Name] = vehicle;
            if(allowSubjectNextInsideGameLoop) _subject.OnNext(vehicle);
        }
        private async Task CheckVehicleForUnsafeVehiclesToSave(Vehicle vehicle)
        {
            int radarRange = RADARINDICATORRANGE; // cells
            object _lock = new object();
            if (!vehicle.AdaptiveCruiseOn)
            {
                return;
            }
            // any other vehicals behind player 1 within range?
            var carsInRadarRange = await this._vehicles
                .Where(otherVehicle => otherVehicle.Key != vehicle.Name
                && otherVehicle.Value.Y == vehicle.Y                                    // in same lane
                && vehicle.RearBumper > otherVehicle.Value.FrontBumper                 // vehicle is in front of cutoff vehicle
                && otherVehicle.Value.FrontBumper + radarRange >= vehicle.RearBumper  // cutoff car's radar is within range of player1's car
                && !otherVehicle.Value.AdaptiveCruiseOn                                 // cutoff car's safety features not enabled
                ).ToAsyncEnumerable().ToList();

            if (carsInRadarRange.Any())
            {
                // SAVE THE CAR!!!!
                var fastestCarInRadarRange = carsInRadarRange.OrderByDescending(x => x.Value.Mph).First().Value;
                _vehicles[fastestCarInRadarRange.Name].AdaptiveCruiseOn = true;
                _vehicles[fastestCarInRadarRange.Name].AdaptiveCruiseMph = fastestCarInRadarRange.Mph;
                var tamedVerb = this.GetRandomTerm(TermList.Tamed);
                _vehicles[fastestCarInRadarRange.Name].Status = $"{tamedVerb} by {vehicle.Name}";
                _vehicles[fastestCarInRadarRange.Name].DrivingStatus = DrivingStatus.Cruising.ToString();
                _vehicles[fastestCarInRadarRange.Name].DrivingAdjective = this.GetRandomTerm(TermList.Safe);
                // GET POINTS!!!!!!!!!!
                _vehicles[vehicle.Name].Points += POINTSPERVEHICLESAVED;
                if(allowSubjectNextInsideGameLoop) _subject.OnNext(vehicle);
            }
        }

        #endregion

        private static int CalculateVehicleBrakingForceToMaintainLeadPreference(Vehicle vehicle, Vehicle leadingCar)
        {
            int brakeDecrease = 0;
            var speedDifference = vehicle.Mph - leadingCar.Mph;
            var cellDistanceFromLeadVehicleRearBumper = leadingCar.RearBumper - vehicle.FrontBumper;
            var brakingCellsLeft = cellDistanceFromLeadVehicleRearBumper - vehicle.AdaptiveCruisePreferredLeadNoOfCells;

            if (speedDifference == 0)
            {
                // don't brake
                return 0;
            }
            else if (speedDifference > 0 && brakingCellsLeft > RADARBRAKERANGE)
            {
                // don't brake (yet); brake when in radarbrakerange
                return 0;
            }
            else if (speedDifference > 0 && cellDistanceFromLeadVehicleRearBumper <= vehicle.AdaptiveCruisePreferredLeadNoOfCells)
            {
                // last chance to brake; brake to match lead vehicle speed!
                return speedDifference;
            }
            else if (speedDifference > 0
                && brakingCellsLeft <= RADARBRAKERANGE
                && cellDistanceFromLeadVehicleRearBumper > vehicle.AdaptiveCruisePreferredLeadNoOfCells)
            {
                // apply gradual braking given the speedDifference && cellDistanceFromLeadVehicleRearBumper
                if (speedDifference == VEHICLE_GRADUAL_MPH_BRAKE_RATE)
                {
                    // apply gradual brakes only when in last cell
                }
                else if (speedDifference >= DANGEROUSESPEEDDIFF)
                {
                    // apply max braking
                    brakeDecrease = VEHICLE_MAX_MPH_BRAKE_RATE;
                }
                else if (speedDifference >= CAUTIONSPEEDDIFF)
                {
                    // apply generous braking
                    brakeDecrease = VEHICLE_CAUTIOUS_MPH_BRAKE_RATE;
                }
                else if (speedDifference < CAUTIONSPEEDDIFF)
                {
                    // apply gradual braking
                    brakeDecrease = VEHICLE_GRADUAL_MPH_BRAKE_RATE;
                }
            }
            return brakeDecrease;
        }
        private string GetRandomTerm(TermList termList)
        {
            Random r = new Random();
            int index = 0;
            string randomString = "";
            switch (termList)
            {
                case TermList.Tamed:
                    index = r.Next(_tamedVerbs.Count);
                    randomString = _tamedVerbs[index];
                    break;
                case TermList.Crash:
                    index = r.Next(_crashVerbs.Count);
                    randomString = _crashVerbs[index];
                    break;
                case TermList.Safe:
                    index = r.Next(_safeAdjectives.Count);
                    randomString = _safeAdjectives[index];
                    break;
                case TermList.Unsafe:
                    index = r.Next(_unsafeAdjectives.Count);
                    randomString = _unsafeAdjectives[index];
                    break;
            }

            return randomString;
        }

        #endregion

        #region broadcasts

        private async Task BroadcastGameLoopBenchmark(long milliseconds)
        {
            await Hub.Clients.All.SendAsync("GameLoopBenchmark", milliseconds);
        }
        private async Task BroadcastGameStateChange(GameState GameState)
        {
            switch (GameState)
            {
                case GameState.Open:
                    await Hub.Clients.All.SendAsync("GameOpened");
                    break;
                case GameState.Closed:
                    await Hub.Clients.All.SendAsync("GameClosed");
                    break;
                default:
                    break;
            }
        }
        private async Task BroadcastGameReset()
        {
            await Hub.Clients.All.SendAsync("GameReset");
        }

        #endregion

        #region public methods & properties

        public enum VehicleAction
        {
            TurnLeft,
            TurnRight,
            IncreaseSpeed,
            DecreaseSpeed,
            ToggleAdaptiveCruise
        }

        public enum TermList
        {
            Crash,
            Tamed,
            Safe,
            Unsafe
        }

        private IHubContext<GameHub> Hub
        {
            get;
            set;
        }
        public GameState GameState
		{
			get { return _GameState; }
			private set { _GameState = value; }
		}

        public IEnumerable<Vehicle> GetAllVehicles()
		{
			return _vehicles.Values;
		}

		public IObservable<Vehicle> StreamVehicles()
		{
			return _subject;
		}

        #region User Initiated Actions

        public async Task OpenGame()
		{
			await _GameStateLock.WaitAsync();
			try
			{
				if (GameState != GameState.Open)
				{
                    if (_timer != null)
                    {
                        _timer.Dispose();
                    }
                    _timer = new Timer(UpdateVehicles, null, _updateInterval, _updateInterval);

                    GameState = GameState.Open;

					await BroadcastGameStateChange(GameState.Open);
				}
			}
			finally
			{
				_GameStateLock.Release();
			}
		}

		public async Task CloseGame()
		{
			await _GameStateLock.WaitAsync();
			try
			{
				if (GameState == GameState.Open)
				{
					if (_timer != null)
					{
						_timer.Dispose();
					}

					GameState = GameState.Closed;

					await BroadcastGameStateChange(GameState.Closed);
				}
			}
			finally
			{
				_GameStateLock.Release();
			}
		}

		public async Task Reset()
		{
			await _GameStateLock.WaitAsync();
			try
			{
                if (GameState != GameState.Closed)
                {
                    throw new InvalidOperationException("Game must be closed before it can be reset.");
                }

                LoadDefaultVehicles();
				await BroadcastGameReset();
			}
			finally
			{
				_GameStateLock.Release();
			}
		}

        public async Task ToggleAdaptiveCruise() => AddVehicleActionToQueue(VehicleAction.ToggleAdaptiveCruise);
        public async Task TurnLeft() => AddVehicleActionToQueue(VehicleAction.TurnLeft);
        public async Task TurnRight() => AddVehicleActionToQueue(VehicleAction.TurnRight);
        public async Task IncreaseSpeed() => AddVehicleActionToQueue(VehicleAction.IncreaseSpeed);
        public async Task DecreaseSpeed() => AddVehicleActionToQueue(VehicleAction.DecreaseSpeed);
        
        #endregion

        #endregion

    }

    public enum GameState
	{
		Closed,
		Open
	}
}
