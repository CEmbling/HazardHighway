using ASPNETCore_SignalR_Angular_TypeScript.App;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static ASPNETCore_SignalR_Angular_TypeScript.Game;

namespace ASPNETCore_SignalR_Angular_TypeScript.Models
{
    public class QueuedVehicleAction
    {
        public string Name { get; set; }
        public VehicleAction VehicleAction { get; set; } 
        public bool Handled { get; set; }

        public static class Factory
        {
            public static QueuedVehicleAction Create(string name, VehicleAction vehicleAction)
            {
                return new QueuedVehicleAction()
                {
                    Name = name,
                    VehicleAction = vehicleAction,
                    Handled = false
                };
            }
        }
    }
}
