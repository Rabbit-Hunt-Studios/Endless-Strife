using System;
using System.Collections.Generic;
using TbsFramework.Grid;
using TbsFramework.Units;

namespace TbsFramework.Players.AI
{
    public class SubsequentUnitSelection : UnitSelection
    {
        public override IEnumerable<Unit> SelectNext(Func<List<Unit>> getUnits, CellGrid cellGrid)
        {
            foreach (var unit in getUnits())
            {
                yield return unit;
            }
        }
    }
}

