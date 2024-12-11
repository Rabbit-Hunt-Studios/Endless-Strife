using System;
using System.Collections.Generic;
using System.Linq;
using TbsFramework.Grid;
using TbsFramework.Units;

namespace TbsFramework.Players.AI
{
    public class MovementFreedomUnitSelection : UnitSelection
    {
        private HashSet<Unit> alreadySelected = new HashSet<Unit>();
        public override IEnumerable<Unit> SelectNext(Func<List<Unit>> getUnits, CellGrid cellGrid)
        {
            var units = getUnits();
            while (alreadySelected.Count < units.Count)
            {
                var nextUnit = units.Where(u => !alreadySelected.Contains(u))
                                    .OrderByDescending(u => u.Cell.GetNeighbours(cellGrid.Cells)
                                                                  .Where(u.IsCellTraversable)
                                                                  .Count())
                                    .First();

                alreadySelected.Add(nextUnit);
                yield return nextUnit;
            }

            alreadySelected = new HashSet<Unit>();
        }
    }
}