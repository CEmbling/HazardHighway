using ASPNETCore_SignalR_Angular_TypeScript.App;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace UnitTests
{
    [TestClass]
    public class BrakeTests
    {

        [TestMethod]
        [DataRow(5, 0, 0, 5)]
        [DataRow(10, 0, 0, 15)]
        [DataRow(15, 0, 0, 26)]
        [DataRow(20, 0, 0, 39)]
        [DataRow(25, 0, 0, 55)]
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
        [DataRow(5, 0, 5, 4-2)]
        [DataRow(10, 0, 10, 8 - 2)]
        [DataRow(15, 0, 15, 12 - 2)]
        [DataRow(20, 0, 20, 16 - 2)]
        [DataRow(25, 0, 25, 20 - 2)]
        [DataRow(30, 0, 30, 24 - 2)]
        [DataRow(35, 0, 35, 28 - 2)]
        [DataRow(40, 0, 40, 32 - 2)]
        [DataRow(45, 0, 45, 36 - 2)]
        [DataRow(50, 0, 50, 40 - 2)]
        [DataRow(55, 0, 55, 44 - 2)]
        [DataRow(60, 0, 60, 48 - 2)]
        [DataRow(65, 0, 65, 52 - 2)]
        [DataRow(70, 0, 70, 56 - 2)]
        [DataRow(75, 0, 75, 60 - 2)]
        [DataRow(80, 0, 80, 64 - 2)]
        [DataRow(90, 0, 90, 72 - 2)]
        [DataRow(95, 0, 95, 76 - 2)]
        [DataRow(100, 0, 100, 80 - 2)]
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
        [DataRow(5, 0, 0, 12)]
        [DataRow(10, 0, 5, 12)]
        [DataRow(10, 0, 0, 12)]
        [DataRow(15, 0, 10, 16 * 1)]
        [DataRow(15, 0, 05, 16 * 2)]
        [DataRow(15, 0, 00, 16 * 3)]
        [DataRow(30, 0, 25, 28 * 1)]
        [DataRow(30, 0, 20, 28 * 2)]
        [DataRow(30, 0, 15, 28 * 3)]
        [DataRow(30, 0, 10, 28 * 4)]
        [DataRow(35, 0, 25, 32 * 1)]
        [DataRow(35, 0, 20, 32 * 2)]
        [DataRow(35, 0, 15, 32 * 3)]
        [DataRow(35, 0, 10, 32 * 4)]
        [DataRow(35, 0, 05, 32 * 5)]
        [DataRow(35, 0, 00, 32 * 6)]
        [DataRow(40, 0, 35, 36 * 1)]
        [DataRow(40, 0, 30, 36 * 1)]
        [DataRow(40, 0, 25, 36 * 2)]
        [DataRow(40, 0, 20, 36 * 3)]
        [DataRow(40, 0, 15, 36 * 4)]
        [DataRow(40, 0, 10, 36 * 5)]
        [DataRow(40, 0, 05, 36 * 6)]
        [DataRow(40, 0, 00, 36 * 7)]
        [DataRow(50, 0, 45, 36 * 1)]
        [DataRow(50, 0, 40, 36 * 2)]
        [DataRow(50, 0, 35, 36 * 3)]
        [DataRow(50, 0, 30, 36 * 4)]
        [DataRow(50, 0, 25, 36 * 5)]
        [DataRow(50, 0, 20, 36 * 6)]
        [DataRow(40, 0, 15, 36 * 7)]
        [DataRow(50, 0, 10, 36 * 8)]
        [DataRow(50, 0, 05, 36 * 9)]
        [DataRow(50, 0, 00, 36 * 10)]
        [DataRow(100, 0, 95, 84 * 1)]
        [DataRow(100, 0, 90, 84 * 2)]
        [DataRow(100, 0, 85, 84 * 3)]
        [DataRow(100, 0, 80, 84 * 4)]
        [DataRow(100, 0, 75, 84 * 5)]
        [DataRow(100, 0, 70, 84 * 6)]
        [DataRow(100, 0, 65, 84 * 7)]
        [DataRow(100, 0, 60, 84 * 8)]
        [DataRow(100, 0, 55, 84 * 9)]
        [DataRow(100, 0, 50, 84 * 10)]
        [DataRow(100, 0, 45, 84 * 11)]
        [DataRow(100, 0, 40, 84 * 12)]
        [DataRow(100, 0, 35, 84 * 13)]
        [DataRow(100, 0, 30, 84 * 14)]
        [DataRow(100, 0, 25, 84 * 15)]
        [DataRow(100, 0, 20, 84 * 16)]
        [DataRow(100, 0, 15, 84 * 17)]
        [DataRow(100, 0, 10, 84 * 18)]
        [DataRow(100, 0, 05, 84 * 19)]
        [DataRow(100, 0, 00, 84 * 20)]
        public void BrakeTest_When_Host_Approaching_Slowing_Lead_Host_Should_Come_To_Stop_Without_Collision(int hostMph, int hostX, int leadCarMph, int leadCarX)
        {
            double updateIntervalTotalMilliseconds = 250;
            Constants constants = new Constants();
            var SUT = Vehicle.Factory.Create("host car", hostMph, hostX, 1, true);
            var lead = Vehicle.Factory.Create("lead car", leadCarMph, leadCarX, 1, true);
            int intervals = 0;
            
            while(SUT.Mph > 0)
            {
                var cellDistanceBetweenCars = lead.RearBumper - SUT.FrontBumper;
                var speedDiff = lead.Mph - SUT.Mph;
                var brake = SUT.CalculateVehicleBrakingForceToMaintainLeadPreference(lead, updateIntervalTotalMilliseconds);
                SUT.SubtractMph(brake);

                // advance both cars
                SUT.IncrementPositionChange(updateIntervalTotalMilliseconds);
                lead.IncrementPositionChange(updateIntervalTotalMilliseconds);
                cellDistanceBetweenCars = lead.RearBumper - SUT.FrontBumper;
                Assert.IsTrue(SUT.FrontBumper < lead.RearBumper);
                Assert.IsTrue(cellDistanceBetweenCars >= 3);

                // slow lead down incrementally (until zero is reached)
                lead.SubtractMph(constants.VEHICLE_GRADUAL_MPH_BRAKE_RATE);
                intervals++;
            }
            Assert.IsTrue(SUT.FrontBumper + 3 < lead.RearBumper);
        }

        [TestMethod]
        [DataRow(5, 0, 5, 3 * 3 + 2)]
        [DataRow(10, 0, 10, 8 * 3 + 2)]
        [DataRow(15, 0, 15, 12 * 3 + 2)]
        [DataRow(20, 0, 20, 16 * 3 + 2)]
        [DataRow(25, 0, 25, 20 * 3 + 2)]
        [DataRow(30, 0, 30, 24 * 3 + 2)]
        [DataRow(35, 0, 35, 28 * 3 + 2)]
        [DataRow(40, 0, 40, 32 * 3 + 2)]
        [DataRow(45, 0, 45, 36 * 3 + 2)]
        [DataRow(50, 0, 50, 40 * 3 + 2)]
        [DataRow(55, 0, 55, 44 * 3 + 2)]
        [DataRow(60, 0, 60, 48 * 3 + 2)]
        [DataRow(65, 0, 65, 52 * 3 + 2)]
        [DataRow(70, 0, 70, 56 * 3 + 2)]
        [DataRow(75, 0, 75, 60 * 3 + 2)]
        [DataRow(80, 0, 80, 64 * 3 + 2)]
        [DataRow(90, 0, 90, 72 * 3 + 2)]
        [DataRow(95, 0, 95, 76 * 3 + 2)]
        [DataRow(100, 0, 100, 80 * 3 + 2)]
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
