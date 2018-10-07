using ASPNETCore_SignalR_Angular_TypeScript.App;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace UnitTests
{
    [TestClass]
    public class AccelerationTests
    {

        [TestMethod]
        [DataRow(5, 0, 5, 4 * 3 + 5)]
        [DataRow(10, 0, 10, 8 * 3 + 5)]
        [DataRow(15, 0, 15, 12 * 3 + 5)]
        [DataRow(20, 0, 20, 16 * 3 + 5)]
        [DataRow(25, 0, 25, 20 * 3 + 5)]
        [DataRow(30, 0, 30, 24 * 3 + 5)]
        [DataRow(35, 0, 35, 28 * 3 + 5)]
        [DataRow(40, 0, 40, 32 * 3 + 5)]
        [DataRow(45, 0, 45, 36 * 3 + 5)]
        [DataRow(50, 0, 50, 40 * 3 + 5)]
        [DataRow(55, 0, 55, 44 * 3 + 5)]
        [DataRow(60, 0, 60, 48 * 3 + 5)]
        [DataRow(65, 0, 65, 52 * 3 + 5)]
        [DataRow(70, 0, 70, 56 * 3 + 5)]
        [DataRow(75, 0, 75, 60 * 3 + 5)]
        [DataRow(80, 0, 80, 64 * 3 + 5)]
        [DataRow(85, 0, 85, 68 * 3 + 5)]
        [DataRow(90, 0, 90, 72 * 3 + 5)]
        [DataRow(95, 0, 95, 76 * 3 + 5)]
        public void AccelerationTest_When_Host_Approaching_Moving_Lead_With_Ample_Distance_Host_Should_Accelerate(int hostMph, int hostX, int leadCarMph, int leadCarX)
        {
            double updateIntervalTotalMilliseconds = 250;
            Constants constants = new Constants();
            var SUT = Vehicle.Factory.Create("host car", hostMph, hostX, 1, adaptiveCruiseOn: true, drivingStatus:DrivingStatus.Driving);
            SUT.AddAdaptiveCruiseMph(constants.VEHICLE_MPH_ACCELERATION_INCREMENT_RATE);
            var lead = Vehicle.Factory.Create("lead car", leadCarMph, leadCarX, 1, true, drivingStatus: DrivingStatus.Driving);
            var accelerate = SUT.CalculateVehicleAccelerationForceToMaintainLeadPreference(lead, updateIntervalTotalMilliseconds);
            Assert.AreEqual(5, accelerate);
        }

        [TestMethod]
        [DataRow(5, 0, 0, 4 + 15 + 2 * 2)]      // 4(car offset) + 5 mph safe Accel. + cellsPerInstance*2 instances
        [DataRow(10, 0, 0, 4 + 26 + 4 * 2)]     // 4(car offset) + 10 mph safe Accel. + cellsPerInstance*2 instances
        //[DataRow(10, 0, 0, 15 * 3 + 5)]
        //[DataRow(15, 0, 0, 26 * 3 + 5)]
        //[DataRow(20, 0, 0, 39 * 3 + 5)]
        //[DataRow(25, 0, 0, 55 * 3 + 5)]
        //[DataRow(30, 0, 0, 74 * 3 + 5)]
        //[DataRow(35, 0, 0, 95 * 3 + 5)]
        //[DataRow(40, 0, 0, 118 * 3 + 5)]
        //[DataRow(45, 0, 0, 144 * 3 + 5)]
        //[DataRow(50, 0, 0, 172 * 3 + 5)]
        //[DataRow(55, 0, 0, 207 * 3 + 5)]
        //[DataRow(60, 0, 0, 239 * 3 + 5)]
        //[DataRow(65, 0, 0, 275 * 3 + 5)]
        //[DataRow(70, 0, 0, 315 * 3 + 5)]
        //[DataRow(75, 0, 0, 356 * 3 + 5)]
        //[DataRow(80, 0, 0, 400 * 3 + 5)]
        //[DataRow(90, 0, 0, 494 * 3 + 5)]
        //[DataRow(95, 0, 0, 545 * 3 + 5)]
        public void AccelerationTest_When_Host_Approaching_Stopped_Lead_Without_Ample_Distance_Host_Should_NOT_Accelerate(int hostMph, int hostX, int leadCarMph, int leadCarX)
        {
            double updateIntervalTotalMilliseconds = 250;
            Constants constants = new Constants();
            var SUT = Vehicle.Factory.Create("host car", hostMph, hostX, 1, true, drivingStatus: DrivingStatus.Driving);
            var lead = Vehicle.Factory.Create("lead car", leadCarMph, leadCarX, 1, true, drivingStatus: DrivingStatus.Driving);
            SUT.AddAdaptiveCruiseMph(constants.VEHICLE_MPH_ACCELERATION_INCREMENT_RATE);

            var accelerate = SUT.CalculateVehicleAccelerationForceToMaintainLeadPreference(lead, updateIntervalTotalMilliseconds);

            Assert.AreEqual(0, accelerate);
        }

        [TestMethod]
        [DataRow(5, 0, 0, 19 + 2*2 + 1)]    // 5 mph safe Acce. + cellsPerInstance*2 instances + 1 cell
        [DataRow(10, 0, 0, 34 + 4*2 + 1)]
        //[DataRow(15, 0, 0, 51)]
        //[DataRow(20, 0, 0, 71)]
        //[DataRow(25, 0, 0, 94)]
        //[DataRow(30, 0, 0, 117)]
        //[DataRow(35, 0, 0, 144)]
        //[DataRow(40, 0, 0, 174)]
        //[DataRow(45, 0, 0, 206)]
        //[DataRow(50, 0, 0, 245)]
        //[DataRow(55, 0, 0, 281)]
        //[DataRow(60, 0, 0, 319)]
        //[DataRow(65, 0, 0, 363)]
        //[DataRow(70, 0, 0, 408)]
        //[DataRow(75, 0, 0, 456)]
        //[DataRow(80, 0, 0, 506)]
        //[DataRow(85, 0, 0, 558)]
        //[DataRow(90, 0, 0, 611)]
        //[DataRow(95, 0, 0, 668)]
        public void AccelerationTest_When_Host_Approaching_Stopped_Lead_With_Ample_Distance_Host_Should_Accelerate(int hostMph, int hostX, int leadCarMph, int leadCarX)
        {
            double updateIntervalTotalMilliseconds = 250;
            Constants constants = new Constants();
            var SUT = Vehicle.Factory.Create("host car", hostMph, hostX, 1, true, drivingStatus: DrivingStatus.Driving);
            var lead = Vehicle.Factory.Create("lead car", leadCarMph, leadCarX, 1, true, drivingStatus: DrivingStatus.Driving);
            SUT.AddAdaptiveCruiseMph(constants.VEHICLE_MPH_ACCELERATION_INCREMENT_RATE);

            var accelerate = SUT.CalculateVehicleAccelerationForceToMaintainLeadPreference(lead, updateIntervalTotalMilliseconds);

            Assert.AreEqual(5, accelerate);
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
        public void AccelerationTest_When_Host_Tailing_Lead_Too_Close_Host_Should_Not_Accelerate(int hostMph, int hostX, int leadCarMph, int leadCarX)
        {
            double updateIntervalTotalMilliseconds = 250;
            Constants constants = new Constants();
            var SUT = Vehicle.Factory.Create("host car", hostMph, hostX, 1, true);
            var lead = Vehicle.Factory.Create("lead car", leadCarMph, leadCarX, 1, true);
            var accelerate = SUT.CalculateVehicleAccelerationForceToMaintainLeadPreference(lead, updateIntervalTotalMilliseconds);
            Assert.AreEqual(0, accelerate);
        }        
    }
}
