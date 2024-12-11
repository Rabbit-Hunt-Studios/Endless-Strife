using System.Collections.Generic;
using System.Linq;
using TbsFramework.Network;
using TbsFramework.Players;
using UnityEngine;

namespace TbsFramework.Grid.GameResolvers
{
    /// <summary>
    /// Represents a game end condition which triggers when the host of the game disconnects.
    /// </summary>
    /// <remarks>
    /// This class extends from <see cref="GameEndCondition"/> and overrides the CheckCondition method to determine
    /// if the game should end based on the host's connection status. It listens to the network connection's PlayerLeftRoom event
    /// to set a flag when the host disconnects.
    /// </remarks>
    public class HostDisconnedctedCondition : GameEndCondition
    {
        [SerializeField] private NetworkConnection _networkConnection;
        private bool _hostLeft = false;

        private void Start()
        {
            _networkConnection.PlayerLeftRoom += (_, player) => { _hostLeft = player.IsHost; };
        }

        public override GameResult CheckCondition(CellGrid cellGrid)
        {
            if (_hostLeft)
            {
                return new GameResult(isFinished: true,
                    FindObjectsOfType<Player>().ToList().Where(p => p is HumanPlayer).Select(p => p.PlayerNumber).ToList(),
                    FindObjectsOfType<Player>().ToList().Where(p => p is RemotePlayer).Select(p => p.PlayerNumber).ToList());
            }
            else
            {
                return new GameResult(isFinished: false, new List<int>(), new List<int>());
            }
        }
    }
}

