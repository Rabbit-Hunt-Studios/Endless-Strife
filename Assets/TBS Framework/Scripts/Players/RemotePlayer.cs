using TbsFramework.Grid;
using TbsFramework.Grid.GridStates;
using TbsFramework.Network;
using UnityEngine;

namespace TbsFramework.Players
{
    public class RemotePlayer : Player
    {
        public NetworkConnection NetworkConnection { get; set; }

        private bool _playerLeft;

        public override void Initialize(CellGrid cellGrid)
        {
            base.Initialize(cellGrid);
            NetworkConnection.PlayerLeftRoom += (sender, networkUser) => 
            {
                if (networkUser.CustomProperties.TryGetValue("playerNumber", out string leavingPlayerNumber) && PlayerNumber.Equals(int.Parse(leavingPlayerNumber)))
                {
                    Debug.Log("Remote player left");
                    _playerLeft = true;

                    if (NetworkConnection.IsHost && PlayerNumber.Equals(cellGrid.CurrentPlayerNumber))
                    {
                        cellGrid.EndTurn();
                    }
                }
            };
        }

        public override void Play(CellGrid cellGrid)
        {
            cellGrid.cellGridState = new CellGridStateRemotePlayerTurn(cellGrid);
            if (NetworkConnection.IsHost && _playerLeft)
            {
                cellGrid.EndTurn();
            }
        }
    }
}

