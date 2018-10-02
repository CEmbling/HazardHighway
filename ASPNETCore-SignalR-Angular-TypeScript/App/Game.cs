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

        private Constants _constants;
        private GameScenario _gameScenario;
        private readonly ConcurrentDictionary<string, QueuedVehicleAction> _queuedVehicleActions = new ConcurrentDictionary<string, QueuedVehicleAction>();
        private readonly List<string> _temporaryDrivingStatuses = new List<string> {
            DrivingStatus.TurningLeft.ToString(), DrivingStatus.TurningRight.ToString(),
            DrivingStatus.Accelerating.ToString(), DrivingStatus.Braking.ToString(),
            DrivingStatus.AutoBraking.ToString(), DrivingStatus.Resuming.ToString() };
        private readonly Subject<VehicleModel> _subject = new Subject<VehicleModel>();
        private const int UPDATE_INTERVAL_MILLISECONDS = 250; //250;
		private readonly TimeSpan _updateInterval = TimeSpan.FromMilliseconds(UPDATE_INTERVAL_MILLISECONDS);
        private readonly Random _updateOrNotRandom = new Random();
        private readonly Terminology _terms;

		private Timer _timer;
		private volatile bool _updatingVehiclePositions;
        private volatile bool _updatingVehicleActions;
        private volatile GameState _GameState;



        #endregion

        #region constructors

        public Game(IHubContext<GameHub> hub, Constants constants, Terminology terms)
		{
			_hub = hub;
            _constants = constants;
            _terms = terms;
			LoadGame();
		}

        #endregion

        #region private methods

        private IHubContext<GameHub> _hub
        {
            get;
            set;
        }
        private void LoadGame()
        {
            this._gameScenario =  GameScenario.Factory.Create(GameLevel.Level1, this._constants, this._terms);
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
            if (!_constants.allowSubjectNextInsideGameLoop)
            {
                await BroadcastAllVehiclesSubjects();
            }
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

                    // update all vehicles before player 1; update player1 last
                    // player 1 publish to client will cause screen re-render
                    foreach (var vehicle in this._gameScenario.Vehicles.Values
                                                                    .Where(v => v.DrivingStatus != DrivingStatus.Crashed.ToString())
                                                                    //.OrderBy(v => v.Name == "Player 1")
                                                                    )
                    {
                        if (TryUpdateVehiclePosition(vehicle))
                        {
                            if (this._constants.allowSubjectNextInsideGameLoop) _subject.OnNext(vehicle.ToModel());
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

                    foreach (var vehicle in this._gameScenario.Vehicles.Values)
                    {
                        if (_temporaryDrivingStatuses.Contains(vehicle.DrivingStatus) && vehicle.Mph > 0)
                        {
                            TryUpdateVehicleDrivingStatus(vehicle, vehicle.AdaptiveCruiseOn? DrivingStatus.Cruising : DrivingStatus.Driving);
                        }
                        else if(_temporaryDrivingStatuses.Contains(vehicle.DrivingStatus) && vehicle.Mph == 0)
                        {
                            TryUpdateVehicleDrivingStatus(vehicle, DrivingStatus.Stopped);
                        }

                        if(this._constants.allowSubjectNextInsideGameLoop) _subject.OnNext(vehicle.ToModel());
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

                    foreach (var vehicle in this._gameScenario.Vehicles.Values)
                    {
                        await CheckVehicleForCollisions(vehicle);

                        if(this._constants.allowSubjectNextInsideGameLoop) _subject.OnNext(vehicle.ToModel());
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

                    foreach (var vehicle in this._gameScenario.Vehicles.Values
                        .Where(v => v.AdaptiveCruiseOn
                            && v.DrivingStatus != DrivingStatus.Crashed.ToString()))
                    {
                        await CheckVehicleForApproachingVehicles(vehicle);

                        if(this._constants.allowSubjectNextInsideGameLoop) _subject.OnNext(vehicle.ToModel());
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

                    var tasks = this._gameScenario.Vehicles
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

        #endregion

        #region task functions

        private bool TryUpdateVehiclePosition(Vehicle vehicle)
        {
            if (vehicle.DrivingStatus == DrivingStatus.Crashed.ToString())
            {
                return false;
            }
            int cellsTravelledPerInterval = vehicle.CalculateCellsTravelledPerInterval(this._updateInterval.TotalMilliseconds);
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
            foreach (var vehicle in this._gameScenario.Vehicles.Values)
            {
                _subject.OnNext(vehicle.ToModel());
            }
            //// signal to ui to re-render highway
            //_subject.OnNext(new VehicleModel { Name = "RENDER_HWY" });
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

                    var playerVehicle = this._gameScenario.Vehicles.Where(v => v.Key == this._constants.PLAYER1).Select(x => x.Value).Single();
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

                    foreach (var vehicle in this._gameScenario.Vehicles
                        .Where(v => v.Key == queuedVehicleAction.Name)
                        .Select(x => x.Value))
                    {
                        if(TryUpdateVehicleAction(vehicle, queuedVehicleAction))
                        {
                            if(this._constants.allowSubjectNextInsideGameLoop) _subject.OnNext(vehicle.ToModel());
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

                    foreach (var Vehicle in this._gameScenario.Vehicles.Where(v => v.Key == vehicle.Name).Select(x => x.Value))
                    {
                        TryUpdateVehicleDrivingStatus(Vehicle, drivingStatus);

                        //if(this._constants.allowSubjectNextInsideGameLoop) _subject.OnNext(vehicle.ToModel());
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

                    foreach (var vehicle in this._gameScenario.Vehicles.Where(v => v.Key == vehicleKey).Select(x => x.Value))
                    {
                        TryUpdateVehicleStatus(vehicle, status);

                        if(this._constants.allowSubjectNextInsideGameLoop) _subject.OnNext(vehicle.ToModel());
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
            var collidedWithList = this._gameScenario.Vehicles
                .Where(otherVehicle => otherVehicle.Key != vehicle.Name
                && vehicle.DrivingStatus != DrivingStatus.Crashed.ToString()    // ignore cars that already crashed
                && otherVehicle.Value.Y == vehicle.Y                            // in same lane
                && otherVehicle.Value.X >= (vehicle.X - this._constants.VEHICLECELLLENGTH)      // within collision range
                && otherVehicle.Value.X <= (vehicle.X + this._constants.VEHICLECELLLENGTH)
                ).ToList();

            bool vehicleCrashed = collidedWithList.Any();
            if (vehicleCrashed)
            {
                var crashedIntoVehicle = collidedWithList.First().Value;
                var isAtFault = vehicle.X < crashedIntoVehicle.X;
                
                var crashAdverb = this._terms.GetRandomTerm(TermList.Crash);
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
                        _subject.OnNext(crashedIntoVehicle.ToModel());
                    }
                }

                TryUpdateVehicleStatus(vehicle, vehicle.Status);
                if (TryUpdateVehicleDrivingStatus(vehicle, DrivingStatus.Crashed))
                {
                    TryUpdateVehicleMph(vehicle, 0);
                    if(this._constants.allowSubjectNextInsideGameLoop) _subject.OnNext(vehicle.ToModel());
                }

                if (vehicle.Name == this._constants.PLAYER1 || crashedIntoVehicle.Name == this._constants.PLAYER1)
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
            var carsInRadarRange = await this._gameScenario.Vehicles
                .Where(otherVehicle => otherVehicle.Key != vehicle.Name
                    && otherVehicle.Value.Y == vehicle.Y                                                // in same lane
                    && otherVehicle.Value.RearBumper <= vehicle.FrontBumper + this._constants.RADARINDICATORRANGE      // within radar range
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
                    var brakeForce = vehicle.CalculateVehicleBrakingForceToMaintainLeadPreference(nearestCar, this._updateInterval.TotalMilliseconds);
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
                            this._gameScenario.Vehicles[vehicle.Name] = vehicle;
                            if (this._constants.allowSubjectNextInsideGameLoop) _subject.OnNext(vehicle.ToModel());
                        }
                        else
                        {
                            vehicle.Status = $"autobraking for {nearestCar.Name}";
                            vehicle.DrivingStatus = DrivingStatus.AutoBraking.ToString();
                            this._gameScenario.Vehicles[vehicle.Name] = vehicle;
                            if (this._constants.allowSubjectNextInsideGameLoop) _subject.OnNext(vehicle.ToModel());
                        }
                    }
                    else
                    {
                        if(vehicle.DrivingStatus != DrivingStatus.Approaching.ToString())
                        {
                            vehicle.DrivingStatus = DrivingStatus.Approaching.ToString();
                            vehicle.Status = $"approaching {nearestCar.Name}";
                            this._gameScenario.Vehicles[vehicle.Name] = vehicle;
                            if (this._constants.allowSubjectNextInsideGameLoop) _subject.OnNext(vehicle.ToModel());
                        }
                    }
                }
                else
                {
                    if(vehicle.Mph == 0 && vehicle.DrivingStatus != DrivingStatus.Stopped.ToString())
                    {
                        vehicle.DrivingStatus = DrivingStatus.Stopped.ToString();
                        vehicle.Status = $"stopped {nearestCar.Name}";
                        this._gameScenario.Vehicles[vehicle.Name] = vehicle;
                        if (this._constants.allowSubjectNextInsideGameLoop) _subject.OnNext(vehicle.ToModel());
                    }
                    // matched speed, 
                    if (vehicle.DrivingStatus != DrivingStatus.Tailing.ToString())
                    {
                        vehicle.DrivingStatus = DrivingStatus.Tailing.ToString();
                        vehicle.Status = $"tailing {nearestCar.Name}";
                        this._gameScenario.Vehicles[vehicle.Name] = vehicle;
                        if (this._constants.allowSubjectNextInsideGameLoop) _subject.OnNext(vehicle.ToModel());
                    }
                }
            }
            else
            {
                if (vehicle.AdaptiveCruiseMph != 0
                    && vehicle.Mph < vehicle.AdaptiveCruiseMph)
                {
                    // accelerating to normal speed
                    vehicle.Mph += this._constants.VEHICLE_MPH_ACCELERATION_RATE;
                    vehicle.Status = $"resuming to {vehicle.AdaptiveCruiseMph}";
                    vehicle.DrivingStatus = DrivingStatus.Resuming.ToString();
                    this._gameScenario.Vehicles[vehicle.Name] = vehicle;
                    if (this._constants.allowSubjectNextInsideGameLoop) _subject.OnNext(vehicle.ToModel());
                }
                else if (vehicle.AdaptiveCruiseMph != 0
                    && vehicle.Mph == vehicle.AdaptiveCruiseMph)
                {
                    // vehicle returned to normal cruise speed
                    if (vehicle.DrivingStatus != DrivingStatus.Cruising.ToString())
                    {
                        vehicle.Status = $"cruising at {vehicle.AdaptiveCruiseMph}";
                        vehicle.DrivingStatus = DrivingStatus.Cruising.ToString();
                        this._gameScenario.Vehicles[vehicle.Name] = vehicle;
                        if (this._constants.allowSubjectNextInsideGameLoop) _subject.OnNext(vehicle.ToModel());
                    }
                }
            }
        }       
        private async Task CheckVehicleForBlindSpots(Vehicle vehicle)
        {
            // check for vehicals within blindspot range of vehicle
            var carsInBlindSpot = await this._gameScenario.Vehicles
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
            this._gameScenario.Vehicles[vehicle.Name] = vehicle;
            if(this._constants.allowSubjectNextInsideGameLoop) _subject.OnNext(vehicle.ToModel());
        }
        private async Task CheckVehicleForUnsafeVehiclesToSave(Vehicle vehicle)
        {
            int radarRange = this._constants.RADARINDICATORRANGETOSAVE; // cells
            object _lock = new object();
            if (!vehicle.AdaptiveCruiseOn)
            {
                return;
            }
            // any other vehicals behind player 1 within range?
            var carsInRadarRange = await this._gameScenario.Vehicles
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
                this._gameScenario.Vehicles[fastestCarInRadarRange.Name].AdaptiveCruiseOn = true;
                this._gameScenario.Vehicles[fastestCarInRadarRange.Name].AdaptiveCruiseMph = fastestCarInRadarRange.Mph;
                var tamedVerb = this._terms.GetRandomTerm(TermList.Tamed);
                this._gameScenario.Vehicles[fastestCarInRadarRange.Name].Status = $"{tamedVerb} by {vehicle.Name}";
                this._gameScenario.Vehicles[fastestCarInRadarRange.Name].DrivingStatus = DrivingStatus.Cruising.ToString();
                this._gameScenario.Vehicles[fastestCarInRadarRange.Name].DrivingAdjective = this._terms.GetRandomTerm(TermList.Safe);
                // GET POINTS!!!!!!!!!!
                this._gameScenario.Vehicles[vehicle.Name].Points += this._constants.POINTSPERVEHICLESAVED;
                if(this._constants.allowSubjectNextInsideGameLoop) _subject.OnNext(vehicle.ToModel());
            }
        }

        #endregion

        #region broadcasts

        private async Task BroadcastGameLoopBenchmark(long milliseconds)
        {
            await _hub.Clients.All.SendAsync("GameLoopBenchmark", milliseconds);
        }
        private async Task BroadcastGameStateChange(GameState GameState)
        {
            switch (GameState)
            {
                case GameState.Open:
                    await _hub.Clients.All.SendAsync("GameOpened");
                    break;
                case GameState.Closed:
                    await _hub.Clients.All.SendAsync("GameClosed");
                    break;
                default:
                    break;
            }
        }
        private async Task BroadcastGameReset()
        {
            await _hub.Clients.All.SendAsync("GameReset");
        }

        #endregion

        #region public methods & properties

        public GameState GameState
		{
			get { return _GameState; }
			private set { _GameState = value; }
		}

        public IEnumerable<VehicleModel> GetAllVehicles()
		{
			return this._gameScenario.Vehicles.Select(v => v.Value.ToModel());
		}

		public IObservable<VehicleModel> StreamVehicles()
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

                LoadGame();
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
}
