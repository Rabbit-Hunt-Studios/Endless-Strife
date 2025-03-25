using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TbsFramework.Grid;
using TbsFramework.Players.AI;
using TbsFramework.Players.AI.Evaluators;
using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.Players.AI.Actions
{
    public class MinimaxSpawnUnitAction : MinimaxAIAction
    {
        public GameObject SelectedPrefab { get; private set; }
        private SpawnAbility spawnAbility;
        private UnitEvaluator[] unitEvaluators;
        private EconomyController economyController;
        
        public override void InitializeAction(Player player, Unit unit, CellGrid cellGrid)
        {
            spawnAbility = unit.GetComponent<SpawnAbility>();
            unitEvaluators = GetComponents<UnitEvaluator>();
            
            if (economyController == null)
                economyController = Object.FindObjectOfType<EconomyController>();
        }
        
        public override bool ShouldExecute(Player player, Unit unit, CellGrid cellGrid)
        {
            if (spawnAbility == null)
            {
                return false;
            }   
            // Check resources and unit limit
            int playerMoney = economyController.GetValue(player.PlayerNumber);
            int currentUnitCount = cellGrid.Units.Count(u => u.PlayerNumber == player.PlayerNumber && 
                                                         !(u as ESUnit).isStructure && 
                                                         u.isActiveAndEnabled);
            
            if (currentUnitCount >= spawnAbility.limitUnits)
            {
                return false;
            }   
            // Check for affordable units
            List<GameObject> affordableUnits = GetAffordableUnits(player.PlayerNumber, playerMoney);
            
            if (affordableUnits.Count == 0)
            {
                return false;
            }   
            // Evaluate all units and select the best one
            SelectedPrefab = SelectBestUnit(affordableUnits, unit, player, cellGrid, currentUnitCount);
            
            return SelectedPrefab != null;
        }
        
        public override void Precalculate(Player player, Unit unit, CellGrid cellGrid)
        {
            // No additional precalculation needed
        }
        
        public override IEnumerator Execute(Player player, Unit unit, CellGrid cellGrid)
        {
            if (spawnAbility != null && SelectedPrefab != null)
            {
                spawnAbility.SelectedPrefab = SelectedPrefab;
                yield return StartCoroutine(spawnAbility.Execute(cellGrid, 
                    _ => {}, // No state change needed
                    _ => {}  // No state restoration needed
                ));
            }
            
            yield return new WaitForSeconds(0.5f);
        }
        
        public override void CleanUp(Player player, Unit unit, CellGrid cellGrid)
        {
            SelectedPrefab = null;
        }
        
        public override void ShowDebugInfo(Player player, Unit unit, CellGrid cellGrid)
        {
            if (SelectedPrefab != null)
            {
                Debug.Log($"Selected unit to spawn: {SelectedPrefab.GetComponent<ESUnit>().UnitName}");
            }
        }
        
        public override void SimulateAction(GameState state, Unit unit)
        {
            if (SelectedPrefab == null || spawnAbility == null)
                return;
                
            // Simulate resource cost
            int cost = SelectedPrefab.GetComponent<Price>().Value;
            if (state.Resources.ContainsKey(unit.PlayerNumber))
            {
                state.Resources[unit.PlayerNumber] -= cost;
            }
            
            // In a real simulation, we would add a new unit to the state
            // For minimax purposes, we'll just update the state to reflect the advantage
            // of having a new unit without actually creating it
            
            // This is a simplified approach - in a full implementation,
            // you would create a new unit object and add it to the state
        }
        
        private List<GameObject> GetAffordableUnits(int playerNumber, int playerMoney)
        {
            List<GameObject> affordableUnits = new List<GameObject>();
            
            // Check regular units
            foreach (var prefab in spawnAbility.Prefabs)
            {
                int cost = prefab.GetComponent<Price>().Value;
                if (cost <= playerMoney)
                {
                    affordableUnits.Add(prefab);
                }
            }
            
            // Check special units if unlocked
            foreach (var prefab in spawnAbility.SpecialPrefabs)
            {
                int cost = prefab.GetComponent<Price>().Value;
                if (cost <= playerMoney && IsUnitUnlocked(prefab, playerNumber))
                {
                    affordableUnits.Add(prefab);
                }
            }
            Debug.Log("Affordable units: " + affordableUnits.Count);
            foreach (var unit in affordableUnits)
            {
                Debug.Log(affordableUnits.IndexOf(unit) + " : " + unit.GetComponent<ESUnit>().UnitName);
            }
            return affordableUnits;
        }
        
        private bool IsUnitUnlocked(GameObject prefab, int playerNumber)
        {
            string unitName = prefab.GetComponent<ESUnit>().UnitName;
            
            // Check if any owned structure unlocks this unit
            return Object.FindObjectsOfType<ESUnit>()
                .Any(u => u.PlayerNumber == playerNumber && 
                      u.UnitUnlock == unitName);
        }
        
        private GameObject SelectBestUnit(List<GameObject> units, Unit baseUnit, Player player, CellGrid cellGrid, int currentUnitCount)
        {
            GameObject bestUnit = null;
            float bestScore = float.MinValue;
            
            foreach (var unitPrefab in units)
            {
                float score = EvaluateUnitPrefab(unitPrefab, baseUnit, player, cellGrid, currentUnitCount);
                Debug.Log($"Score for {unitPrefab.GetComponent<ESUnit>().UnitName}: {score}");
                if (score > bestScore)
                {
                    bestScore = score;
                    bestUnit = unitPrefab;
                }
            }
            
            return bestUnit;
        }
        
        private float EvaluateUnitPrefab(GameObject prefab, Unit baseUnit, Player player, CellGrid cellGrid, int currentUnitCount)
        {
            ESUnit unitStats = prefab.GetComponent<ESUnit>();
            int cost = prefab.GetComponent<Price>().Value;
            int playerMoney = economyController.GetValue(player.PlayerNumber);
            
            float score = 0;
            
            // Basic unit stats evaluation
            score += unitStats.AttackFactor * 0.5f;
            score += unitStats.DefenceFactor * 0.4f;
            score += unitStats.HitPoints * 0.1f;
            score += unitStats.MovementPoints * 0.5f;
            
            // Value for ranged units
            if (unitStats.AttackRange > 1)
            {
                score += unitStats.AttackRange;
            }   
            // Economy consideration - don't spend all money
            float moneyRatio = (float)cost / playerMoney;
            if (moneyRatio > 0.7f)
            {
                score -= (moneyRatio - 0.7f) * 10;
            }   
            // Unit diversity - check what types we already have
            if (GetUnitTypeCount(GetUnitType(unitStats), player.PlayerNumber, cellGrid) == 0)
            {
                score += 2.0f; // Bonus for new unit types
            }   
            // Consider game state - more units early game, stronger units late game
            if (cellGrid.Turns[player.PlayerNumber, 0] < 5 && cost < 250)
            {
                score += 1.0f; // Cheap units early game
            }
            else if (cellGrid.Turns[player.PlayerNumber, 0] > 10 && unitStats.AttackFactor + unitStats.DefenceFactor >= 80)
            {
                score += 1.5f; // Strong units late game
            }    
            // If we're near unit limit, prioritize quality over quantity
            if (currentUnitCount > spawnAbility.limitUnits * 0.8f)
            {
                score += (unitStats.AttackFactor + unitStats.DefenceFactor + unitStats.AttackRange) * 0.5f;
            }    
            // Use custom evaluators if any
            if (unitEvaluators != null && unitEvaluators.Length > 0)
            {
                foreach (var evaluator in unitEvaluators)
                {
                    score += evaluator.Evaluate(prefab.GetComponent<Unit>(), baseUnit, player, cellGrid);
                }
            }
            
            // Consider position to objective - if we need units for capturing
            if (!IsControllingObjective(player.PlayerNumber, cellGrid) && unitStats.MovementPoints >= 3)
            {
                score += 1.5f;
            }
            
            return score;
        }
        
        private int GetUnitTypeCount(int unitType, int playerNumber, CellGrid cellGrid)
        {
            return cellGrid.Units.Count(u => u.PlayerNumber == playerNumber && 
                                        u is ESUnit esUnit && 
                                        !esUnit.isStructure && 
                                        GetUnitType(esUnit) == unitType);
        }
        
        private bool IsControllingObjective(int playerNumber, CellGrid cellGrid)
        {
            foreach (var cell in cellGrid.Cells)
            {
                if (cell.CurrentUnits.Count > 0)
                {
                    foreach (var unit in cell.CurrentUnits)
                    {   
                        if (unit == null || !unit.isActiveAndEnabled)
                            continue;
                        if (unit.GetComponent<ESUnit>() != null && 
                            unit.PlayerNumber == playerNumber && 
                            unit.GetComponent<ESUnit>().UnitName.Contains("Objective"))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
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
        public override int GetActionIndex()
        {
            return 4; // Unique ID for move action
        }
    }
}