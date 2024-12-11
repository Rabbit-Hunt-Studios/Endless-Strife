using TbsFramework.Grid;
using TbsFramework.Units;

namespace TbsFramework.Players.AI.Evaluators
{
    public class HPUnitEvaluator : UnitEvaluator
    {
        public override float Evaluate(Unit unitToEvaluate, Unit evaluatingUnit, Player currentPlayer, CellGrid cellGrid)
        {
            return (unitToEvaluate.TotalHitPoints - (float)unitToEvaluate.HitPoints) / unitToEvaluate.TotalHitPoints;
        }
    }
}
