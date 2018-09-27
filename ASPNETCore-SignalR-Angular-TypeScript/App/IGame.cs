using ASPNETCore_SignalR_Angular_TypeScript.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASPNETCore_SignalR_Angular_TypeScript.App
{
    interface IGame
    {
        GameState GameState { get; }
        IEnumerable<Vehicle> GetAllVehicles();
        IObservable<Vehicle> StreamVehicles();
        Task OpenGame();
        Task CloseGame();
        Task Reset();
        Task ToggleAdaptiveCruise();
        Task TurnLeft();
        Task TurnRight();
        Task IncreaseSpeed();
        Task DecreaseSpeed();
    }
}
