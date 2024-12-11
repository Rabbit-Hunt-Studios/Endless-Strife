using System;
using System.Collections.Generic;
using System.Linq;
using TbsFramework.Grid;
using TbsFramework.Players.AI;
using TbsFramework.Units;

namespace TbsFramework.Example4
{
    public class AdvWrsUnitSelection : UnitSelection
    {
        private HashSet<Unit> alreadySelected = new HashSet<Unit>();
        public override IEnumerable<Unit> SelectNext(Func<List<Unit>> getUnits, CellGrid cellGrid)
        {
            var units = getUnits();
            while (alreadySelected.Count < units.Count)
            {
                Unit nextUnit = null;
                var structures = units.Where(u => !alreadySelected.Contains(u) && (u as AdvWrsUnit).isStructure && u.Cell.CurrentUnits.Count == 1).ToList();
                nextUnit = units.Where(u => !alreadySelected.Contains(u) && (u as AdvWrsUnit).isStructure && u.Cell.CurrentUnits.Count == 1).FirstOrDefault();

                if (nextUnit == null)
                {
                    nextUnit = units.Where(u => !alreadySelected.Contains(u) && !(u as AdvWrsUnit).isStructure)
                                    .OrderByDescending(u => u.Cell.GetNeighbours(cellGrid.Cells)
                                                                  .Where(u.IsCellTraversable)
                                                                  .Count())
                                                                  .FirstOrDefault();
                }

                if (nextUnit == null)
                {
                    break;
                }

                alreadySelected.Add(nextUnit);
                yield return nextUnit;
            }

            alreadySelected = new HashSet<Unit>();
        }
    }
}

