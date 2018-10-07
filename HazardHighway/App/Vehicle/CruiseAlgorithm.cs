using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ASPNETCore_SignalR_Angular_TypeScript.Models;

namespace ASPNETCore_SignalR_Angular_TypeScript.App
{
    public class CruiseAlgorithm : BrakingAlgorithmBase, ICruiseAlgorithm
    {
        public CruiseAlgorithm(Constants constants): base(constants)
        {

        }

        public int CalculateBrakeForce(Vehicle lead, Vehicle host, double updateIntervalTotalMilliseconds)
        {
            return base.CalculateBrakeForce(lead, host, updateIntervalTotalMilliseconds);
        }
        public int CalculateAccelerationForce(Vehicle lead, Vehicle host, double updateIntervalTotalMilliseconds)
        {
            return base.CalculateAccelerationForce(lead, host, updateIntervalTotalMilliseconds);
        }
        public int CalculateCellsTravelledPerInterval(int mph, double updateIntervalTotalMilliseconds)
        {
            return base.CalculateCellsTravelledPerInterval(mph, updateIntervalTotalMilliseconds);
        }
        public int PredictCellDistanceAfterNumberOfIntevals(Vehicle lead, Vehicle host, int numberOfIntervals, double updateIntervalTotalMilliseconds)
        {
            return base.PredictCellDistanceAfterNumberOfIntevals(lead, host, numberOfIntervals, updateIntervalTotalMilliseconds);
        }
    }
}
