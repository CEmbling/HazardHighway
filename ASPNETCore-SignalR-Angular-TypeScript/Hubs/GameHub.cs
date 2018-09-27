using ASPNETCore_SignalR_Angular_TypeScript.Models;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ASPNETCore_SignalR_Angular_TypeScript.Hubs
{

	/// <summary>
	/// Vehicle hub 
	/// </summary>
    public class GameHub : Hub
    {
		private Game _game;

		public GameHub(Game game)
		{
			this._game = game;
		}

		public IEnumerable<Vehicle> GetAllVehicles()
		{
			return _game.GetAllVehicles();
		}

		public ChannelReader<Vehicle> StreamVehicles()
		{
			return _game.StreamVehicles().AsChannelReader(10);
		}

		public string GetGameState()
		{
			return _game.GameState.ToString();
		}

		public async Task OpenGame()
		{
			await _game.OpenGame();
		}

		public async Task CloseGame()
		{
			await _game.CloseGame();
		}

		public async Task Reset()
		{
			await _game.Reset();
		}

        public async Task ToggleAdaptiveCruise()
        {
            await _game.ToggleAdaptiveCruise();
        }

        public async Task TurnRight()
        {
            await _game.TurnRight();
        }

        public async Task TurnLeft()
        {
            await _game.TurnLeft();
        }

        public async Task IncreaseSpeed()
        {
            await _game.IncreaseSpeed();
        }

        public async Task DecreaseSpeed()
        {
            await _game.DecreaseSpeed();
        }

    }
}
