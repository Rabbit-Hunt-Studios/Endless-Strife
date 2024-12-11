using System.Collections;
using System.Linq;
using TbsFramework.Grid;
using TbsFramework.Units;
using TbsFramework.Units.Abilities;

namespace TbsFramework.Example1
{
    public class AoeBuffAbility : Ability
    {
        public Buff Buff;
        public int Range = 1;
        public bool ApplyToSelf = false;

        public override IEnumerator Act(CellGrid cellGrid, bool isNetworkInvoked = false)
        {
            var myUnits = cellGrid.GetCurrentPlayerUnits();
            var unitsInRange = myUnits.Where(u => u.Cell.GetDistance(UnitReference.Cell) <= Range);

            foreach (var unit in unitsInRange)
            {
                if (unit.Equals(UnitReference) && !ApplyToSelf)
                {
                    continue;
                }
                unit.AddBuff(Buff);
            }

            yield return null;
        }

        public override void OnTurnStart(CellGrid cellGrid)
        {
            StartCoroutine(Act(cellGrid, false));
        }
    }
}