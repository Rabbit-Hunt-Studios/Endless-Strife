using System.Linq;
using TbsFramework.Grid;
using TbsFramework.Units.Abilities;

namespace TbsFramework.HOMMExample
{
    public class AutoTurnFinish : Ability
    {
        public override void Display(CellGrid cellGrid)
        {
            var canPerformAction = UnitReference.GetComponents<Ability>()
                                                .ToList()
                                                .Select(a => a.CanPerform(cellGrid))
                                                .Aggregate((result, next) => result || next);
            if (!canPerformAction)
            {
                cellGrid.EndTurn();
            }
        }
    }
}

