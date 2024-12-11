using System.Collections.Generic;
using System.Linq;
using TbsFramework.Network;
using TbsFramework.Players;
using UnityEngine;

namespace TbsFramework.Grid.GameResolvers
{
    /// <summary>
    /// Represents a game end condition which is triggered when only one player remains connected.
    /// </summary>
    /// <remarks>
    /// This class listens to player connection and disconnection events to maintain 
    /// a set of currently connected players. The game ends when only one player is left in the network session.
    /// Inherits from <see cref="GameEndCondition"/>.
    /// </remarks>
    public class PlayerDisconnectedCondition : GameEndCondition
    {
        [SerializeField] private NetworkConnection _networkConnection;
        private HashSet<NetworkUser> _networkPlayers = new HashSet<NetworkUser>();

        private void Start()
        {
            _networkConnection.PlayerLeftRoom += (_, player) => { _networkPlayers.Remove(player); };
            _networkConnection.PlayerEnteredRoom += (_, player) => { _networkPlayers.Add(player); };
            _networkConnection.RoomJoined += (sender, roomData) => {
                foreach(var user in roomData.Users)
                {
                    _networkPlayers.Add(user);
                }
            };
        }

        public override GameResult CheckCondition(CellGrid cellGrid)
        {
            if(_networkPlayers.Count == 1)
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

