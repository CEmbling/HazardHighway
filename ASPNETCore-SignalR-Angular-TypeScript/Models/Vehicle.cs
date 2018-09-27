using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASPNETCore_SignalR_Angular_TypeScript.Models
{
    public class Vehicle
    {
        public string Name { get; set; }
        public int Mph { get; set; }
        public int X { get; set; }
        public int FrontBumper { get { return X + 2; } }
        public int RearBumper { get { return X - 2; } }
        public int Y { get; set; }
        public string DrivingStatus { get; set; } = "";
        public string DrivingAdjective { get; set; } = "";
        public string Status { get; set; } = "";
        public bool AdaptiveCruiseOn { get; set; }
        public int AdaptiveCruiseMph { get; set; }
        public bool AdaptiveCruiseFrontRadarIndicator { get; set; }
        public int AdaptiveCruisePreferredLeadNoOfCells { get; set; } = 4;
        public int Points { get; set; }
        public bool LeftBlindSpotIndicator { get; set; }
        public bool RightBlindSpotIndicator { get; set; }
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
}
