using TbsFramework.Cells;
using TbsFramework.Grid;
using TbsFramework.Units;
using UnityEngine;


namespace TbsFramework.Players.AI.Evaluators
{
    public class KillPotentialUnitEvaluator : UnitEvaluator
    {
        public override float Evaluate(Unit unitToEvaluate, Unit evaluatingUnit, Player currentPlayer, CellGrid cellGrid)
        {
            if (unitToEvaluate.PlayerNumber == currentPlayer.PlayerNumber || unitToEvaluate.PlayerNumber == -1)
                return 0; // Not an enemy
                
            // Basic factors for damage calculation
            float potentialDamage = evaluatingUnit.AttackFactor - unitToEvaluate.DefenceFactor * 0.5f;
            potentialDamage = Mathf.Max(1, potentialDamage); // Minimum damage is 1
            
            // Score based on potential to kill
            float killPotential = potentialDamage / unitToEvaluate.HitPoints;
            
            // Bonus if we can kill the unit in one hit
            if (potentialDamage >= unitToEvaluate.HitPoints)
                killPotential += 3.0f;
                
            // Bonus for targeting damaged units
            float healthPercent = unitToEvaluate.HitPoints / unitToEvaluate.TotalHitPoints;
            killPotential += (1 - healthPercent) * 2.0f;
            
            // Bonus for targeting high-value units
            if (unitToEvaluate is ESUnit esUnit)
            {
                // Higher bonus for ranged or strong units
                if (esUnit.AttackRange > 1)
                    killPotential += 1.5f;
                    
                if (esUnit.AttackFactor > 15)
                    killPotential += 1.0f;
            }
            
            return killPotential;
        }
    }
}
