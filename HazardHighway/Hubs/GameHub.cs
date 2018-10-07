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

        #region queries

        public IEnumerable<VehicleModel> GetAllVehicles()
		{
			return _game.GetAllVehicles()
                .OrderByDescending(v => v.Name == "Player 1")
                .ThenBy(v => v.IsHazard)
                .ThenBy(v => v.Name);
		}

		public ChannelReader<VehicleModel> StreamVehicles()
		{
			return _game.StreamVehicles().AsChannelReader(10);
		}

        public string GetGameState()
        {
            return _game.GameState.ToString();
        }

        #endregion


        #region commands

        public async Task OpenGame()
		{
			await _game.OpenGame();
		}

		public async Task CloseGame()
		{
			await _game.CloseGame();
		}

		public async Task Reset(string level)
		{
			await _game.Reset(level);
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

        #endregion

    }
}
