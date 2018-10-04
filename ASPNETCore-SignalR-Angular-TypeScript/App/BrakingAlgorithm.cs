using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ASPNETCore_SignalR_Angular_TypeScript.Models;

namespace ASPNETCore_SignalR_Angular_TypeScript.App
{
    public class BrakingAlgorithm : BrakingAlgorithmBase, IBrakingAlgorithm
    {
        public BrakingAlgorithm(Constants constants): base(constants)
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
            return this.CalculateCellsTravelledPerInterval(mph, updateIntervalTotalMilliseconds);
        }
    }
}
