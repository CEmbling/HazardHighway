using ASPNETCore_SignalR_Angular_TypeScript.App;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASPNETCore_SignalR_Angular_TypeScript.Models
{
    public class VehicleModel
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

        public static class Factory
        {
            public static VehicleModel Create(Vehicle v)
            {
                return new VehicleModel
                {
                    Name = v.Name,
                    X = v.X,
                    Y = v.Y,
                    DrivingAdjective = v.DrivingAdjective,
                    DrivingStatus = v.DrivingStatus,
                    AdaptiveCruiseOn = v.AdaptiveCruiseOn,
                    AdaptiveCruiseFrontRadarIndicator = v.AdaptiveCruiseFrontRadarIndicator,
                    AdaptiveCruiseMph = v.AdaptiveCruiseMph,
                    AdaptiveCruisePreferredLeadNoOfCells = v.AdaptiveCruisePreferredLeadNoOfCells,
                    Mph = v.Mph,
                    Points = v.Points,
                    Status = v.Status
                };
            }
        }
    }
}
