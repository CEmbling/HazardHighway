using ASPNETCore_SignalR_Angular_TypeScript.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASPNETCore_SignalR_Angular_TypeScript.App
{
    public interface ICruiseAlgorithm
    {
        int CalculateBrakeForce(Vehicle lead, Vehicle host, double updateIntervalTotalMilliseconds);
        int CalculateAccelerationForce(Vehicle lead, Vehicle host, double updateIntervalTotalMilliseconds);
        int CalculateCellsTravelledPerInterval(int mph, double updateIntervalTotalMilliseconds);
        int PredictCellDistanceAfterNumberOfIntevals(Vehicle lead, Vehicle host, int numberOfIntervals, double updateIntervalTotalMilliseconds);
    }
}
