using System.Collections.Generic;
using System.Linq;
using TbsFramework.Cells;
using TbsFramework.Grid;
using TbsFramework.Units;

namespace TbsFramework.Players.AI.Evaluators
{
    public class DamageCellEvaluator : CellEvaluator
    {
        private float maxPossibleDamage;
        private List<Unit> enemyUnits;

        private Dictionary<Unit, float> damage;

        public override float Evaluate(Cell cellToEvaluate, Unit evaluatingUnit, Player currentPlayer, CellGrid cellGrid)
        {
            if(maxPossibleDamage.Equals(0f))
            {
                return 0;
            }

            var scores = enemyUnits.Select(u =>
            {
                var isAttackableVal = evaluatingUnit.IsUnitAttackable(u, cellToEvaluate) ? 1f : 0f;
                var localScore = (isAttackableVal * damage[u]) / maxPossibleDamage;

                return localScore;
            });

            var maxScore = scores.DefaultIfEmpty().Max();
            return maxScore;
        }

        public override void Precalculate(Unit evaluatingUnit, Player currentPlayer, CellGrid cellGrid)
        {
            damage = new Dictionary<Unit, float>();
            maxPossibleDamage = 0f;

            enemyUnits = cellGrid.GetEnemyUnits(currentPlayer);
            foreach (var enemy in enemyUnits)
            {
                var realDamage = evaluatingUnit.DryAttack(enemy);
                damage.Add(enemy, realDamage);

                if (realDamage > maxPossibleDamage)
                {
                    maxPossibleDamage = realDamage;
                }
            }
        }
    }
}
