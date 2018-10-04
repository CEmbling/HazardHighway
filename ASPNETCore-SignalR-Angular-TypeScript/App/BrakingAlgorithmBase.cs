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

        public int CalculateBrakeForce(Vehicle lead, Vehicle host, double updateIntervalTotalMilliseconds)
        {
            var cellClosurePerInterval = Math.Abs(lead.CalculateCellsTravelledPerInterval(updateIntervalTotalMilliseconds) - host.CalculateCellsTravelledPerInterval(updateIntervalTotalMilliseconds));
            var cellDistance = lead.RearBumper - host.FrontBumper;
            var intervalsToCollision = this.CalculateCrashCellDistance(cellDistance, cellClosurePerInterval);

            // WARNING: never match or exceed acceleration safeDistanceMultiplier
            double safeDistanceMultiplier = 1.6;  // 1.2, 1.4, 1.6 
            var hostSpeedDifferenceFromLead = lead.Mph - host.Mph;

            if (lead.Mph > host.Mph)
            {
                // lead is accelerating faster than host, no need to brake
                return 0;
            }
            else if (lead.Mph == 0)
            {
                // lead is stopped; host is approaching
                var safeStoppingDistance = this._constants.safeStoppingCellDistances[host.Mph - lead.Mph];
                if (cellDistance <= safeStoppingDistance * safeDistanceMultiplier)
                {
                    return _constants.VEHICLE_GRADUAL_MPH_BRAKE_RATE;
                }
            }
            else
            {
                // host is approaching moving lead
                var safeTailingCellDistance = this._constants.safeTailingCellDistances[Math.Abs(hostSpeedDifferenceFromLead)];
                if(cellDistance < safeTailingCellDistance * safeDistanceMultiplier)
                {
                    return _constants.VEHICLE_GRADUAL_MPH_BRAKE_RATE;
                }

                // predict where host & lead will be next interval, if it's too close, brake this interval
                // driver prefers: 
                //      approaching to match speed and safe tailing distance vs.
                //      approaching too close, braking and then acceleration to match speed and safe tailing distance
                var cellDistancePredictedNextInterval = lead.RearBumper + lead.CalculateCellsTravelledPerInterval(updateIntervalTotalMilliseconds) - (host.FrontBumper + host.CalculateCellsTravelledPerInterval(updateIntervalTotalMilliseconds));
                if (cellDistancePredictedNextInterval < safeTailingCellDistance * safeDistanceMultiplier)
                {
                    return _constants.VEHICLE_GRADUAL_MPH_BRAKE_RATE;
                }

                // safety prediction check
                var intervalsToCollisionPredictedNextInterval = this.CalculateCrashCellDistance(cellDistance, cellClosurePerInterval);
                if ((intervalsToCollisionPredictedNextInterval == 1))
                {
                    return _constants.VEHICLE_GRADUAL_MPH_BRAKE_RATE;
                }
            }
            return 0;
        }

        public int CalculateAccelerationForce(Vehicle lead, Vehicle host, double updateIntervalTotalMilliseconds)
        {
            if (!host.AdaptiveCruiseOn)
            {
                return 0;
            }
            var isHostGoingDesiredMph = host.AdaptiveCruiseMph == host.Mph || host.AdaptiveCruiseMph == 0;
            if (isHostGoingDesiredMph)
            {
                return 0;
            }
            // from here down, host desires to go faster than current mph

            var cellClosurePerInterval = Math.Abs(lead.CalculateCellsTravelledPerInterval(updateIntervalTotalMilliseconds) - host.CalculateCellsTravelledPerInterval(updateIntervalTotalMilliseconds));
            var cellDistance = lead.RearBumper - host.FrontBumper;
            double safeDistanceMultiplier = 2;  // 1.2, 1.4, 1.6
            var hostSpeedDifferenceFromLead = lead.Mph - host.Mph;

            if (lead.Mph > host.Mph)
            {
                // lead is accelerating faster than host, no need to brake
                return _constants.VEHICLE_MPH_ACCELERATION_RATE;
                
            }
            else if (lead.Mph == 0)
            {
                // lead is stopped; host is approaching
                var safeStoppingDistance = this._constants.safeStoppingCellDistances[host.Mph - lead.Mph];
                if (cellDistance > safeStoppingDistance * safeDistanceMultiplier)
                {
                    return _constants.VEHICLE_MPH_ACCELERATION_RATE;
                }
            }
            else
            {
                // host is approaching moving lead
                var safeTailingCellDistance = this._constants.safeTailingCellDistances[Math.Abs(hostSpeedDifferenceFromLead)];
                if (cellDistance > safeTailingCellDistance * safeDistanceMultiplier)
                {
                    return _constants.VEHICLE_MPH_ACCELERATION_RATE;
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
