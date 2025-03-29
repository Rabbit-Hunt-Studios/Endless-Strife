using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TbsFramework.Grid;
using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.Players.AI.Evaluators
{
    public class SpawnUnitEvaluator : UnitEvaluator
    {
        public float swordmanWeight = 1.0f;
        public float archerWeight = 1.1f;
        public float knightWeight = 1.2f;
        public float spearmanWeight = 1.0f;
        public float musketeerWeight = 1.4f;
        public float axemanWeight = 1.1f;
        public float wizardWeight = 1.3f;
        public float assassinWeight = 1.5f;
        
        public override float Evaluate(Unit unitToEvaluate, Unit evaluatingUnit, Player currentPlayer, CellGrid cellGrid)
        {
            if (!(unitToEvaluate is ESUnit esUnit))
                return 0;
                
            float score = 0;
            
            switch (GetUnitType(esUnit))
            {
                case 1:
                    score = swordmanWeight;
                    break;
                case 2:
                    score = archerWeight;
                    break;
                case 3:
                    score = knightWeight;
                    break;
                case 4:
                    score = spearmanWeight;
                    break;
                case 5:
                    score = musketeerWeight;
                    break;
                case 6:
                    score = wizardWeight;
                    break;
                case 7:
                    score = spearmanWeight;
                    break;
                case 8:
                    score = assassinWeight;
                    break;
                default:
                    score = 1.0f;
                    break;
            }
            
            int turn = cellGrid.Turns[currentPlayer.PlayerNumber, 0];
            if (turn < 5) // Early game
            {
                if (GetUnitType(esUnit) == 1 || GetUnitType(esUnit) == 2) // Swordmen and archers important early
                    score *= 1.3f;
            }
            else if (turn >= 10) // Late game
            {
                if (GetUnitType(esUnit) != 1) // Swordmen less important late
                    score *= 1.3f;
            }
            
            int unitCount = cellGrid.Units.Count(u => u.PlayerNumber == currentPlayer.PlayerNumber && 
                                                 u is ESUnit eu && !eu.isStructure);
            int typeCount = cellGrid.Units.Count(u => u.PlayerNumber == currentPlayer.PlayerNumber && 
                                               u is ESUnit eu && !eu.isStructure && 
                                               GetUnitType(eu) == GetUnitType(esUnit));
            
            if (unitCount > 0 && typeCount > 0)
            {
                float typeRatio = (float)typeCount / unitCount;
                if (typeRatio > 0.4f)
                    score *= (1.0f - (typeRatio - 0.4f) * 2.0f);
            }
            
            return score;
        }

        public int GetUnitType(ESUnit unit)
        {
            string unitName = unit.name.ToLower();
            
            if (unitName.Contains("swordman"))
                return 1;
            else if (unitName.Contains("archer"))
                return 2;
            else if (unitName.Contains("knight"))
                return 3;
            else if (unitName.Contains("spearman"))
                return 4;
            else if (unitName.Contains("musketeer"))
                return 5;
            else if (unitName.Contains("axeman"))
                return 6;
            else if (unitName.Contains("wizard"))
                return 7;
            else if (unitName.Contains("assassin")) 
                return 8;
            else if (unit.isStructure)
                return 0;
                
            return 0;
        }
    }
}
