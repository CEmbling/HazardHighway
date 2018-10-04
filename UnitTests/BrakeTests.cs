using ASPNETCore_SignalR_Angular_TypeScript.App;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace UnitTests
{
    [TestClass]
    public class BrakeTests
    {

        [TestMethod]
        [DataRow(5, 0, 0, 6)]
        [DataRow(10, 0, 0, 15)]
        [DataRow(15, 0, 0, 15)]
        [DataRow(20, 0, 0, 15)]
        [DataRow(25, 0, 0, 15)]
        [DataRow(30, 0, 0, 74)]
        [DataRow(35, 0, 0, 95)]
        [DataRow(40, 0, 0, 118)]
        [DataRow(45, 0, 0, 144)]
        [DataRow(50, 0, 0, 172)]
        [DataRow(55, 0, 0, 207)]
        [DataRow(60, 0, 0, 239)]
        [DataRow(65, 0, 0, 275)]
        [DataRow(70, 0, 0, 315)]
        [DataRow(75, 0, 0, 356)]
        [DataRow(80, 0, 0, 400)]
        [DataRow(90, 0, 0, 494)]
        [DataRow(95, 0, 0, 545)]
        [DataRow(100, 0, 0, 598)]
        public void BrakeTest_When_Host_Approaching_Stopped_Lead_Host_Should_Brake(int hostMph, int hostX, int leadCarMph, int leadCarX)
        {
            double updateIntervalTotalMilliseconds = 250;
            Constants constants = new Constants();
            var SUT = Vehicle.Factory.Create("host car", hostMph, hostX, 1, true);
            var lead = Vehicle.Factory.Create("lead car", leadCarMph, leadCarX, 1, true);
            var brake = SUT.CalculateVehicleBrakingForceToMaintainLeadPreference(lead, updateIntervalTotalMilliseconds);
            Assert.AreEqual(5, brake);
        }

        [TestMethod]
        [DataRow(5, 0, 0, 6 * 2)]
        [DataRow(10, 0, 0, 15 * 2)]
        [DataRow(15, 0, 0, 26 * 2)]
        [DataRow(20, 0, 0, 39 * 2)]
        [DataRow(25, 0, 0, 55 * 2)]
        [DataRow(30, 0, 0, 74 * 2)]
        [DataRow(35, 0, 0, 95 * 2)]
        [DataRow(40, 0, 0, 118 * 2)]
        [DataRow(45, 0, 0, 144 * 2)]
        [DataRow(50, 0, 0, 172 * 2)]
        [DataRow(55, 0, 0, 207 * 2)]
        [DataRow(60, 0, 0, 239 * 2)]
        [DataRow(65, 0, 0, 275 * 2)]
        [DataRow(70, 0, 0, 315 * 2)]
        [DataRow(75, 0, 0, 356 * 2)]
        [DataRow(80, 0, 0, 400 * 2)]
        [DataRow(90, 0, 0, 494 * 2)]
        [DataRow(95, 0, 0, 545 * 2)]
        [DataRow(100, 0, 0, 598 * 2)]
        public void BrakeTest_When_Host_Approaching_Stopped_Lead_Host_Host_ShouldNotBrake(int hostMph, int hostX, int leadCarMph, int leadCarX)
        {
            double updateIntervalTotalMilliseconds = 250;
            Constants constants = new Constants();
            var SUT = Vehicle.Factory.Create("host car", hostMph, hostX, 1, true);
            var lead = Vehicle.Factory.Create("lead car", leadCarMph, leadCarX, 1, true);
            var brake = SUT.CalculateVehicleBrakingForceToMaintainLeadPreference(lead, updateIntervalTotalMilliseconds);
            Assert.AreEqual(0, brake);
        }

        [TestMethod]
        [DataRow(5, 0, 5, 6-2)]
        [DataRow(10, 0, 10, 15 - 2)]
        [DataRow(15, 0, 15, 26 - 2)]
        [DataRow(20, 0, 20, 39 - 2)]
        [DataRow(25, 0, 25, 55 - 2)]
        [DataRow(30, 0, 30, 74 - 2)]
        [DataRow(35, 0, 35, 95 - 2)]
        [DataRow(40, 0, 40, 118 - 2)]
        [DataRow(45, 0, 45, 144 - 2)]
        [DataRow(50, 0, 50, 172 - 2)]
        [DataRow(55, 0, 55, 207 - 2)]
        [DataRow(60, 0, 60, 239 - 2)]
        [DataRow(65, 0, 65, 275 - 2)]
        [DataRow(70, 0, 70, 315 - 2)]
        [DataRow(75, 0, 75, 356 - 2)]
        [DataRow(80, 0, 80, 400 - 2)]
        [DataRow(90, 0, 90, 494 - 2)]
        [DataRow(95, 0, 95, 545 - 2)]
        [DataRow(100, 0, 100, 598 - 2)]
        public void BrakeTest_When_Host_Tailing_Lead_Too_Close_Host_Should_Brake(int hostMph, int hostX, int leadCarMph, int leadCarX)
        {
            double updateIntervalTotalMilliseconds = 250;
            Constants constants = new Constants();
            var SUT = Vehicle.Factory.Create("host car", hostMph, hostX, 1, true);
            var lead = Vehicle.Factory.Create("lead car", leadCarMph, leadCarX, 1, true);
            var brake = SUT.CalculateVehicleBrakingForceToMaintainLeadPreference(lead, updateIntervalTotalMilliseconds);
            Assert.AreEqual(5, brake);
        }

        [TestMethod]
        [DataRow(5, 0, 5, 6)]
        [DataRow(10, 0, 10, 15)]
        [DataRow(15, 0, 15, 15)]
        [DataRow(20, 0, 20, 15)]
        [DataRow(25, 0, 25, 15)]
        [DataRow(30, 0, 30, 74)]
        [DataRow(35, 0, 35, 95)]
        [DataRow(40, 0, 40, 118)]
        [DataRow(45, 0, 45, 144)]
        [DataRow(50, 0, 50, 172)]
        [DataRow(55, 0, 55, 207)]
        [DataRow(60, 0, 60, 239)]
        [DataRow(65, 0, 65, 275)]
        [DataRow(70, 0, 70, 315)]
        [DataRow(75, 0, 75, 356)]
        [DataRow(80, 0, 80, 400)]
        [DataRow(90, 0, 90, 494)]
        [DataRow(95, 0, 95, 545)]
        [DataRow(100, 0, 100, 598)]
        public void BrakeTest_When_Host_Approaching_Moving_Lead_Host_Should_Brake(int hostMph, int hostX, int leadCarMph, int leadCarX)
        {
            double updateIntervalTotalMilliseconds = 250;
            Constants constants = new Constants();
            var SUT = Vehicle.Factory.Create("host car", hostMph, hostX, 1, true);
            var lead = Vehicle.Factory.Create("lead car", leadCarMph, leadCarX, 1, true);
            var brake = SUT.CalculateVehicleBrakingForceToMaintainLeadPreference(lead, updateIntervalTotalMilliseconds);
            Assert.AreEqual(5, brake);
        }

        [TestMethod]
        [DataRow(5, 0, 0, 6 * 2)]
        [DataRow(10, 0, 0, 15 * 2)]
        [DataRow(15, 0, 0, 26 * 2)]
        [DataRow(20, 0, 0, 39 * 2)]
        [DataRow(25, 0, 0, 55 * 2)]
        [DataRow(30, 0, 0, 74 * 2)]
        [DataRow(35, 0, 0, 95 * 2)]
        [DataRow(40, 0, 0, 118 * 2)]
        [DataRow(45, 0, 0, 144 * 2)]
        [DataRow(50, 0, 0, 172 * 2)]
        [DataRow(55, 0, 0, 207 * 2)]
        [DataRow(60, 0, 0, 239 * 2)]
        [DataRow(65, 0, 0, 275 * 2)]
        [DataRow(70, 0, 0, 315 * 2)]
        [DataRow(75, 0, 0, 356 * 2)]
        [DataRow(80, 0, 0, 400 * 2)]
        [DataRow(90, 0, 0, 494 * 2)]
        [DataRow(95, 0, 0, 545 * 2)]
        [DataRow(100, 0, 0, 598 * 2)]
        public void BrakeTest_When_Host_Approaching_Moving_Lead_Host_Should_Not_Brake(int hostMph, int hostX, int leadCarMph, int leadCarX)
        {
            double updateIntervalTotalMilliseconds = 250;
            Constants constants = new Constants();
            var SUT = Vehicle.Factory.Create("host car", hostMph, hostX, 1, true);
            var lead = Vehicle.Factory.Create("lead car", leadCarMph, leadCarX, 1, true);
            var brake = SUT.CalculateVehicleBrakingForceToMaintainLeadPreference(lead, updateIntervalTotalMilliseconds);
            Assert.AreEqual(0, brake);
        }
    }
}
