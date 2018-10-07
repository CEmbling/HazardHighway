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
			LoadGame(GameLevel.Level1);
		}

        #endregion

        #region private methods

        private IHubContext<GameHub> _hub
        {
            get;
            set;
        }
        private void LoadGame(GameLevel gameLevel)
        {
            this._gameScenario =  GameScenario.Factory.Create(gameLevel, this._constants, this._terms);
        }

        // THIS IS THE GAME LOOP CYCLE
        private async void UpdateVehicles(object state)
        {
            // This function must be re-entrant as it's running as a timer interval handler
            Stopwatch s = new Stopwatch();
            s.Start();
            await ClearTemporaryVehicleDrivingStatuses();

            // physics & queued vehicle actions (ie. user initiated steering & speed changes)
            bool didUserInitiateAction = _queuedVehicleActions.Any();
            await UpdateVehiclesPhysics();
            if (didUserInitiateAction)
            {
                // user-initiated actions that we queued up for game loop
                await UpdateVehicleSteeringAndSpeedChanges();
            }
            await CheckVehiclesForCollisions();

            // reactive/automatic vehicle monitoring
            List<Task> tasks = new List<Task>();
            tasks.Add(CheckVehiclesForBlindSpots());
            tasks.Add(CheckVehiclesForApproachingVehicles());
            Task.WaitAll(tasks.ToArray());
            
            if (!_constants.allowSubjectNextInsideGameLoop)
            {
                await BroadcastAllVehiclesSubjects();
            }
            s.Stop();
            await BroadcastGameLoopBenchmark(s.ElapsedMilliseconds);
            await BroadcastGameLoopVehicles();
            await CheckForGameEnding();
        }

        private async Task UpdateVehiclesPhysics()
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
                    foreach (var vehicle in this._gameScenario.Vehicles.Values)
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
        private async Task CheckForGameEnding()
        {
            bool gameWon = false;
            bool gameLost = false;
            // This function must be re-entrant as it's running as a timer interval handler
            await _VehiclePositionsLock.WaitAsync();
            try
            {
                if (!_updatingVehiclePositions)
                {
                    _updatingVehiclePositions = true;

                    var vehicleCount = this._gameScenario.Vehicles.Count();

                    // did player 1 crash?
                    gameLost = this._gameScenario.Vehicles
                                    .Where(v => v.Value.Name == _constants.PLAYER1
                                            && v.Value.DrivingStatus == DrivingStatus.Crashed.ToString())
                                    .Any();

                    // did all vehicles stop successfully?
                    gameLost = this._gameScenario.Vehicles
                                    .Where(v => v.Value.DrivingStatus == DrivingStatus.Stopped.ToString()
                                    || v.Value.DrivingStatus == DrivingStatus.Crashed.ToString())
                                    .Count() == vehicleCount;

                    // did all vehicles stop successfully?
                    gameWon = this._gameScenario.Vehicles
                                    .Where(v => v.Value.DrivingStatus == DrivingStatus.Stopped.ToString())
                                    .Count() == vehicleCount;

                    _updatingVehiclePositions = false;
                }
            }
            finally
            {
                _VehiclePositionsLock.Release();
            }


            if (gameWon)
            {
                await CloseGame();
                await BroadcastGameWon();
                return;
            }
            else if (gameLost)
            {
                await CloseGame();
                await BroadcastGameLost();
                return;
            }
            else
            {
                // game still going
            }

        }
        private async Task UpdateVehicleSteeringAndSpeedChanges()
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
            vehicle.IncrementPositionChange(this._updateInterval.TotalMilliseconds);

            return true;
        }
        private bool TryUpdateVehicleAction(Vehicle host, QueuedVehicleAction action)
        {
            if (host.DrivingStatus == DrivingStatus.Crashed.ToString())
            {
                return false;
            }
            switch (action.VehicleAction)
            {
                case VehicleAction.IncreaseSpeed_CruiseInitiated:
                    if (!host.IsGoingDesiredMph)
                    {
                        // accelerating to desired speed
                        host.AddMph(_constants.VEHICLE_MPH_ACCELERATION_INCREMENT_RATE, isHumanInitiating: false);
                        if (host.DrivingStatus != DrivingStatus.Resuming.ToString())
                        {
                            host.Status = $"resuming to {host.AdaptiveCruiseDesiredMph} mph";
                            host.DrivingStatus = DrivingStatus.Resuming.ToString();
                            if (this._constants.allowSubjectNextInsideGameLoop) _subject.OnNext(host.ToModel());
                        }
                        this._gameScenario.Vehicles[host.Name] = host;
                    }
                    break;
                case VehicleAction.IncreaseSpeed_UserInitiated:
                    if (TryIncreaseVehicleMph(host, this._constants.VEHICLE_MPH_ACCELERATION_INCREMENT_RATE))
                    {
                        host.DrivingStatus = DrivingStatus.Accelerating.ToString();
                        if (host.AdaptiveCruiseOn)
                        {
                            // keep in AdaptiveCruiseMph insync w/ human accelerating
                            host.AdaptiveCruiseDesiredMph = host.Mph;
                        }
                    }
                    break;
                case VehicleAction.DecreaseSpeed_CruiseInitiated:
                    if (host.AdaptiveCruiseDesiredMph == 0)
                    {
                        // store original speed (so we can return to it when the slow car moves)
                        host.AdaptiveCruiseDesiredMph = host.Mph;
                    }
                    host.SubtractMph(this._constants.VEHICLE_GRADUAL_MPH_BRAKE_RATE);
                    if (host.Mph == 0)
                    {
                        if (host.DrivingStatus != DrivingStatus.Stopped.ToString())
                        {
                            host.Status = $"stopped for {action.leadVehicleName}";
                            host.DrivingStatus = DrivingStatus.Stopped.ToString();
                            if (this._constants.allowSubjectNextInsideGameLoop) _subject.OnNext(host.ToModel());
                        }
                        this._gameScenario.Vehicles[host.Name] = host;
                    }
                    else
                    {
                        if (host.DrivingStatus != DrivingStatus.AutoBraking.ToString())
                        {
                            host.Status = $"autobraking for {action.leadVehicleName}";
                            host.DrivingStatus = DrivingStatus.AutoBraking.ToString();
                            if (this._constants.allowSubjectNextInsideGameLoop) _subject.OnNext(host.ToModel());
                        }
                        this._gameScenario.Vehicles[host.Name] = host;
                    }
                    break;
                case VehicleAction.DecreaseSpeed_UserInitiated:
                    if (TryDecreaseVehicleMph(host, this._constants.VEHICLE_GRADUAL_MPH_BRAKE_RATE))
                    {
                        if (host.Mph == 0)
                        {
                            if (host.DrivingStatus != DrivingStatus.Stopped.ToString())
                            {
                                TryUpdateVehicleDrivingStatus(host, DrivingStatus.Stopped);
                                host.DrivingStatus = DrivingStatus.Stopped.ToString();
                                host.Status = $"stopped by {action.leadVehicleName}";
                                if (this._constants.allowSubjectNextInsideGameLoop) _subject.OnNext(host.ToModel());
                            }
                            this._gameScenario.Vehicles[host.Name] = host;
                        }
                        else
                        {
                            TryUpdateVehicleDrivingStatus(host, DrivingStatus.Braking);
                        }
                        
                        if (host.AdaptiveCruiseOn)
                        {
                            // keep in AdaptiveCruiseMph insync w/ human deaccelaration
                            host.AdaptiveCruiseDesiredMph = host.Mph;
                        }
                    }
                    break;
                case VehicleAction.TurnLeft:
                    if (host.Mph > 0 && host.Y >= 3)
                    {
                        host.Y -= 1;
                        TryUpdateVehicleDrivingStatus(host, DrivingStatus.TurningLeft);
                        CheckVehicleForUnsafeVehiclesToSave(host);
                    }
                    break;
                case VehicleAction.TurnRight:
                    if (host.Mph > 0 && host.Y < 8)
                    {
                        host.Y += 1;
                        TryUpdateVehicleDrivingStatus(host, DrivingStatus.TurningRight);
                        CheckVehicleForUnsafeVehiclesToSave(host);
                    }
                    break;
                case VehicleAction.ToggleAdaptiveCruise:
                    host.AdaptiveCruiseOn = !host.AdaptiveCruiseOn;
                    if (host.AdaptiveCruiseOn)
                    {
                        TryUpdateVehicleDrivingStatus(host, DrivingStatus.Cruising);
                        // keep AdaptiveCruiseMph insync w/ human initiated events, like enabled AC
                        host.AdaptiveCruiseDesiredMph = host.Mph;
                    }
                    else
                    {
                        TryUpdateVehicleDrivingStatus(host, DrivingStatus.Driving);
                        host.AdaptiveCruiseDesiredMph = 0;
                    }
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
        private bool TryDecreaseVehicleMph(Vehicle vehicle, int mph)
        {
            vehicle.SubtractMph(mph);
            return true;
        }
        private bool TryIncreaseVehicleMph(Vehicle vehicle, int mph)
        {
            vehicle.AddMph(mph, isHumanInitiating:true);
            return true;
        }
        private async Task BroadcastAllVehiclesSubjects()
        {
            foreach (var vehicle in this._gameScenario.Vehicles.Values)
            {
                _subject.OnNext(vehicle.ToModel());
            }
        }

        #endregion


        #region User Initiated private methods

        private async Task AddVehicleActionToQueue(string vehicleKey, VehicleAction action, string leadVehicleName)
        {
            await _VehicleActionsLock.WaitAsync();
            try
            {
                if (!_updatingVehicleActions)
                {
                    _updatingVehicleActions = true;

                    var vehicle = this._gameScenario.Vehicles.Where(v => v.Key == vehicleKey).Select(x => x.Value).Single();
                    TryAddVehicleActionToQueue(vehicle.Name, action, leadVehicleName);

                    _updatingVehicleActions = false;
                }
            }
            finally
            {
                _VehicleActionsLock.Release();
            }
        }

        private bool TryAddVehicleActionToQueue(string vehicleKey, VehicleAction vehicleAction, string leadVehicleName)
        {
            return _queuedVehicleActions.TryAdd(vehicleKey, QueuedVehicleAction.Factory.Create(vehicleKey, vehicleAction, leadVehicleName));
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

                    if (TryUpdateVehicleDrivingStatus(vehicle, drivingStatus))
                    {
                        if (this._constants.allowSubjectNextInsideGameLoop) _subject.OnNext(vehicle.ToModel());
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
                if (TryUpdateVehicleDrivingStatus(crashedIntoVehicle, DrivingStatus.Crashed))
                {
                    TryDecreaseVehicleMph(crashedIntoVehicle, vehicle.Mph);
                    if (this._constants.allowSubjectNextInsideGameLoop) _subject.OnNext(crashedIntoVehicle.ToModel());
                }
                if (crashedIntoVehicle.DrivingStatus != DrivingStatus.Crashed.ToString())
                {
                    TryUpdateVehicleStatus(crashedIntoVehicle, crashedIntoVehicle.Status);   
                }

                TryUpdateVehicleStatus(vehicle, vehicle.Status);
                if (TryUpdateVehicleDrivingStatus(vehicle, DrivingStatus.Crashed))
                {
                    if(this._constants.allowSubjectNextInsideGameLoop) _subject.OnNext(vehicle.ToModel());
                }

                //if (vehicle.Name == this._constants.PLAYER1 || crashedIntoVehicle.Name == this._constants.PLAYER1)
                //{
                //    // game over; player1 crashed! consider doing this in the game status check
                //    await this.CloseGame();
                //    await this.BroadcastGameLost();
                //}
            }
        }
        private async Task CheckVehicleForApproachingVehicles(Vehicle host)
        {
            if (!host.AdaptiveCruiseOn)
                return;

            var isHostStopped = host.Mph == 0;

            // check for lead vehicals within range in front of host vehicle
            var carsInRadarRange = await this._gameScenario.Vehicles
                .Where(otherVehicle => otherVehicle.Key != host.Name
                    && otherVehicle.Value.Y == host.Y                                                // in same lane
                    && otherVehicle.Value.RearBumper <= host.FrontBumper + this._constants.RADARINDICATORRANGE      // within radar range
                    && otherVehicle.Value.RearBumper > host.FrontBumper                              // within radar range
                ).ToAsyncEnumerable().ToList();

            host.AdaptiveCruiseFrontRadarIndicator = carsInRadarRange.Any();

            if (host.AdaptiveCruiseFrontRadarIndicator)
            {
                // lead vehicle in radar range
                var lead = carsInRadarRange.OrderBy(x => x.Value.X).First().Value;

                var isHostApproaching = host.Mph > lead.Mph;
                var isHostTailing = host.Mph == lead.Mph;

                // we may need to decrease speed...check distance first
                // calculate breaking force
                var brakeForce = host.CalculateVehicleBrakingForceToMaintainLeadPreference(lead, this._updateInterval.TotalMilliseconds);
                if (brakeForce > 0)
                {
                    // breaking is necessary
                    await this.AddVehicleActionToQueue(host.Name, VehicleAction.DecreaseSpeed_CruiseInitiated, lead.Name);
                }
                else // brakeForce == 0  // no braking necessary
                {
                    if (!host.IsGoingDesiredMph)
                    {
                        // determine if acceleration is desired
                        var accelerationForce = host.CalculateVehicleAccelerationForceToMaintainLeadPreference(lead, this._updateInterval.TotalMilliseconds);
                        if (accelerationForce > 0)
                        {
                            await this.AddVehicleActionToQueue(host.Name, VehicleAction.IncreaseSpeed_CruiseInitiated, "");
                        }
                        else
                        {
                            // no acceleration necessary
                            if (isHostApproaching)
                            {
                                // host is approaching
                                if (host.DrivingStatus != DrivingStatus.Approaching.ToString())
                                {
                                    host.DrivingStatus = DrivingStatus.Approaching.ToString();
                                    host.Status = $"approaching {lead.Name}";
                                    if (this._constants.allowSubjectNextInsideGameLoop) _subject.OnNext(host.ToModel());
                                }
                                this._gameScenario.Vehicles[host.Name] = host;
                            }
                            else if (isHostTailing)
                            {
                                // host is tailing
                                if (host.DrivingStatus != DrivingStatus.Tailing.ToString())
                                {
                                    host.DrivingStatus = DrivingStatus.Tailing.ToString();
                                    host.Status = $"tailing {lead.Name}";
                                    if (this._constants.allowSubjectNextInsideGameLoop) _subject.OnNext(host.ToModel());
                                }
                                this._gameScenario.Vehicles[host.Name] = host;
                            }
                        }
                    }
                }
            }
            else
            {
                // no lead cars in range of host
                if (!host.IsGoingDesiredMph)
                {
                    // accelerating to desired speed
                    await this.AddVehicleActionToQueue(host.Name, VehicleAction.IncreaseSpeed_CruiseInitiated, "");
                }
                else if (host.IsGoingDesiredMph)
                {
                    // vehicle returned to normal cruise speed
                    if (host.DrivingStatus != DrivingStatus.Cruising.ToString())
                    {
                        host.Status = $"cruising at {host.AdaptiveCruiseDesiredMph} mph";
                        host.DrivingStatus = DrivingStatus.Cruising.ToString();
                        if (this._constants.allowSubjectNextInsideGameLoop) _subject.OnNext(host.ToModel());
                    }
                    this._gameScenario.Vehicles[host.Name] = host;
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
                this._gameScenario.Vehicles[fastestCarInRadarRange.Name].AdaptiveCruiseDesiredMph = fastestCarInRadarRange.Mph;
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
        private async Task BroadcastGameLoopVehicles()
        {
            await _hub.Clients.All.SendAsync("GameLoopVehicles", this.GetAllVehicles());
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
        private async Task BroadcastGameLost()
        {
            await _hub.Clients.All.SendAsync("GameLost");
        }
        private async Task BroadcastGameWon()
        {
            await _hub.Clients.All.SendAsync("GameWon");
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
		public async Task Reset(string level)
		{
            GameLevel gameLevel = GameLevel.Level1;
            Enum.TryParse(level, out gameLevel);
			await _GameStateLock.WaitAsync();
			try
			{
                if (GameState != GameState.Closed)
                {
                    throw new InvalidOperationException("Game must be closed before it can be reset.");
                }

                LoadGame(gameLevel);
				await BroadcastGameReset();
			}
			finally
			{
				_GameStateLock.Release();
			}
		}
        public async Task ToggleAdaptiveCruise() => AddVehicleActionToQueue(this._constants.PLAYER1, VehicleAction.ToggleAdaptiveCruise, "user");
        public async Task TurnLeft() => AddVehicleActionToQueue(this._constants.PLAYER1, VehicleAction.TurnLeft, "user");
        public async Task TurnRight() => AddVehicleActionToQueue(this._constants.PLAYER1, VehicleAction.TurnRight, "user");
        public async Task IncreaseSpeed() => AddVehicleActionToQueue(this._constants.PLAYER1, VehicleAction.IncreaseSpeed_UserInitiated, "user");
        public async Task DecreaseSpeed() => AddVehicleActionToQueue(this._constants.PLAYER1, VehicleAction.DecreaseSpeed_UserInitiated, "user");
        
        #endregion

        #endregion

    }
}
