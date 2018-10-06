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
        private ICruiseAlgorithm _brakingAlgorithm;

        #endregion

        public string Name { get; set; }
        public int Mph { get; private set; }
        public bool IsGoingDesiredMph
        {
            get
            {
                return this.AdaptiveCruiseDesiredMph == 0 ||
                    this.AdaptiveCruiseDesiredMph == this.Mph;
            }
        }
        public int X { get; set; }
        public int FrontBumper { get { return X + 2; } }
        public int RearBumper { get { return X - 2; } }
        public int Y { get; set; }
        public string DrivingStatus { get; set; } = "";
        public string DrivingAdjective { get; set; } = "";
        public string Status { get; set; } = "";
        public bool AdaptiveCruiseOn { get; set; }
        public int AdaptiveCruiseDesiredMph { get; set; }
        public bool AdaptiveCruiseFrontRadarIndicator { get; set; }
        public int AdaptiveCruisePreferredLeadNoOfCells { get; set; } = 4;
        public int Points { get; set; }
        public bool LeftBlindSpotIndicator { get; set; }
        public bool RightBlindSpotIndicator { get; set; }
        public int NearRadarRange { get; set; } = 6;
        public int MidRadarRange { get; set; } = 400;
        public int FarRadarRange { get; set; } = 500;
        public bool IsHazard { get; set; }

        #region constructor

        public Vehicle(Constants constants, ICruiseAlgorithm brakingAlgorithm)
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
        public int CalculateVehicleAccelerationForceToMaintainLeadPreference(Vehicle leadVehicle, double updateIntervalTotalMilliseconds)
        {
            return this._brakingAlgorithm.CalculateAccelerationForce(leadVehicle, this, updateIntervalTotalMilliseconds);
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

            // .044     = feet per millisecond  (30 mph)
            // .051     = feet per millisecond  (35 mph)
            // .088     = feet per millisecond  (60 mph)
            Double ftpms = Convert.ToDouble(Convert.ToDouble(ftphr) / Convert.ToDouble(3600000));

            // 11       = feetTraveledPerInterval  (30 mph)
            // 12.75    = feetTraveledPerInterval  (35 mph)
            // 22       = feetTraveledPerInterval  (60 mph)
            Double feetTraveledPerInterval = Convert.ToDouble(ftpms * updateIntervalTotalMilliseconds);

            // 0        = cellsTravelledPerInterval  (00 mph)
            // 1.83     = cellsTravelledPerInterval  (05 mph)
            // 3.6      = cellsTravelledPerInterval  (10 mph)
            // 5.5      = cellsTravelledPerInterval  (15 mph)
            // 7.3      = cellsTravelledPerInterval  (20 mph)
            // 9.16     = cellsTravelledPerInterval  (25 mph)
            // 11       = cellsTravelledPerInterval  (30 mph)
            // 12.83    = cellsTravelledPerInterval  (35 mph)
            // 14       = cellsTravelledPerInterval  (40 mph)
            // 16.5     = cellsTravelledPerInterval  (45 mph)
            // 18.3     = cellsTravelledPerInterval  (50 mph)
            // 20.1     = cellsTravelledPerInterval  (55 mph)
            // 22       = cellsTravelledPerInterval  (60 mph)
            // 23.83    = cellsTravelledPerInterval  (65 mph)
            // 25.667   = cellsTravelledPerInterval  (70 mph)
            // 27.5     = cellsTravelledPerInterval  (75 mph)
            // 29.333   = cellsTravelledPerInterval  (80 mph)
            // 31.166   = cellsTravelledPerInterval  (85 mph)
            // 33       = cellsTravelledPerInterval  (90 mph)
            // 34.833   = cellsTravelledPerInterval  (95 mph)
            // 36.667   = cellsTravelledPerInterval  (100 mph)
            int cellsTravelledPerInterval = Convert.ToInt32(feetTraveledPerInterval / _constants.FEETPERCELL);
            return cellsTravelledPerInterval;
        }

        public void AddMph(int accelerationMph, bool isHumanInitiating)
        {
            if (this.Mph + accelerationMph > this._constants.VEHICLE_MPH_MAX_ACCELERATION)
            {
                return;
            }

            if (isHumanInitiating)
            {
                this.Mph += accelerationMph;
                if (this.AdaptiveCruiseOn)
                {
                    this.AdaptiveCruiseDesiredMph += accelerationMph;
                }
            }
            else
            {
                // speed increase caused by adaptive cruise resuming 
                // make sure car doesn't accelerate past the desired mph
                if (this.Mph < this.AdaptiveCruiseDesiredMph)
                {
                    this.Mph += accelerationMph;
                }
            }
        }
        public void AddAdaptiveCruiseMph(int mph)
        {
            if (this.AdaptiveCruiseDesiredMph + mph > this._constants.VEHICLE_MPH_MAX_ACCELERATION)
            {
                return;
            }
            this.AdaptiveCruiseDesiredMph += mph;
        }
        public void SubtractMph(int brakeMph)
        {
            if (brakeMph <= 0)
                return;
            if(this.Mph - brakeMph < 0)
            {
                return;
            }
            this.Mph -= brakeMph;
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
                AdaptiveCruiseMph = this.AdaptiveCruiseDesiredMph,
                AdaptiveCruisePreferredLeadNoOfCells = this.AdaptiveCruisePreferredLeadNoOfCells,
                Mph = this.Mph,
                Points = this.Points,
                Status = this.Status,
                IsHazard = this.IsHazard
            };
        }

        public void IncrementPositionChange(double intervalTotalMilliseconds)
        {
            if (this.DrivingStatus == ASPNETCore_SignalR_Angular_TypeScript.App.DrivingStatus.Crashed.ToString())
            {
                return;
            }
            int cellsTravelledPerInterval = this.CalculateCellsTravelledPerInterval(intervalTotalMilliseconds);
            this.X += cellsTravelledPerInterval;
        }

        public static class Factory
        {
            public static Vehicle Create(string name, int mph, int x, int y, bool adaptiveCruiseOn, bool isHazard = false)
            {
                Constants constants = new Constants();
                return new Vehicle(constants, new CruiseAlgorithm(constants))
                {
                    Name = name,
                    Mph = mph,
                    X = x,
                    Y = y,
                    AdaptiveCruiseOn = adaptiveCruiseOn,
                    AdaptiveCruiseDesiredMph = adaptiveCruiseOn? mph : 0,
                    IsHazard = isHazard
                };
            }
        }
    }
}
