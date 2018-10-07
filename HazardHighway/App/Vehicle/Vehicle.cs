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
            return CalculateCellsTravelledPerInterval(this.Mph, updateIntervalTotalMilliseconds);
        }
        public int CalculateCellsTravelledPerInterval(int mph, double updateIntervalTotalMilliseconds)
        {
            return this._brakingAlgorithm.CalculateCellsTravelledPerInterval(mph, updateIntervalTotalMilliseconds);
        }

        public int CalculateCellsNeededForCompleteStop(double intervalTotalMilliseconds)
        {
            return this.CalculateCellsTravelledPerInterval(this.Mph, intervalTotalMilliseconds);
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
        public void AddMph(int accelerationMph, bool isHumanInitiating)
        {
            if(accelerationMph == 0)
            {
                return;
            }
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

        public static class Factory
        {
            public static Vehicle Create(string name, int mph, int x, int y, bool adaptiveCruiseOn, DrivingStatus drivingStatus = App.DrivingStatus.Driving, bool isHazard = false)
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
                    IsHazard = isHazard,
                    DrivingStatus = mph == 0? App.DrivingStatus.Stopped.ToString(): drivingStatus.ToString()
                };
            }
        }
    }
}
