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
            if (lead.Mph == 0)
            {
                // brake
                return 5;
            }

            var cellClosurePerInterval = Math.Abs(lead.CalculateCellsTravelledPerInterval(updateIntervalTotalMilliseconds) - host.CalculateCellsTravelledPerInterval(updateIntervalTotalMilliseconds));
            var cellDistance = lead.RearBumper - host.FrontBumper;
            var intervalsToCollision = this.CalculateCrashCellDistance(cellDistance, cellClosurePerInterval);
            var safeIntervalsToFollow = 2;  // 2-4
            var hostSpeedDifferenceFromLead = lead.Mph - host.Mph;

            if(hostSpeedDifferenceFromLead > 0)
            {
                // lead is accelerating away
            }
            else if (hostSpeedDifferenceFromLead == 0)
            {
                // check if too close
                if (intervalsToCollision <= safeIntervalsToFollow)
                {
                    return _constants.VEHICLE_GRADUAL_MPH_BRAKE_RATE;
                }
            }
            else if (hostSpeedDifferenceFromLead < 0)
            {
                // lead is traveling slower; host is approaching
                var intervalsNeededToMatchSpeed = Math.Abs(hostSpeedDifferenceFromLead) / _constants.VEHICLE_GRADUAL_MPH_BRAKE_RATE;

                if (intervalsToCollision <= (safeIntervalsToFollow + intervalsNeededToMatchSpeed))
                {
                    return _constants.VEHICLE_GRADUAL_MPH_BRAKE_RATE;
                }
            }
            return 0;
        }
        private double CalculateCrashCellDistance(double cellDistanceFromLead, double cellClosurePerInterval)
        {
            double intervalsToCollision = cellDistanceFromLead / cellClosurePerInterval;
            if (intervalsToCollision > Convert.ToInt32(intervalsToCollision))
            {
                intervalsToCollision = Convert.ToDouble(Convert.ToInt32(intervalsToCollision) + 1);
            }
            return Math.Abs(intervalsToCollision);
        }
    }
}
