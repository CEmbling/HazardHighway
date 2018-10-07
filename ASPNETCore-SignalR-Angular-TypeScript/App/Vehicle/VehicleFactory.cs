using ASPNETCore_SignalR_Angular_TypeScript.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASPNETCore_SignalR_Angular_TypeScript.App
{
    public static class VehicleFactory
    {
        public static List<Vehicle> GetVehicles(Constants constants, Terminology terms)
        {
            var vehicles = new List<Vehicle>
            {
                // left lane
                Vehicle.Factory.Create("Toyota Prius",      mph:30, x: 5,   y: 3, adaptiveCruiseOn: false, drivingStatus: DrivingStatus.Driving),
                Vehicle.Factory.Create("Toyota Camry",      mph:30, x: 50,  y: 3, adaptiveCruiseOn: false, drivingStatus: DrivingStatus.Driving),
                Vehicle.Factory.Create("Toyota Highlander", mph:30, x: 100, y: 3, adaptiveCruiseOn: false, drivingStatus: DrivingStatus.Driving),
                Vehicle.Factory.Create("Toyota Corolla",    mph:30, x: 150, y: 3, adaptiveCruiseOn: false, drivingStatus: DrivingStatus.Driving),               

                // middle lane
                Vehicle.Factory.Create(constants.PLAYER1,   mph:30, x: 0,   y: 5, adaptiveCruiseOn: false, drivingStatus: DrivingStatus.Driving),
                Vehicle.Factory.Create("Ford Edge",         mph:30, x: 21,  y: 5, adaptiveCruiseOn: false, drivingStatus: DrivingStatus.Driving),
                Vehicle.Factory.Create("Ford Explorer",     mph:30, x: 40,  y: 5, adaptiveCruiseOn: false, drivingStatus: DrivingStatus.Driving),
                Vehicle.Factory.Create("Ford Escape",       mph:30, x: 160, y: 5, adaptiveCruiseOn: false, drivingStatus: DrivingStatus.Driving),

                // right lane
                Vehicle.Factory.Create("Chevy Traverse",    mph:30, x:7,  y:7, adaptiveCruiseOn: false, drivingStatus: DrivingStatus.Driving),
                Vehicle.Factory.Create("Chevy Malibu",      mph:30, x:30, y:7, adaptiveCruiseOn: false, drivingStatus: DrivingStatus.Driving),
                Vehicle.Factory.Create("Chevy Taho",        mph:30, x:50, y:7, adaptiveCruiseOn: false, drivingStatus: DrivingStatus.Driving),
                Vehicle.Factory.Create("Cal's Pigeon",      mph:30, x:70, y:7, adaptiveCruiseOn: false, drivingStatus: DrivingStatus.Driving),

                // introduce hazards into the highway
                Vehicle.Factory.Create("Gawker 1",          mph:0,  x:3100,   y:1, adaptiveCruiseOn: true, isHazard:true),
                Vehicle.Factory.Create("Gawker 2",          mph:0,  x:3100,   y:2,adaptiveCruiseOn: true, isHazard:true),
                Vehicle.Factory.Create("Gawker 3",          mph:0,  x:3100,   y:3,adaptiveCruiseOn: true, isHazard:true),
                Vehicle.Factory.Create("Gawker 4",          mph:0,  x:3100,   y:4,adaptiveCruiseOn: true, isHazard:true),
                Vehicle.Factory.Create("Disabled Vehicle",  mph:0,  x:3100,   y:5,adaptiveCruiseOn: true, isHazard:true),
                Vehicle.Factory.Create("Gawker 5",          mph:0,  x:3100,   y:6,adaptiveCruiseOn: true, isHazard:true),
                Vehicle.Factory.Create("Gawker 6",          mph:0,  x:3100,   y:7,adaptiveCruiseOn: true, isHazard:true),
                Vehicle.Factory.Create("Gawker 7",          mph:0,  x:3100,   y:8,adaptiveCruiseOn: true, isHazard:true),
            };

            vehicles.ForEach(v => v.DrivingAdjective = v.AdaptiveCruiseOn ? terms.GetRandomTerm(TermList.Safe) : terms.GetRandomTerm(TermList.Unsafe));
            vehicles.ForEach(v => v.DrivingStatus = v.AdaptiveCruiseOn ? DrivingStatus.Cruising.ToString() : DrivingStatus.Driving.ToString());
            return vehicles;
        }
        public static List<Vehicle> GetVehiclesForTestingStopping(Constants constants, Terminology terms)
        {
            var vehicles = new List<Vehicle>
            {
                // left lane
                Vehicle.Factory.Create("Toyota Prius",      mph:30, x: 5,   y: 3, adaptiveCruiseOn: true),
                Vehicle.Factory.Create("Toyota Camry",      mph:30, x: 50,  y: 3, adaptiveCruiseOn: true),
                Vehicle.Factory.Create("Toyota Highlander", mph:30, x: 100, y: 3, adaptiveCruiseOn: true),
                Vehicle.Factory.Create("Toyota Corolla",    mph:30, x: 150, y: 3, adaptiveCruiseOn: true),               

                // middle lane
                Vehicle.Factory.Create(constants.PLAYER1,   mph:30, x: 0,   y: 5, adaptiveCruiseOn: true),
                Vehicle.Factory.Create("Ford Edge",         mph:30, x: 21,  y: 5, adaptiveCruiseOn: true),
                Vehicle.Factory.Create("Ford Explorer",     mph:30, x: 40,  y: 5, adaptiveCruiseOn: true),
                Vehicle.Factory.Create("Ford Escape",       mph:30, x: 160, y: 5, adaptiveCruiseOn: true),

                // right lane
                Vehicle.Factory.Create("Chevy Traverse",    mph:30, x:7,  y:7, adaptiveCruiseOn: true),
                Vehicle.Factory.Create("Chevy Malibu",      mph:30, x:30, y:7, adaptiveCruiseOn: true),
                Vehicle.Factory.Create("Chevy Taho",        mph:30, x:50, y:7, adaptiveCruiseOn: true),
                Vehicle.Factory.Create("Cal's Pigeon",      mph:30, x:70, y:7, adaptiveCruiseOn: true),

                // introduce hazards into the highway
                Vehicle.Factory.Create("Gawker 1",          mph:0,  x:1000,   y:1,adaptiveCruiseOn: true, isHazard:true),
                Vehicle.Factory.Create("Gawker 2",          mph:0,  x:1000,   y:2,adaptiveCruiseOn: true, isHazard:true),
                Vehicle.Factory.Create("Gawker 3",          mph:0,  x:1000,   y:3,adaptiveCruiseOn: true, isHazard:true),
                Vehicle.Factory.Create("Gawker 4",          mph:0,  x:1000,   y:4,adaptiveCruiseOn: true, isHazard:true),
                Vehicle.Factory.Create("Disabled Vehicle",  mph:0,  x:1000,   y:5,adaptiveCruiseOn: true, isHazard:true),
                Vehicle.Factory.Create("Gawker 5",          mph:0,  x:1000,   y:6,adaptiveCruiseOn: true, isHazard:true),
                Vehicle.Factory.Create("Gawker 6",          mph:0,  x:1000,   y:7,adaptiveCruiseOn: true, isHazard:true),
                Vehicle.Factory.Create("Gawker 7",          mph:0,  x:1000,   y:8,adaptiveCruiseOn: true, isHazard:true),
            };

            vehicles.ForEach(v => v.DrivingAdjective = v.AdaptiveCruiseOn ? terms.GetRandomTerm(TermList.Safe) : terms.GetRandomTerm(TermList.Unsafe));
            vehicles.ForEach(v => v.DrivingStatus = v.AdaptiveCruiseOn ? DrivingStatus.Cruising.ToString() : DrivingStatus.Driving.ToString());
            vehicles.ForEach(v => v.DrivingStatus = v.Mph == 0 ? DrivingStatus.Stopped.ToString() : v.DrivingStatus);
            return vehicles;
        }
    }
}
