using System.Collections;
using System.Collections.Generic;
using TbsFramework.Cells;
using TbsFramework.Grid;
using TbsFramework.Grid.GridStates;
using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.HOMMExample
{
    public class FireballSpell : SpellAbility
    {
        public int Range;
        public int Damage;

        List<Cell> inRange;
        public Cell SelectedCell { get; set; }

        public override IEnumerator Act(CellGrid cellGrid, bool isNetworkInvoked = false)
        {
            if (CanPerform(cellGrid))
            {
                if (inRange == null)
                {
                    inRange = cellGrid.Cells.FindAll(c => c.GetDistance(SelectedCell) <= Range);
                }

                Unit tempUnit = null;
                foreach (var cell in inRange)
                {
                    foreach (Unit unit in new List<Unit>(cell.CurrentUnits))
                    {
                        unit.DefendHandler(UnitReference, Damage);
                        if (unit != null)
                        {
                            tempUnit = unit;
                        }
                    }
                }

                if (tempUnit != null)
                {
                    UnitReference.MarkAsAttacking(tempUnit);
                }
            }
            yield return base.Act(cellGrid, false);
        }

        public override void OnCellSelected(Cell cell, CellGrid cellGrid)
        {
            if (cell == null || cell.CurrentUnits.Count > 0 && (cell.CurrentUnits[0] as HOMMUnit).IsHero)
            {
                return;
            }

            inRange = cellGrid.Cells.FindAll(c => c.GetDistance(cell) <= Range);
            inRange.ForEach(c =>
            {
                c.MarkAsHighlighted();
                if (c.CurrentUnits.Count > 0)
                {
                    c.CurrentUnits[0].MarkAsReachableEnemy();
                }
            });
        }

        public override void OnCellDeselected(Cell cell, CellGrid cellGrid)
        {
            if (cell == null || cell.CurrentUnits.Count > 0 && (cell.CurrentUnits[0] as HOMMUnit).IsHero)
            {
                return;
            }
            inRange.ForEach(c =>
            {
                c.UnMark();
                if (c.CurrentUnits.Count > 0)
                {
                    if (cellGrid.GetCurrentPlayerUnits().Contains(c.CurrentUnits[0]))
                    {
                        c.CurrentUnits[0].MarkAsFriendly();
                    }
                    else
                    {
                        c.CurrentUnits[0].UnMark();
                    }
                }
            });
        }

        public override void OnUnitHighlighted(Unit unit, CellGrid cellGrid)
        {
            OnCellSelected(unit.Cell, cellGrid);
        }

        public override void OnUnitDehighlighted(Unit unit, CellGrid cellGrid)
        {
            OnCellDeselected(unit.Cell, cellGrid);
        }

        public override void OnCellClicked(Cell cell, CellGrid cellGrid)
        {
            if (cell == null || cell.CurrentUnits.Count > 0 && (cell.CurrentUnits[0] as HOMMUnit).IsHero)
            {
                return;
            }
            SelectedCell = cell;

            StartCoroutine(Execute(cellGrid,
                _ => cellGrid.cellGridState = new CellGridStateBlockInput(cellGrid),
                _ => cellGrid.cellGridState = new CellGridStateWaitingForInput(cellGrid)));
        }

        public override void OnUnitClicked(Unit unit, CellGrid cellGrid)
        {
            OnCellClicked(unit.Cell, cellGrid);
        }

        public override void OnTurnStart(CellGrid cellGrid)
        {
            inRange = null;
            SelectedCell = null;
        }

        public override void CleanUp(CellGrid cellGrid)
        {
            base.CleanUp(cellGrid);
            OnCellDeselected(null, cellGrid);

            inRange.ForEach(c =>
            {
                c.UnMark();
                if (c.CurrentUnits.Count > 0)
                {
                    if (cellGrid.GetCurrentPlayerUnits().Contains(c.CurrentUnits[0]))
                    {
                        c.CurrentUnits[0].MarkAsFriendly();
                    }
                    else
                    {
                        c.CurrentUnits[0].UnMark();
                    }
                }
            });
        }

        public override string GetDetails()
        {
            return string.Format("{0} Mana\n{1} Damage", ManaCost, Damage);
        }

        public override IEnumerator Apply(CellGrid cellGrid, IDictionary<string, string> actionParams, bool isNetworkInvoked)
        {
            SelectedCell = cellGrid.Cells.Find(c => c.OffsetCoord.Equals(new UnityEngine.Vector2(float.Parse(actionParams["selected_cell_x"]), float.Parse(actionParams["selected_cell_y"]))));
            yield return StartCoroutine(RemoteExecute(cellGrid));
        }

        public override IDictionary<string, string> Encapsulate()
        {
            var actionParams = new Dictionary<string, string>();

            actionParams.Add("selected_cell_x", SelectedCell.OffsetCoord.x.ToString());
            actionParams.Add("selected_cell_y", SelectedCell.OffsetCoord.y.ToString());

            return actionParams;
        }
    }
}
