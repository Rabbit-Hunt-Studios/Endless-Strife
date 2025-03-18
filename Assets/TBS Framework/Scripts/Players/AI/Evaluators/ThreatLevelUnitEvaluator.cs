using TbsFramework.Cells;
using TbsFramework.Grid;
using TbsFramework.Units;
using UnityEngine;


namespace TbsFramework.Players.AI.Evaluators
{
    public class ThreatLevelUnitEvaluator : UnitEvaluator
    {
        public override float Evaluate(Unit unitToEvaluate, Unit evaluatingUnit, Player currentPlayer, CellGrid cellGrid)
        {
            if (unitToEvaluate.PlayerNumber == currentPlayer.PlayerNumber)
                return 0;
                
            float threatScore = 0;
            
            // Base threat on unit stats
            threatScore += unitToEvaluate.AttackFactor * 0.5f;
            threatScore += unitToEvaluate.DefenceFactor * 0.3f;
            threatScore += unitToEvaluate.HitPoints * 0.02f;
            
            // Bonus for ranged units
            if (unitToEvaluate is ESUnit esUnit && esUnit.AttackRange > 1)
                threatScore += 2.0f;
            
            // Consider position - units near objectives or our base are more threatening
            Cell objectiveCell = FindObjectiveCell(cellGrid);
            Cell ourBaseCell = FindBaseCell(currentPlayer.PlayerNumber, cellGrid);
            
            if (objectiveCell != null)
            {
                float distToObjective = Vector3.Distance(unitToEvaluate.Cell.transform.position, 
                                                       objectiveCell.transform.position);
                float objectiveThreat = 5 / (distToObjective + 1);
                threatScore += objectiveThreat;
            }
            
            if (ourBaseCell != null)
            {
                float distToBase = Vector3.Distance(unitToEvaluate.Cell.transform.position, 
                                                  ourBaseCell.transform.position);
                float baseThreat = 4 / (distToBase + 1);
                threatScore += baseThreat;
            }
            
            return threatScore;
        }
        
        private Cell FindObjectiveCell(CellGrid cellGrid)
        {
            foreach (var cell in cellGrid.Cells)
            {
                if (cell.CurrentUnits.Count > 0)
                {
                    foreach (var unit in cell.CurrentUnits)
                    {
                        ESUnit esUnit = unit.GetComponent<ESUnit>();
                        if (esUnit != null && 
                            esUnit.isStructure &&
                            esUnit.UnitName.Contains("Objective"))
                        {
                            return cell;
                        }
                    }
                }
            }
            return null;
        }
        
        private Cell FindBaseCell(int playerNumber, CellGrid cellGrid)
        {
            foreach (var cell in cellGrid.Cells)
            {
                if (cell.CurrentUnits.Count > 0)
                {
                    foreach (var unit in cell.CurrentUnits)
                    {
                        ESUnit esUnit = unit.GetComponent<ESUnit>();
                        if (esUnit != null && 
                            esUnit.isStructure &&
                            esUnit.UnitName.Contains("Objective") &&
                            esUnit.PlayerNumber == playerNumber)
                        {
                            return cell;
                        }
                    }
                }
            }
            return null;
        }
    }
}