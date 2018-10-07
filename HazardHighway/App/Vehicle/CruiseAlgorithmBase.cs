using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASPNETCore_SignalR_Angular_TypeScript.App
{
    public abstract class BrakingAlgorithmBase
    {
        private Constants _constants;
        
        public BrakingAlgorithmBase(Constants constants)
        {
            this._constants = constants;
        }

        public int CalculateCellsTravelledPerInterval(int mph, double updateIntervalTotalMilliseconds)
        {
            if (mph <= 0)
            {
                return 0;
            }
            // 158,400 = 30 mph * 5280 ftpm
            // 184,000 = 35 mph * 5280 ftpm
            // 316,800 = 60 mph * 5280 ftpm
            var ftphr = mph * _constants.FEETPERMILE;

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
            double cellsTravelledPerInterval = feetTraveledPerInterval / _constants.FEETPERCELL;
            return Convert.ToInt32(Math.Ceiling(cellsTravelledPerInterval));
        }

        public int CalculateBrakeForce(Vehicle lead, Vehicle host, double updateIntervalTotalMilliseconds)
        {
            var cellClosurePerInterval = Math.Abs(lead.CalculateCellsTravelledPerInterval(updateIntervalTotalMilliseconds) - host.CalculateCellsTravelledPerInterval(updateIntervalTotalMilliseconds));
            var cellDistance = lead.RearBumper - host.FrontBumper;
            var intervalsToCollision = this.CalculateCrashCellDistance(cellDistance, cellClosurePerInterval);

            var hostSpeedDifferenceFromLead = lead.Mph - host.Mph;

            if (lead.Mph > host.Mph)
            {
                // lead is accelerating faster than host, no need to brake
                return 0;
            }
            else if (lead.Mph == 0)
            {
                // lead is stopped; host is approaching
                var safeStoppingDistance = this._constants.safeStoppingCellDistances[host.Mph];
                if (cellDistance <= safeStoppingDistance)
                {
                    return _constants.VEHICLE_GRADUAL_MPH_BRAKE_RATE;
                }
            }
            else if(lead.Mph == host.Mph)
            {
                // host is tailing lead; is host following too closely?
                var safeTailingCellDistance = this._constants.safeTailingCellDistances[host.Mph];
                if (cellDistance < safeTailingCellDistance)
                {
                    return _constants.VEHICLE_GRADUAL_MPH_BRAKE_RATE;
                }
            }
            else
            {
                // host is approaching lead
                var safeTailingCellDistance = this.CalculateSafeTailingCellDistanceFromLead(lead.Mph, host.Mph);
                if (cellDistance < safeTailingCellDistance)
                {
                    return _constants.VEHICLE_GRADUAL_MPH_BRAKE_RATE;
                }

                // predict where host & lead will be next interval, if it's too close, brake this interval
                // drivers prefer: 
                //      slow approach to match speed and safe tailing distance vs.
                //      quick approach, but too close, braking and then accelerating to match speed and safe tailing distance
                var cellDistancePredictedNextInterval = lead.RearBumper + lead.CalculateCellsTravelledPerInterval(updateIntervalTotalMilliseconds) - (host.FrontBumper + host.CalculateCellsTravelledPerInterval(updateIntervalTotalMilliseconds));
                if (cellDistancePredictedNextInterval < safeTailingCellDistance)
                {
                    return _constants.VEHICLE_GRADUAL_MPH_BRAKE_RATE;
                }

                // safety prediction check
                var intervalsToCollisionPredictedNextInterval = this.CalculateCrashCellDistance(cellDistancePredictedNextInterval, cellClosurePerInterval);
                if (intervalsToCollisionPredictedNextInterval > 0 
                    && (intervalsToCollisionPredictedNextInterval - 1 < hostSpeedDifferenceFromLead / _constants.VEHICLE_GRADUAL_MPH_BRAKE_RATE))
                {
                    return _constants.VEHICLE_GRADUAL_MPH_BRAKE_RATE;
                }
            }
            return 0;
        }

        public int CalculateSafeTailingCellDistanceFromLead(int leadMph, int hostMph)
        {
            // when host & lead are going same speed, this is the safe distance
            var safeTailingCellDistance = this._constants.safeTailingCellDistances[hostMph];
            var hostSpeedDifferenceFromLead = hostMph - leadMph;
            if(hostSpeedDifferenceFromLead > 0)
            {
                var multiplier = (Math.Abs(hostSpeedDifferenceFromLead == 0 ? 1 : hostSpeedDifferenceFromLead / _constants.VEHICLE_GRADUAL_MPH_BRAKE_RATE));
                return safeTailingCellDistance * multiplier;
            }
            else
            {
                return safeTailingCellDistance;
            }


            //var leadStop = lead.CalculateCellsNeededForCompleteStop(updateIntervalTotalMilliseconds);
            //var hostStop = host.CalculateCellsNeededForCompleteStop(updateIntervalTotalMilliseconds);
            //if (host.X + hostStop + 10 < lead.X + leadStop)
            //{
            //    // time to break
            //    return _constants.VEHICLE_GRADUAL_MPH_BRAKE_RATE;
            //}
            //else
            //{
            //    // don't really need to brake
            //    //return _constants.VEHICLE_GRADUAL_MPH_BRAKE_RATE;
            //}

        }

        public int PredictCellDistanceAfterNumberOfIntevals(Vehicle lead, Vehicle host, int numberOfIntervals, double updateIntervalTotalMilliseconds)
        {
            return lead.RearBumper + lead.CalculateCellsTravelledPerInterval(numberOfIntervals * updateIntervalTotalMilliseconds) 
                - (host.FrontBumper + host.CalculateCellsTravelledPerInterval(numberOfIntervals * updateIntervalTotalMilliseconds));
        }

        public int CalculateAccelerationForce(Vehicle lead, Vehicle host, double updateIntervalTotalMilliseconds)
        {
            if (!host.AdaptiveCruiseOn)
            {
                return 0;
            }
            if (host.IsGoingDesiredMph)
            {
                return 0;
            }
            // from here down, host desires to go faster than current mph

            // predict where host & lead will be after the next n intervals
            // drivers prefer: 
            //      slow approach to match speed and safe tailing distance vs.
            //      quick approach, but too close, braking and then accelerating to match speed and safe tailing distance
            var cellDistanceFromLead = this.PredictCellDistanceAfterNumberOfIntevals(lead, host, 0, updateIntervalTotalMilliseconds);
            var cellDistancePredictedNextInterval = this.PredictCellDistanceAfterNumberOfIntevals(lead, host, 2, updateIntervalTotalMilliseconds);
            var hostSpeedDifferenceFromLead = lead.Mph - host.Mph;

            if (lead.Mph == 0) 
            {
                // lead is stopped; host is approaching
                // predict whether increasing mph is host still within safe stopping distance
                var predictSafeStoppingCellDistance = this._constants.safeStoppingCellDistances[host.Mph + _constants.VEHICLE_MPH_ACCELERATION_INCREMENT_RATE];
                if (cellDistancePredictedNextInterval > predictSafeStoppingCellDistance)
                {
                    return _constants.VEHICLE_MPH_ACCELERATION_INCREMENT_RATE;
                }
            }
            else if (lead.Mph == host.Mph)
            {
                // host is tailing lead; can host speed up and still maintain safe tailing distance?
                var predictSafeTailingCellDistance = this._constants.safeTailingCellDistances[host.Mph + _constants.VEHICLE_MPH_ACCELERATION_INCREMENT_RATE];
                if (cellDistancePredictedNextInterval > predictSafeTailingCellDistance)
                {
                    return _constants.VEHICLE_MPH_ACCELERATION_INCREMENT_RATE;
                }
            }
            else
            {
                // host is approaching moving lead
                // add mph to host to predict if car would still be in safe tailing distance from lead
                var predictSafeTailingCellDistance = this.CalculateSafeTailingCellDistanceFromLead(lead.Mph, host.Mph + _constants.VEHICLE_MPH_ACCELERATION_INCREMENT_RATE);
                if (cellDistancePredictedNextInterval > predictSafeTailingCellDistance)
                {
                    return _constants.VEHICLE_MPH_ACCELERATION_INCREMENT_RATE;
                }
            }
            return 0;
        }
        private double CalculateCrashCellDistance(double cellDistanceFromLead, double cellClosurePerInterval)
        {
            if(cellClosurePerInterval == 0)
            {
                return 0;
            }
            double intervalsToCollision = cellDistanceFromLead / cellClosurePerInterval;
            if (intervalsToCollision > Convert.ToInt32(intervalsToCollision))
            {
                intervalsToCollision = Convert.ToDouble(Convert.ToInt32(intervalsToCollision) + 1);
            }
            return Math.Abs(intervalsToCollision);
        }
    }
}
