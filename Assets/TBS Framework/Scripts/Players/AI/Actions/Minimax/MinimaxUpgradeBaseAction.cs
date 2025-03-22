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
                state.Resources[unit.PlayerNumber] += unit.GetComponent<IncomeAbility>().Amount; // Represent future income advantage
            }
        }
        
        private bool ShouldUpgradeBase(Player player, Unit unit, CellGrid cellGrid, int playerMoney)
        {
            // ROI (Return on Investment) calculation
            int upgradeCost = upgradeAbility.upgradeCost;
  
            if (upgradeCost > playerMoney * 0.7f)
            {
                return false; // Too expensive relative to current funds
            }
            if (playerMoney > upgradeCost * 3)
            {
                return true; // Very affordable
            }
            if (spawnAbility.limitUnits == cellGrid.GetPlayerUnits(player).Count(u => !u.GetComponent<ESUnit>().isStructure) && 
                cellGrid.Turns[player.PlayerNumber, 0] > 5 + ((unit.name.Any(char.IsDigit) ? int.Parse(unit.name.Last(char.IsDigit).ToString()) : 0) * 5))
            {
                return true; 
            }
            return false;
        }
        public override int GetActionIndex()
        {
            return 5; // Unique ID for move action
        }
    }
}