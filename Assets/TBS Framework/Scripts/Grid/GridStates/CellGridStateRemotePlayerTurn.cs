using UnityEngine;

namespace TbsFramework.Grid.GridStates
{
    public class CellGridStateRemotePlayerTurn : CellGrid.CellGridState
    {
        public CellGridStateRemotePlayerTurn(CellGrid cellGrid) : base(cellGrid)
        {
        }

        public override void EndTurn(bool isNetworkInvoked)
        {
            base.EndTurn(isNetworkInvoked);
        }
    }
}

