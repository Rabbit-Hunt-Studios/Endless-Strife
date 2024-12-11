using System.Linq;
using TbsFramework.Grid;
using TbsFramework.Units;


namespace TbsFramework.Players.AI.Evaluators
{
    public class DamageUnitEvaluator : UnitEvaluator
    {
        private float topDamage;

        public override void Precalculate(Unit evaluatingUnit, Player currentPlayer, CellGrid cellGrid)
        {
            var enemyUnits = cellGrid.GetEnemyUnits(currentPlayer);
            var enemiesInRange = enemyUnits.Where(u => evaluatingUnit.Cell.GetDistance(u.Cell) <= evaluatingUnit.AttackRange);
            topDamage = enemiesInRange.Select(u => evaluatingUnit.DryAttack(u))
                                          .DefaultIfEmpty()
                                          .Max();
        }

        public override float Evaluate(Unit unitToEvaluate, Unit evaluatingUnit, Player currentPlayer, CellGrid cellGrid)
        {
            var score = evaluatingUnit.DryAttack(unitToEvaluate) / topDamage;
            return score;
        }
    }
}
