using System.Collections.Generic;
using TbsFramework.Cells;
using TbsFramework.Players;
using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.Grid.GridStates
{
    public class CellGridStateRLTurn : CellGrid.CellGridState
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
                if (value != null && RLPlayer.debugMode)
                {
                    foreach (Cell cell in cellDebugInfo.Keys)
                    {
                        cell.SetColor(cellDebugInfo[cell].Color);
                    }
                }
            }
        }
        public Dictionary<Unit, string> UnitDebugInfo { private get; set; }

        private RLPlayer RLPlayer;

        public CellGridStateRLTurn(CellGrid cellGrid, RLPlayer RLPlayer) : base(cellGrid)
        {
            this.RLPlayer = RLPlayer;
        }

        public override void OnCellDeselected(Cell cell)
        {
            base.OnCellDeselected(cell);
            if (RLPlayer.debugMode && CellDebugInfo != null && CellDebugInfo.ContainsKey(cell))
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
            if (RLPlayer.debugMode && CellDebugInfo != null && CellDebugInfo.ContainsKey(cell))
            {
                Debug.Log(CellDebugInfo[cell].Metadata);
            }
        }
        public override void OnUnitClicked(Unit unit)
        {
            if (RLPlayer.debugMode && UnitDebugInfo != null && UnitDebugInfo.ContainsKey(unit))
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