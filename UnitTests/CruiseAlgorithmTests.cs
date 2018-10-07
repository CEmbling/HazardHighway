using ASPNETCore_SignalR_Angular_TypeScript.App;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace UnitTests
{
    [TestClass]
    public class CruiseAlgorithmTests
    {
        [TestMethod]
        [DataRow(5, 0, 0, 22, 2, 14)]
        [DataRow(10, 0, 0, 39, 2, 27)]
        public void PredictCellDistanceAfterNumberOfIntevals(int hostMph, int hostX, int leadCarMph, int leadCarX, int intervals, int expectedPredictedCellDistance)
        {
            double updateIntervalTotalMilliseconds = 250;
            Constants constants = new Constants();
            var host = Vehicle.Factory.Create("host car", hostMph, hostX, 1, true, drivingStatus: DrivingStatus.Driving);
            var lead = Vehicle.Factory.Create("lead car", leadCarMph, leadCarX, 1, true, drivingStatus: DrivingStatus.Driving);

            var SUT = new CruiseAlgorithm(constants);
            int cellDistanceFromLead = SUT.PredictCellDistanceAfterNumberOfIntevals(lead, host, 0, updateIntervalTotalMilliseconds);
            int predictedCellDistanceFromLead = SUT.PredictCellDistanceAfterNumberOfIntevals(lead, host, intervals, updateIntervalTotalMilliseconds);

            Assert.AreEqual(expectedPredictedCellDistance, predictedCellDistanceFromLead);
        }
    }
}
