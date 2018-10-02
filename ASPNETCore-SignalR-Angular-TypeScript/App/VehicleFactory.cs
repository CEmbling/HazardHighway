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
                // introduce hazards into the highway
                Vehicle.Factory.Create("Gawker 1",          mph:0, x:3170,      y:3, adaptiveCruiseOn: true),
                Vehicle.Factory.Create("Disabled Vehicle",  mph:0, x:3200,      y: 5, adaptiveCruiseOn: true),
                Vehicle.Factory.Create("Gawker 2",          mph: 0, x: 3170,    y: 7, adaptiveCruiseOn: true),

                // left lane
                Vehicle.Factory.Create("Toyota Prius",      mph:30, x: 5,   y: 3, adaptiveCruiseOn: false),
                Vehicle.Factory.Create("Toyota Camry",      mph:30, x: 50,  y: 3, adaptiveCruiseOn: false),
                Vehicle.Factory.Create("Lamborgini",        mph:30, x: 100, y: 3, adaptiveCruiseOn: false),
                Vehicle.Factory.Create("Ferrari 430",       mph:30, x: 250, y: 3, adaptiveCruiseOn: false),               

                // middle lane
                Vehicle.Factory.Create(constants.PLAYER1,   mph:30, x: 0,   y: 5, adaptiveCruiseOn: true),
                Vehicle.Factory.Create("Chevy Traverse",    mph:30, x: 21,  y: 5, adaptiveCruiseOn: false),
                Vehicle.Factory.Create("Ford Explorer",     mph:30, x: 40,  y: 5, adaptiveCruiseOn: false),
                Vehicle.Factory.Create("Toyota Highlander", mph:30, x: 160, y: 5, adaptiveCruiseOn: false),

                // right lane
                Vehicle.Factory.Create("Ford Escape",       mph:30, x:7,  y:7, adaptiveCruiseOn: false),
                Vehicle.Factory.Create("Chevy Malibu",      mph:30, x:30, y:7, adaptiveCruiseOn: false),
                Vehicle.Factory.Create("Subaru Forester",   mph:30, x:50, y:7, adaptiveCruiseOn: false),
                Vehicle.Factory.Create("Cal's Pigeon",      mph:30, x:70, y:7, adaptiveCruiseOn: false),
            };

            vehicles.ForEach(v => v.DrivingAdjective = v.AdaptiveCruiseOn ? terms.GetRandomTerm(TermList.Safe) : terms.GetRandomTerm(TermList.Unsafe));
            vehicles.ForEach(v => v.DrivingStatus = v.AdaptiveCruiseOn ? DrivingStatus.Cruising.ToString() : DrivingStatus.Driving.ToString());
            return vehicles;
        }
    }
}
