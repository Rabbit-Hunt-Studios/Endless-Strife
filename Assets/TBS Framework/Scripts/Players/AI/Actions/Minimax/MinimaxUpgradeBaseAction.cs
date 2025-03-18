using System.Collections;
using System.Linq;
using TbsFramework.Grid;
using TbsFramework.Players.AI;
using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.Players.AI.Actions
{
    public class MinimaxUpgradeBaseAction : MinimaxAIAction
    {
        private UpgradeAbility upgradeAbility;
        private SpawnAbility spawnAbility;
        private EconomyController economyController;
        private bool upgradePerformed = false; 
        
        public override void InitializeAction(Player player, Unit unit, CellGrid cellGrid)
        {
            upgradeAbility = unit.GetComponent<UpgradeAbility>();
            spawnAbility = unit.GetComponent<SpawnAbility>();
            
            if (economyController == null)
                economyController = Object.FindObjectOfType<EconomyController>();
        }
        
        public override bool ShouldExecute(Player player, Unit unit, CellGrid cellGrid)
        {
            if (upgradePerformed || upgradeAbility == null)
                return false;
                
            // Check if enough resources
            int playerMoney = economyController.GetValue(player.PlayerNumber);
            if (playerMoney < upgradeAbility.upgradeCost)
                return false;
                
            // Strategic evaluation for upgrading
            return ShouldUpgradeBase(player, unit, cellGrid, playerMoney);
        }
        
        public override void Precalculate(Player player, Unit unit, CellGrid cellGrid)
        {
            // No additional precalculation needed
        }
        
        public override IEnumerator Execute(Player player, Unit unit, CellGrid cellGrid)
        {
            if (upgradeAbility != null)
            {
                yield return StartCoroutine(upgradeAbility.Execute(cellGrid,
                    _ => {}, // No state change needed
                    _ => {}  // No state restoration needed
                ));
            }
            upgradePerformed = true;
            
            yield return new WaitForSeconds(0.5f);
        }
        
        public override void CleanUp(Player player, Unit unit, CellGrid cellGrid)
        {
            // Nothing to clean up
        }
        
        public override void ShowDebugInfo(Player player, Unit unit, CellGrid cellGrid)
        {
            Debug.Log($"Executing base upgrade for {unit.name}");
        }
        
        public override void SimulateAction(GameState state, Unit unit)
        {
            if (upgradeAbility == null)
                return;
                
            // Simulate resource cost
            if (state.Resources.ContainsKey(unit.PlayerNumber))
            {
                state.Resources[unit.PlayerNumber] -= upgradeAbility.upgradeCost;
            }
            
            // Simulate income increase (approximate)
            // In a real simulation, this would depend on the upgraded base's income ability
            // For now, we'll just assume a fixed increase
            if (state.Resources.ContainsKey(unit.PlayerNumber))
            {
                // This is a proxy for the long-term advantage of increased income
                state.Resources[unit.PlayerNumber] += 10; // Represent future income advantage
            }
        }
        
        private bool ShouldUpgradeBase(Player player, Unit unit, CellGrid cellGrid, int playerMoney)
        {
            // ROI (Return on Investment) calculation
            int upgradeCost = upgradeAbility.upgradeCost;
            int currentIncome = 0;
            int projectedIncome = 0;
            
            // Get current income
            var incomeAbility = unit.GetComponent<IncomeAbility>();
            if (incomeAbility != null)
            {
                currentIncome = incomeAbility.Amount;
            }
            
            // Get projected income from upgraded prefab
            if (upgradeAbility.prefabToChange != null)
            {
                var upgradedIncomeAbility = upgradeAbility.prefabToChange.GetComponent<IncomeAbility>();
                if (upgradedIncomeAbility != null)
                {
                    projectedIncome = upgradedIncomeAbility.Amount;
                }
            }
            
            int incomeIncrease = projectedIncome - currentIncome;
            Debug.Log($"Upgrade Cost: {upgradeCost} : Income increase: {incomeIncrease}");
            // Calculate turns to break even
            float turnsToBreakEven = incomeIncrease > 0 ? (float)upgradeCost / incomeIncrease : float.MaxValue;
            
            // Basic economic analysis
            // 1. Is it affordable without depleting resources?
            if (upgradeCost > playerMoney * 0.7f)
                return false; // Too expensive relative to current funds
            
            if (spawnAbility.limitUnits == cellGrid.GetPlayerUnits(player).Count(u => !u.GetComponent<ESUnit>().isStructure) && cellGrid.Turns[player.PlayerNumber, 0] > 5 + ((unit.name.Any(char.IsDigit) ? int.Parse(unit.name.Last(char.IsDigit).ToString()) : 0) * 5))
                return true; 
                
            // Is the ROI reasonable?
            if (turnsToBreakEven > 10 && cellGrid.Turns[player.PlayerNumber, 0] < 5)
                return false; // Not worth it early game with long ROI
                
            if (turnsToBreakEven > 15)
                return false; // Generally not worth it with very long ROI
                
            // Game phase considerations
            if (cellGrid.Turns[player.PlayerNumber, 0] < 3) // Very early game
            {
                // Early upgrade can be good for economy
                return turnsToBreakEven <= 7;
            }
            else if (cellGrid.Turns[player.PlayerNumber, 0] < 10) // Mid game
            {
                // More selective in mid-game
                return turnsToBreakEven <= 5 && playerMoney > upgradeCost * 1.5f;
            }
            else // Late game
            {
                // Late game - only if very good ROI or excess resources
                return turnsToBreakEven <= 3 || playerMoney > upgradeCost * 3;
            }
        }
        public override int GetActionIndex()
        {
            return 5; // Unique ID for move action
        }
    }
}