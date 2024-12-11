using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TbsFramework.Cells;
using TbsFramework.Grid;
using TbsFramework.Grid.GridStates;
 
namespace TbsFramework.Units.Abilities
{
    public class MoveAbility : Ability
    {
        public Cell Destination { get; set; }
        private IList<Cell> currentPath;
        public HashSet<Cell> availableDestinations;

        public override IEnumerator Act(CellGrid cellGrid, bool isNetworkInvoked = false)
        {
            if (UnitReference.ActionPoints > 0 && availableDestinations.Contains(Destination))
            {
                var path = UnitReference.FindPath(cellGrid.Cells, Destination);
                yield return UnitReference.Move(Destination, path);
            }
            yield return base.Act(cellGrid, isNetworkInvoked);
        }
        public override void Display(CellGrid cellGrid)
        {
            if (UnitReference.ActionPoints > 0)
            {
                foreach (var cell in availableDestinations)
                {
                    cell.MarkAsReachable();
                }
            }
        }

        public override void OnUnitClicked(Unit unit, CellGrid cellGrid)
        {
            if (cellGrid.GetCurrentPlayerUnits().Contains(unit))
            {
                cellGrid.cellGridState = new CellGridStateAbilitySelected(cellGrid, unit, unit.GetComponents<Ability>().ToList());
            }
        }
        public override void OnCellClicked(Cell cell, CellGrid cellGrid)
        {
            if (availableDestinations.Contains(cell))
            {
                Destination = cell;
                currentPath = null;
                StartCoroutine(HumanExecute(cellGrid));
            }
            else
            {
                cellGrid.cellGridState = new CellGridStateWaitingForInput(cellGrid);
            }
        }
        public override void OnCellSelected(Cell cell, CellGrid cellGrid)
        {
            if (UnitReference.ActionPoints > 0 && availableDestinations.Contains(cell))
            {
                currentPath = UnitReference.FindPath(cellGrid.Cells, cell);
                foreach (var c in currentPath)
                {
                    c.MarkAsPath();
                }
            }
        }
        public override void OnCellDeselected(Cell cell, CellGrid cellGrid)
        {
            if (UnitReference.ActionPoints > 0 && availableDestinations.Contains(cell))
            {
                if(currentPath == null)
                {
                    return;
                }
                foreach(var c in currentPath)
                {
                    c.MarkAsReachable();
                }
            }
        }

        public override void OnAbilitySelected(CellGrid cellGrid)
        {
            UnitReference.CachePaths(cellGrid.Cells);
            availableDestinations = UnitReference.GetAvailableDestinations(cellGrid.Cells);
        }

        public override void CleanUp(CellGrid cellGrid)
        {
            foreach (var cell in availableDestinations)
            {
                cell.UnMark();
            }
        }

        public override bool CanPerform(CellGrid cellGrid)
        {
            return UnitReference.ActionPoints > 0 && UnitReference.GetAvailableDestinations(cellGrid.Cells).Count > 0;
        }

        public override IDictionary<string, string> Encapsulate()
        {
            var actionParams = new Dictionary<string, string>();

            actionParams.Add("destination_x", Destination.OffsetCoord.x.ToString());
            actionParams.Add("destination_y", Destination.OffsetCoord.y.ToString());

            return actionParams;
        }

        public override IEnumerator Apply(CellGrid cellGrid, IDictionary<string, string> actionParams, bool isNetworkInvoked = false)
        {
            var actionDestination = cellGrid.Cells.Find(c => c.OffsetCoord.Equals(new UnityEngine.Vector2(float.Parse(actionParams["destination_x"]), float.Parse(actionParams["destination_y"]))));
            Destination = actionDestination;
            yield return StartCoroutine(RemoteExecute(cellGrid));
        }
    }
}
