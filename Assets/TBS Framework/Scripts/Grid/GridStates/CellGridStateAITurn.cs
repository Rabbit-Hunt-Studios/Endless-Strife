using System.Collections.Generic;
using TbsFramework.Cells;
using TbsFramework.Players;
using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.Grid.GridStates
{
    public class CellGridStateAITurn : CellGrid.CellGridState
    {
        private Dictionary<Cell, DebugInfo> cellDebugInfo;
        public Dictionary<Cell, DebugInfo> CellDebugInfo
        {
            get
            {
                return cellDebugInfo;
            }
            set
            {
                cellDebugInfo = value;
                if (value != null && AIPlayer.DebugMode)
                {
                    foreach (Cell cell in cellDebugInfo.Keys)
                    {
                        cell.SetColor(cellDebugInfo[cell].Color);
                    }
                }
            }
        }
        public Dictionary<Unit, string> UnitDebugInfo { private get; set; }

        private AIPlayer AIPlayer;

        public CellGridStateAITurn(CellGrid cellGrid, AIPlayer AIPlayer) : base(cellGrid)
        {
            this.AIPlayer = AIPlayer;
        }

        public override void OnCellDeselected(Cell cell)
        {
            base.OnCellDeselected(cell);
            if (AIPlayer.DebugMode && CellDebugInfo != null && CellDebugInfo.ContainsKey(cell))
            {
                cell.SetColor(CellDebugInfo[cell].Color);
            }
        }

        public override void OnCellSelected(Cell cell)
        {
            base.OnCellSelected(cell);
        }

        public override void OnCellClicked(Cell cell)
        {
            if (AIPlayer.DebugMode && CellDebugInfo != null && CellDebugInfo.ContainsKey(cell))
            {
                Debug.Log(CellDebugInfo[cell].Metadata);
            }
        }
        public override void OnUnitClicked(Unit unit)
        {
            if (AIPlayer.DebugMode && UnitDebugInfo != null && UnitDebugInfo.ContainsKey(unit))
            {
                Debug.Log(UnitDebugInfo[unit]);
            }
        }

        public override void OnStateEnter()
        {
        }

        public override void OnStateExit()
        {
        }
    }
}