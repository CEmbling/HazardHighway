using ASPNETCore_SignalR_Angular_TypeScript.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASPNETCore_SignalR_Angular_TypeScript.App
{
    public class GameScenario
    {
        public ConcurrentDictionary<string, Vehicle> Vehicles { get; set; } = new ConcurrentDictionary<string, Vehicle>();
        public string Highway { get; set; }
        public int EndX { get; set; }

        public static class Factory
        {
            public static GameScenario Create(GameLevel level, Constants constants, Terminology terms)
            {
                var vehicles = new List<Vehicle>();
                GameScenario scenario = new GameScenario();
                switch (level)
                {
                    case GameLevel.Level1:
                    default: // level 1
                        vehicles = VehicleFactory.GetTrainingVehicles(constants, terms);
                        scenario.EndX = 5000;
                        break;
                    case GameLevel.Level2:
                        vehicles = VehicleFactory.GetVehicles(constants, terms);
                        scenario.EndX = 5000;
                        break;
                    case GameLevel.Level3:
                        vehicles = VehicleFactory.GetVehicles(constants, terms);
                        scenario.EndX = 3600;
                        break;
                }
                
                vehicles.ForEach(v => scenario.Vehicles.TryAdd(v.Name, v));

                return scenario;
            }
        }
    }
}
