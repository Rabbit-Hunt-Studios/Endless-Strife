using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TbsFramework.Grid;
using TbsFramework.Grid.GridStates;
using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.Players.AI.Actions
{
    public class SpawnRLAction : RLAction
    {
        // Selected unit prefab to spawn
        public GameObject SelectedPrefab { get; private set; }
        
        // Debug information
        private Dictionary<GameObject, string> prefabDebugInfo;
        private List<(GameObject prefab, float value)> prefabScores;
        
        private EconomyController economyController;
        
        public override void InitializeAction(Player player, Unit unit, CellGrid cellGrid)
        {
            prefabDebugInfo = new Dictionary<GameObject, string>();
            economyController = Object.FindObjectOfType<EconomyController>();
        }
        
        public override bool ShouldExecute(Player player, Unit unit, CellGrid cellGrid)
        {
            // Check if unit has SpawnAbility
            var spawnAbility = unit.GetComponent<SpawnAbility>();
            if (spawnAbility == null)
            {
                return false;
            }
            
            // Check if player has enough money to spawn any unit
            int playerMoney = economyController.GetValue(unit.PlayerNumber);
            
            // Check if player is at unit limit
            int currentUnitCount = cellGrid.Units.Count(u => u.PlayerNumber == unit.PlayerNumber && 
                                                         !(u as ESUnit).isStructure && 
                                                         u.isActiveAndEnabled);
            
            if (currentUnitCount >= spawnAbility.limitUnits)
            {
                return false;
            }
            
            // Get all available prefabs
            List<GameObject> availablePrefabs = new List<GameObject>();
            availablePrefabs.AddRange(spawnAbility.Prefabs);
            
            // Add special units if they're unlocked
            foreach (var specialPrefab in spawnAbility.SpecialPrefabs)
            {
                if (cellGrid.Units.Exists(u => u.PlayerNumber == player.PlayerNumber && 
                                          u.GetComponent<ESUnit>().UnitUnlock == specialPrefab.GetComponent<ESUnit>().UnitName))
                {
                    availablePrefabs.Add(specialPrefab);
                }
            }
            
            // Check if any prefab is affordable
            bool canAffordAny = availablePrefabs.Any(p => p.GetComponent<Price>().Value <= playerMoney);
            
            if (!canAffordAny)
            {
                return false;
            }
            
            // Evaluate all affordable prefabs
            prefabScores = EvaluateUnitPrefabs(availablePrefabs, unit, player, cellGrid, playerMoney);
            
            if (prefabScores.Count == 0)
            {
                return false;
            }
            
            // Select best unit to spawn
            var (bestPrefab, bestScore) = prefabScores.OrderByDescending(p => p.value).First();
            
            // Only spawn if score is above threshold (adjust as needed)
            if (bestScore > 0.5f)
            {
                SelectedPrefab = bestPrefab;
                return true;
            }
            
            return false;
        }
        
        public override void Precalculate(Player player, Unit unit, CellGrid cellGrid)
        {
            // No additional precalculation needed for spawning
        }
        
        public override IEnumerator Execute(Player player, Unit unit, CellGrid cellGrid)
        {
            // Execute spawn action
            var spawnAbility = unit.GetComponent<SpawnAbility>();
            
            // Set the selected prefab
            spawnAbility.SelectedPrefab = SelectedPrefab;
            
            // Execute the ability
            yield return StartCoroutine(spawnAbility.Execute(cellGrid,
                _ => {},  // No need for state change
                _ => {}   // No need for state restoration
            ));
            
            yield return new WaitForSeconds(0.5f);
        }
        
        public override void CleanUp(Player player, Unit unit, CellGrid cellGrid)
        {
            SelectedPrefab = null;
            prefabScores = null;
        }
        
        public override void ShowDebugInfo(Player player, Unit unit, CellGrid cellGrid)
        {
            if (prefabScores == null)
            {
                return;
            }
            
            Debug.Log("SpawnUnitRLAction Evaluation:");
            foreach (var (prefab, score) in prefabScores)
            {
                string unitName = prefab.GetComponent<ESUnit>().UnitName;
                Debug.Log($"Unit: {unitName}, Score: {score:F2}, Info: {prefabDebugInfo[prefab]}");
            }
            
            if (SelectedPrefab != null)
            {
                Debug.Log($"Selected unit to spawn: {SelectedPrefab.GetComponent<ESUnit>().UnitName}");
            }
        }
        
        public override int GetActionIndex()
        {
            return 4; // Unique ID for spawn action
        }
        
        #region Helper Methods
        
        private List<(GameObject prefab, float value)> EvaluateUnitPrefabs(
            List<GameObject> prefabs, 
            Unit baseUnit, 
            Player player, 
            CellGrid cellGrid,
            int playerMoney)
        {
            List<(GameObject prefab, float value)> scores = new List<(GameObject prefab, float value)>();
            
            foreach (var prefab in prefabs)
            {
                // Skip if can't afford
                int cost = prefab.GetComponent<Price>().Value;
                if (cost > playerMoney)
                {
                    prefabDebugInfo[prefab] = "Can't afford";
                    continue;
                }
                
                // Get unit stats
                var unitStats = prefab.GetComponent<ESUnit>();
                
                // Start with base score
                float score = 0.5f; // Base value
                string debugInfo = "";
                
                // Evaluate based on game state
                
                // 1. Consider unit type distribution
                Dictionary<string, int> unitTypeCounts = CountUnitTypes(cellGrid, player.PlayerNumber);
                string unitType = unitStats.UnitName;
                
                if (!unitTypeCounts.ContainsKey(unitType))
                {
                    // Bonus for new unit types
                    score += 0.2f;
                    debugInfo += "New unit type: +0.2\n";
                }
                else if (unitTypeCounts[unitType] < 2) // Want at least a couple of each type
                {
                    score += 0.1f;
                    debugInfo += "Few units of this type: +0.1\n";
                }
                
                // 2. Consider unit stats relative to cost
                float valueRatio = (unitStats.AttackFactor + unitStats.DefenceFactor + unitStats.HitPoints + unitStats.MovementPoints) / cost;
                float valueScore = Mathf.Clamp(valueRatio / 5.0f, 0, 0.3f); // Scale and cap
                score += valueScore;
                debugInfo += $"Value ratio: +{valueScore:F2}\n";
                
                // 3. Consider game phase
                int turn = cellGrid.Turns[player.PlayerNumber, 0];
                
                if (turn < 5) // Early game
                {
                    // Prefer cheaper units early
                    float cheapScore = Mathf.Clamp(1.0f - (cost / 20.0f), 0, 0.2f);
                    score += cheapScore;
                    debugInfo += $"Early game cheap bonus: +{cheapScore:F2}\n";
                }
                else if (turn >= 10) // Late game
                {
                    // Prefer stronger units late
                    float strengthScore = Mathf.Clamp((unitStats.AttackFactor + unitStats.DefenceFactor) / 20.0f, 0, 0.2f);
                    score += strengthScore;
                    debugInfo += $"Late game strength bonus: +{strengthScore:F2}\n";
                }
                
                // 4. Consider special roles
                if (unitStats.AttackRange > 1)
                {
                    // Value ranged units
                    score += 0.15f;
                    debugInfo += "Ranged unit: +0.15\n";
                }
                
                if (unitStats.MovementPoints > 3)
                {
                    // Value mobile units
                    score += 0.1f;
                    debugInfo += "Mobile unit: +0.1\n";
                }
                
                // 5. Economic consideration - don't spend all money
                float moneyRatioSpent = (float)cost / playerMoney;
                if (moneyRatioSpent > 0.7f)
                {
                    // Penalty for spending too much money
                    float penaltyScore = -(moneyRatioSpent - 0.7f) * 0.5f;
                    score += penaltyScore;
                    debugInfo += $"Money conservation: {penaltyScore:F2}\n";
                }
                
                // 6. Consider if we need units for objective capture
                bool needObjectiveControl = !IsControllingObjective(cellGrid, player.PlayerNumber);
                if (needObjectiveControl && unitStats.MovementPoints >= 3)
                {
                    // Bonus for mobile units when we need to capture objectives
                    score += 0.2f;
                    debugInfo += "Objective capture need: +0.2\n";
                }
                
                // Store debug info and add to scores
                prefabDebugInfo[prefab] = debugInfo + $"Total: {score:F2}";
                scores.Add((prefab, score));
            }
            
            return scores;
        }
        
        private Dictionary<string, int> CountUnitTypes(CellGrid cellGrid, int playerNumber)
        {
            Dictionary<string, int> counts = new Dictionary<string, int>();
            
            foreach (var unit in cellGrid.Units)
            {
                if (unit.PlayerNumber == playerNumber && unit is ESUnit esUnit && !esUnit.isStructure)
                {
                    string unitType = esUnit.UnitName;
                    if (counts.ContainsKey(unitType))
                    {
                        counts[unitType]++;
                    }
                    else
                    {
                        counts[unitType] = 1;
                    }
                }
            }
            
            return counts;
        }
        
        private bool IsControllingObjective(CellGrid cellGrid, int playerNumber)
        {
            foreach (var cell in cellGrid.Cells)
            {
                if (cell.CurrentUnits.Count == 0)
                {
                    continue;
                }
                ESUnit esUnit = cell.CurrentUnits
                    .Select(u => u.GetComponent<ESUnit>())
                    .OfType<ESUnit>()
                    .FirstOrDefault();
                if (esUnit.isStructure &&
                    esUnit.UnitName.Contains("Objective") &&
                    esUnit.PlayerNumber == playerNumber)
                {
                    return true;
                }
            }
            return false;
        }
        
        #endregion
    }
}
