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

            // predict where host & lead will be next interval, if it's too close, brake this interval
            // drivers prefer: 
            //      slow approach to match speed and safe tailing distance vs.
            //      quick approach, but too close, braking and then accelerating to match speed and safe tailing distance
            var cellDistancePredictedNextInterval = lead.RearBumper + lead.CalculateCellsTravelledPerInterval(updateIntervalTotalMilliseconds) - (host.FrontBumper + host.CalculateCellsTravelledPerInterval(updateIntervalTotalMilliseconds));

            var cellClosurePerInterval = Math.Abs(lead.CalculateCellsTravelledPerInterval(updateIntervalTotalMilliseconds) - host.CalculateCellsTravelledPerInterval(updateIntervalTotalMilliseconds));
            
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
