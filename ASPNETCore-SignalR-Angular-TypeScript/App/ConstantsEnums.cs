using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASPNETCore_SignalR_Angular_TypeScript.App
{
    public class Constants
    {

        public bool allowSubjectNextInsideGameLoop { get => false; }
        public string PLAYER1 { get => "Player 1"; }
        public int FEETPERMILE { get => 5280; }
        public double FEETPERCELL { get => 1; }
        public int MILLISECONDSPERHOUR { get => 3600000; }
        public int VEHICLECELLLENGTH { get => 5; }
        public int RADARINDICATORRANGE { get => 600; }
        // accelerating
        public int VEHICLE_MPH_ACCELERATION_RATE { get => 5; }
        // braking
        public int RADARBRAKERANGE { get => 8; } // will be multipled by vehicle's cellsTraveledPerInterval value
        public int POINTSPERVEHICLESAVED { get => 1000; }
        public int DANGEROUSESPEEDDIFF { get => 50; }
        public int CAUTIONSPEEDDIFF { get => 35; }
        public int VEHICLE_MAX_MPH_BRAKE_RATE { get => 15; }
        public int VEHICLE_CAUTIOUS_MPH_BRAKE_RATE { get => 10; }
        public int VEHICLE_GRADUAL_MPH_BRAKE_RATE { get => 5; }
        // saving
        public int RADARINDICATORRANGETOSAVE = 8;
        public Dictionary<int, int> safeStoppingCellDistances = new Dictionary<int, int>()
        {
            {0, 4},
            {5, 6},
            {10, 15},
            {15, 26},
            {20, 39},
            {25, 55},
            {30, 74},
            {35, 95},
            {40, 118},
            {45, 144},
            {50, 172},
            {55, 207},
            {60, 239},
            {65,  275},
            {70, 315},
            {75,  356},
            {80, 400},
            {85, 446},
            {90, 494},
            {95, 545},
            {100, 598}
        };
        public Dictionary<int, int> safeTailingCellDistances = new Dictionary<int, int>()
        {
            {0, 4},
            {5, 4},
            {10, 8},
            {15, 12},
            {20, 16},
            {25, 20},
            {30, 24},
            {35, 28},
            {40, 32},
            {45, 36},
            {50, 40},
            {55, 44},
            {60, 48},
            {65, 52},
            {70, 56},
            {75, 60},
            {80, 64},
            {85, 68},
            {90, 72},
            {95, 76},
            {100, 80}
        };
    }
    public enum GameState
    {
        Closed,
        Open
    }
    public enum GameLevel
    {
        Level1,
        Level2,
        Level3 
    }
    public enum VehicleAction
    {
        TurnLeft,
        TurnRight,
        IncreaseSpeed,
        DecreaseSpeed,
        ToggleAdaptiveCruise
    }
    public enum DrivingStatus
    {
        Braking,            // Human
        Accelerating,       // Human
        TurningLeft,        // Human
        TurningRight,       // Human
        Driving,            // Human
        Stopped,            // Human or AC
        Cruising,           // AC Only
        Approaching,        // AC Only
        AutoBraking,        // AC Only
        Tailing,            // AC Only
        Resuming,           // AC Only
        LeftAdjustment,     // AC Only
        RightAdjustment,    // AC Only
        Crashed             // No!!!!
    }
    public enum TermList
    {
        Crash,
        Tamed,
        Safe,
        Unsafe
    }
}
