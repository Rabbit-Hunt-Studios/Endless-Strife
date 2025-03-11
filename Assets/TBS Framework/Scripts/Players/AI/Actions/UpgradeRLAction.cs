using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TbsFramework.Grid;
using TbsFramework.Grid.GridStates;
using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.Players.AI.Actions
{
    public class UpgradeRLAction : RLAction
    {
        // Debug information
        private string upgradeDebugInfo;
        private float upgradeScore;
        
        private EconomyController economyController;
        
        public override void InitializeAction(Player player, Unit unit, CellGrid cellGrid)
        {
            economyController = Object.FindObjectOfType<EconomyController>();
        }
        
        public override bool ShouldExecute(Player player, Unit unit, CellGrid cellGrid)
        {
            // Check if unit has UpgradeAbility
            var upgradeAbility = unit.GetComponent<UpgradeAbility>();
            if (upgradeAbility == null || unit.ActionPoints <= 0)
            {
                return false;
            }
            
            // Check if player has enough money for upgrade
            int playerMoney = economyController.GetValue(unit.PlayerNumber);
            int upgradeCost = upgradeAbility.upgradeCost;
            
            if (playerMoney < upgradeCost)
            {
                return false;
            }
            
            // Evaluate if upgrade is worthwhile now
            upgradeScore = EvaluateUpgrade(upgradeAbility, unit, player, cellGrid, playerMoney);
            
            // Only upgrade if score exceeds threshold
            return upgradeScore > 0.6f; // Higher threshold since it's an expensive commitment
        }
        
        public override void Precalculate(Player player, Unit unit, CellGrid cellGrid)
        {
            // No additional precalculation needed for upgrading
        }
        
        public override IEnumerator Execute(Player player, Unit unit, CellGrid cellGrid)
        {
            // Execute upgrade action
            var upgradeAbility = unit.GetComponent<UpgradeAbility>();
            
            // Execute the ability
            yield return StartCoroutine(upgradeAbility.Execute(cellGrid,
                _ => {},  // No need for state change
                _ => {}   // No need for state restoration
            ));
            
            yield return new WaitForSeconds(0.5f);
        }
        
        public override void CleanUp(Player player, Unit unit, CellGrid cellGrid)
        {
            // No cleanup needed
        }
        
        public override void ShowDebugInfo(Player player, Unit unit, CellGrid cellGrid)
        {
            Debug.Log($"UpgradeBaseRLAction Evaluation: Score = {upgradeScore:F2}\n{upgradeDebugInfo}");
        }
        
        public override int GetActionIndex()
        {
            return 4; // Unique ID for upgrade action
        }
        
        #region Helper Methods
        
        private float EvaluateUpgrade(
            UpgradeAbility upgradeAbility, 
            Unit baseUnit, 
            Player player, 
            CellGrid cellGrid,
            int playerMoney)
        {
            float score = 0.0f;
            upgradeDebugInfo = "";
            
            int upgradeCost = upgradeAbility.upgradeCost;
            GameObject upgradedPrefab = upgradeAbility.prefabToChange;
            
            // 1. Consider game turn - more valuable to upgrade earlier
            int turn = cellGrid.Turns[player.PlayerNumber, 0];
            float turnBonus = Mathf.Max(0, 0.3f - (turn * 0.015f)); // Starts at 0.3, reduces over time
            score += turnBonus;
            upgradeDebugInfo += $"Turn timing: +{turnBonus:F2}\n";
            
            // 2. Consider economy - how much income increase for the cost
            int incomeIncrease = 0;
            if (upgradedPrefab != null && upgradedPrefab.GetComponent<IncomeAbility>() != null)
            {
                var baseIncome = baseUnit.GetComponent<IncomeAbility>()?.Amount ?? 0;
                var newIncome = upgradedPrefab.GetComponent<IncomeAbility>().Amount;
                incomeIncrease = newIncome - baseIncome;
            }
            
            // Calculate return on investment (turns to pay back)
            float roi = incomeIncrease > 0 ? (float)upgradeCost / incomeIncrease : float.MaxValue;
            
            // Score based on ROI (lower is better)
            float roiScore = 0;
            if (roi < float.MaxValue)
            {
                roiScore = Mathf.Clamp(1.0f - (roi / 20.0f), 0, 0.4f); // Max bonus for ROI of 1, down to 0 for ROI of 20+
            }
            score += roiScore;
            upgradeDebugInfo += $"Income ROI ({roi:F1} turns): +{roiScore:F2}\n";
            
            // 3. Consider unit cap increase
            float unitCapScore = 0.2f; // Assume upgrade increases unit cap
            score += unitCapScore;
            upgradeDebugInfo += $"Unit cap increase: +{unitCapScore:F2}\n";
            
            // 4. Consider money ratio - don't upgrade if it uses too much money
            float moneyRatioSpent = (float)upgradeCost / playerMoney;
            float moneyRatioScore = 0;
            
            if (moneyRatioSpent > 0.6f)
            {
                // Penalty for spending too much money
                moneyRatioScore = -(moneyRatioSpent - 0.6f) * 0.8f;
            }
            score += moneyRatioScore;
            upgradeDebugInfo += $"Money conservation: {moneyRatioScore:F2}\n";
            
            // 5. Consider current unit count relative to cap
            int currentUnitCount = cellGrid.Units.Count(u => u.PlayerNumber == player.PlayerNumber && 
                                                        !(u as ESUnit).isStructure && 
                                                        u.isActiveAndEnabled);
            
            // Get current limit (this assumes SpawnAbility is on the same object)
            int unitLimit = baseUnit.GetComponent<SpawnAbility>()?.limitUnits ?? 4;
            
            // Calculate how close to limit we are
            float unitCapRatio = (float)currentUnitCount / unitLimit;
            float unitCapRatioScore = 0;
            
            if (unitCapRatio > 0.7f)
            {
                // Bonus for being close to unit cap
                unitCapRatioScore = (unitCapRatio - 0.7f) * 0.5f;
            }
            score += unitCapRatioScore;
            upgradeDebugInfo += $"Unit cap pressure ({unitCapRatio:P0}): +{unitCapRatioScore:F2}\n";
            
            // 6. Consider game state (winning/losing)
            bool isWinning = IsWinning(player.PlayerNumber, cellGrid);
            float gameStateScore = isWinning ? 0.1f : 0.2f; // More pressure to upgrade when losing
            score += gameStateScore;
            upgradeDebugInfo += $"Game state ({(isWinning ? "winning" : "losing")}): +{gameStateScore:F2}\n";
            
            // Final score calculation
            upgradeDebugInfo += $"Total: {score:F2}";
            return score;
        }
        
        private bool IsWinning(int playerNumber, CellGrid cellGrid)
        {
            // Simple heuristic for determining if the player is winning
            
            // Count units for each player
            Dictionary<int, int> unitCounts = new Dictionary<int, int>();
            Dictionary<int, float> unitValues = new Dictionary<int, float>();
            
            foreach (var unit in cellGrid.Units)
            {
                if (!(unit as ESUnit).isStructure)
                {
                    int player = unit.PlayerNumber;
                    
                    // Count units
                    if (!unitCounts.ContainsKey(player))
                    {
                        unitCounts[player] = 0;
                        unitValues[player] = 0;
                    }
                    
                    unitCounts[player]++;
                    
                    // Sum up unit values (simplified measure)
                    float unitValue = unit.AttackFactor + unit.DefenceFactor + unit.HitPoints / 2;
                    unitValues[player] += unitValue;
                }
            }
            
            // Check objective control
            bool hasObjective = false;
            int objectiveControlTurns = 0;
            
            foreach (var cell in cellGrid.Cells)
            {
                ESUnit esUnit = cell.CurrentUnits
                    .Select(u => u.GetComponent<ESUnit>())
                    .OfType<ESUnit>()
                    .FirstOrDefault();
                if (esUnit.UnitName.Contains("Objective") && 
                    esUnit.PlayerNumber == playerNumber &&
                    esUnit.isStructure)
                {
                    hasObjective = true;
                    objectiveControlTurns++; // Simplified - assume 1 turn if controlled
                }
            }
            
            // Compare player's unit value to highest opponent
            float playerValue = unitValues.ContainsKey(playerNumber) ? unitValues[playerNumber] : 0;
            float maxOpponentValue = 0;
            
            foreach (var kvp in unitValues)
            {
                if (kvp.Key != playerNumber && kvp.Value > maxOpponentValue)
                {
                    maxOpponentValue = kvp.Value;
                }
            }
            
            // Consider winning if:
            // 1. Player has more unit value than any opponent by 20%+, or
            // 2. Player controls the objective
            return (playerValue > maxOpponentValue * 1.2f) || hasObjective;
        }
        
        #endregion
    }
}
