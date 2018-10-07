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
                if (cellDistance <= safeStoppingDistance - 2)
                {
                    return _constants.VEHICLE_GRADUAL_MPH_BRAKE_RATE;
                }
            }
            else
            {
                // host is approaching or tailing moving lead
                var safeTailingCellDistance = this._constants.safeTailingCellDistances[host.Mph];
                if (cellDistance < (safeTailingCellDistance * (Math.Abs(hostSpeedDifferenceFromLead == 0? 1 : hostSpeedDifferenceFromLead/_constants.VEHICLE_GRADUAL_MPH_BRAKE_RATE))))
                {
                    return _constants.VEHICLE_GRADUAL_MPH_BRAKE_RATE;
                }

                // predict where host & lead will be next interval, if it's too close, brake this interval
                // driver prefers: 
                //      approaching to match speed and safe tailing distance vs.
                //      approaching too close, braking and then acceleration to match speed and safe tailing distance
                var cellDistancePredictedNextInterval = lead.RearBumper + lead.CalculateCellsTravelledPerInterval(updateIntervalTotalMilliseconds) - (host.FrontBumper + host.CalculateCellsTravelledPerInterval(updateIntervalTotalMilliseconds));
                if (cellDistancePredictedNextInterval < (safeTailingCellDistance * (Math.Abs(hostSpeedDifferenceFromLead == 0 ? 1 : hostSpeedDifferenceFromLead / _constants.VEHICLE_GRADUAL_MPH_BRAKE_RATE))))
                {
                    return _constants.VEHICLE_GRADUAL_MPH_BRAKE_RATE;
                }

                //// safety prediction check
                //var intervalsToCollisionPredictedNextInterval = this.CalculateCrashCellDistance(cellDistance, cellClosurePerInterval);
                //if ((intervalsToCollisionPredictedNextInterval < hostSpeedDifferenceFromLead / _constants.VEHICLE_GRADUAL_MPH_BRAKE_RATE))
                //{
                //    return _constants.VEHICLE_GRADUAL_MPH_BRAKE_RATE;
                //}
            }
            return 0;
        }

        public int CalculateAccelerationForce(Vehicle lead, Vehicle host, double updateIntervalTotalMilliseconds)
        {
            if (!host.AdaptiveCruiseOn)
            {
                return 0;
            }
            var isHostGoingDesiredMph = host.AdaptiveCruiseDesiredMph == host.Mph || host.AdaptiveCruiseDesiredMph == 0;
            if (isHostGoingDesiredMph)
            {
                return 0;
            }
            // from here down, host desires to go faster than current mph

            var cellClosurePerInterval = Math.Abs(lead.CalculateCellsTravelledPerInterval(updateIntervalTotalMilliseconds) - host.CalculateCellsTravelledPerInterval(updateIntervalTotalMilliseconds));
            var cellDistance = lead.RearBumper - host.FrontBumper;
            double safeDistanceMultiplier = 2;  // 1.2, 1.4, 1.6
            var hostSpeedDifferenceFromLead = lead.Mph - host.Mph;

            if (lead.Mph == 0)
            {
                // lead is stopped; host is approaching
                var safeStoppingDistance = this._constants.safeStoppingCellDistances[host.Mph];
                //if (cellDistance > (safeStoppingDistance * (safeDistanceMultiplier + Math.Abs(hostSpeedDifferenceFromLead))))
                if (cellDistance > safeStoppingDistance)
                {
                    return _constants.VEHICLE_MPH_ACCELERATION_INCREMENT_RATE;
                }
            }
            else
            {
                // host is approaching moving lead
                var safeTailingCellDistance = this._constants.safeTailingCellDistances[Math.Abs(host.Mph)];
                if (cellDistance > (safeTailingCellDistance * (Math.Abs(hostSpeedDifferenceFromLead == 0? 1: hostSpeedDifferenceFromLead/_constants.VEHICLE_GRADUAL_MPH_BRAKE_RATE))))
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
