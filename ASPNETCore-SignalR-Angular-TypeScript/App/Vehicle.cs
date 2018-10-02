using ASPNETCore_SignalR_Angular_TypeScript.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASPNETCore_SignalR_Angular_TypeScript.App
{
    public class Vehicle
    {
        #region private members

        private Constants _constants;
        private IBrakingAlgorithm _brakingAlgorithm;

        #endregion

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
        public int NearRadarRange { get; set; } = 6;
        public int MidRadarRange { get; set; } = 400;
        public int FarRadarRange { get; set; } = 500;

        #region constructor

        public Vehicle(Constants constants, IBrakingAlgorithm brakingAlgorithm)
        {
            this._constants = constants;
            this._brakingAlgorithm = brakingAlgorithm;
        }

        #endregion

        public bool IsWithinFarRadarRange(Vehicle leadVehicle) => leadVehicle.RearBumper < this.FrontBumper + FarRadarRange;
        public bool IsWithinMidRadarRange(Vehicle leadVehicle) => leadVehicle.RearBumper < this.FrontBumper + MidRadarRange;
        public bool IsWithinNearRadarRange(Vehicle leadVehicle) => leadVehicle.RearBumper < this.FrontBumper + NearRadarRange;
        public int CalculateVehicleBrakingForceToMaintainLeadPreference(Vehicle leadVehicle, double updateIntervalTotalMilliseconds)
        {
            return this._brakingAlgorithm.CalculateBrakeForce(leadVehicle, this, updateIntervalTotalMilliseconds);
        }
        public int CalculateCellsTravelledPerInterval(double updateIntervalTotalMilliseconds)
        {
            if (this.Mph <= 0)
            {
                return 0;
            }
            // 158,400 = 30 mph * 5280 ftpm
            // 184,000 = 35 mph * 5280 ftpm
            // 316,800 = 60 mph * 5280 ftpm
            var ftphr = this.Mph * _constants.FEETPERMILE;

            // .044 = feet per millisecond  (30 mph)
            // .051 = feet per millisecond  (35 mph)
            // .088 = feet per millisecond  (60 mph)
            Double ftpms = Convert.ToDouble(Convert.ToDouble(ftphr) / Convert.ToDouble(3600000));

            // 11 = feetTraveledPerInterval  (30 mph)
            // 12.75 = feetTraveledPerInterval  (35 mph)
            // 22 = feetTraveledPerInterval  (60 mph)
            Double feetTraveledPerInterval = Convert.ToDouble(ftpms * updateIntervalTotalMilliseconds);

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
            // 22       = cellsTravelledPerInterval  (60 mph)
            // 6.375    = cellsTravelledPerInterval  (75 mph)
            int cellsTravelledPerInterval = Convert.ToInt32(feetTraveledPerInterval / _constants.FEETPERCELL);
            return cellsTravelledPerInterval;
        }

        public VehicleModel ToModel()
        {
            return new VehicleModel
            {
                Name = this.Name,
                X = this.X,
                Y = this.Y,
                DrivingAdjective = this.DrivingAdjective,
                DrivingStatus = this.DrivingStatus,
                AdaptiveCruiseOn = this.AdaptiveCruiseOn,
                AdaptiveCruiseFrontRadarIndicator = this.AdaptiveCruiseFrontRadarIndicator,
                AdaptiveCruiseMph = this.AdaptiveCruiseMph,
                AdaptiveCruisePreferredLeadNoOfCells = this.AdaptiveCruisePreferredLeadNoOfCells,
                Mph = this.Mph,
                Points = this.Points,
                Status = this.Status
            };
        }

        public static class Factory
        {
            public static Vehicle Create(string name, int mph, int x, int y, bool adaptiveCruiseOn)
            {
                Constants constants = new Constants();
                return new Vehicle(constants, new BrakingAlgorithm(constants))
                {
                    Name = name,
                    Mph = mph,
                    X = x,
                    Y = y,
                    AdaptiveCruiseOn = adaptiveCruiseOn
                };
            }
        }
    }
}
