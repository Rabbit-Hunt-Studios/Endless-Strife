using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using TbsFramework.Grid;
using TbsFramework.Grid.GridStates;
using TbsFramework.Players.AI;
using TbsFramework.Players.AI.Evaluators;
using TbsFramework.Units;
using TbsFramework.Units.Abilities;
using UnityEngine;

namespace TbsFramework.Players.AI.Actions
{
    public class MinimaxAttackAIAction : MinimaxAIAction
    {
        public Unit Target { get; private set; }
        private Dictionary<Unit, string> unitDebugInfo;
        private List<(Unit unit, float value)> unitScores;

        private Dictionary<string, Dictionary<string, float>> executionTime;
        private Stopwatch stopWatch = new Stopwatch();

        public override void InitializeAction(Player player, Unit unit, CellGrid cellGrid)
        {
            unit.GetComponent<AttackAbility>().OnAbilitySelected(cellGrid);

            executionTime = new Dictionary<string, Dictionary<string, float>>();
            executionTime.Add("precalculate", new Dictionary<string, float>());
            executionTime.Add("evaluate", new Dictionary<string, float>());
        }
        
        public override bool ShouldExecute(Player player, Unit unit, CellGrid cellGrid)
        {
            if (unit.GetComponent<AttackAbility>() == null)
            {
                return false;
            }

            var enemyUnits = cellGrid.GetEnemyUnits(player);
            if (enemyUnits.Count == 0)
            {
                return false;
            }

            var isEnemyInRange = enemyUnits.Any(u => unit.IsUnitAttackable(u, unit.Cell) && !u.GetComponent<ESUnit>().isStructure);
            return isEnemyInRange && unit.ActionPoints > 0;
        }
        
        public override void Precalculate(Player player, Unit unit, CellGrid cellGrid)
        {
            var enemyUnits = cellGrid.GetEnemyUnits(player);
            var enemiesInRange = enemyUnits.Where(e => unit.IsUnitAttackable(e, unit.Cell))
                                           .ToList();

            unitDebugInfo = new Dictionary<Unit, string>();
            enemyUnits.ForEach(u => unitDebugInfo[u] = "");

            if (enemiesInRange.Count == 0)
            {
                return;
            }

            var evaluators = GetComponents<UnitEvaluator>();
            foreach (var e in evaluators)
            {
                stopWatch.Start();
                e.Precalculate(unit, player, cellGrid);
                stopWatch.Stop();

                executionTime["precalculate"][e.GetType().Name] = stopWatch.ElapsedMilliseconds;
                executionTime["evaluate"][e.GetType().Name] = 0;

                stopWatch.Reset();
            }

            unitScores = enemiesInRange.Select(u => (unit: u, value: evaluators.Select(e =>
            {
                stopWatch.Start();
                var score = e.Evaluate(u, unit, player, cellGrid);
                stopWatch.Stop();
                executionTime["evaluate"][e.GetType().Name] += stopWatch.ElapsedMilliseconds;
                stopWatch.Reset();

                var weightedScore = score * e.Weight;
                unitDebugInfo[u] += string.Format("{0:+0.00;-0.00} * {1:+0.00;-0.00} = {2:+0.00;-0.00} : {3}\n", 
                                                  e.Weight, score, weightedScore, e.GetType().ToString());

                return weightedScore;
            }).DefaultIfEmpty(0f).Aggregate((result, next) => result + next))).ToList();
            
            unitScores.ForEach(s => unitDebugInfo[s.unit] += string.Format("Total: {0:0.00}", s.value));

            var (topUnit, maxValue) = unitScores.OrderByDescending(o => o.value)
                                                .First();

            Target = topUnit;
        }
        
        public override IEnumerator Execute(Player player, Unit unit, CellGrid cellGrid)
        {
            if (Target != null)
            {
                unit.GetComponent<AttackAbility>().UnitToAttack = Target;
                unit.GetComponent<AttackAbility>().UnitToAttackID = Target.UnitID;
                yield return StartCoroutine(unit.GetComponent<AttackAbility>().AIExecute(cellGrid));
                yield return new WaitForSeconds(0.5f);
            }
        }
        
        public override void CleanUp(Player player, Unit unit, CellGrid cellGrid)
        {
            foreach (var enemy in cellGrid.GetEnemyUnits(player))
            {
                enemy.UnMark();
            }
            Target = null;
            unitScores = null;
        }
        
        public override void ShowDebugInfo(Player player, Unit unit, CellGrid cellGrid)
        {
            if (!(cellGrid.cellGridState is CellGridStateAITurn aiTurnState))
            {
                return;
            }

            aiTurnState.UnitDebugInfo = unitDebugInfo;

            if (unitScores == null)
            {
                return;
            }

            var minScore = unitScores.DefaultIfEmpty().Min(e => e.value);
            var maxScore = unitScores.DefaultIfEmpty().Max(e => e.value);
            foreach (var (u, value) in unitScores)
            {
                var color = Color.Lerp(Color.red, Color.green, value >= 0 ? value / maxScore : value / minScore * (-1));
                u.SetColor(color);
            }

            if (Target != null)
            {
                Target.SetColor(Color.blue);
            }

            var evaluators = GetComponents<UnitEvaluator>();
            var sb = new StringBuilder();
            var sum = 0f;

            sb.AppendFormat("{0} evaluators execution time summary:\n", GetType().Name);
            foreach (var e in evaluators)
            {
                var precalculateTime = executionTime["precalculate"][e.GetType().Name];
                var evaluateTime = executionTime["evaluate"][e.GetType().Name];
                sum += precalculateTime + evaluateTime;

                sb.AppendFormat("total: {0}ms\tprecalculate: {1}ms\tevaluate: {2}ms\t:{3}\n",
                                (precalculateTime + evaluateTime).ToString().PadLeft(4),
                                precalculateTime.ToString().PadLeft(4),
                                evaluateTime.ToString().PadLeft(4),
                                e.GetType().Name);
            }
            sb.AppendFormat("sum: {0}ms", sum.ToString().PadLeft(4));
            UnityEngine.Debug.Log(sb.ToString());
        }
        
        // MINIMAX EXTENSION - Simulation method for minimax
        public override void SimulateAction(GameState state, Unit unit)
        {
            if (Target == null)
                return;
            
            // Find the attacking unit in the state
            Unit stateUnit = null;
            Unit stateTarget = null;
            
            foreach (var u in state.Units)
            {
                if (u.UnitID == unit.UnitID)
                {
                    stateUnit = u;
                }
                else if (u.UnitID == Target.UnitID)
                {
                    stateTarget = u;
                }
                
                if (stateUnit != null && stateTarget != null)
                    break;
            }
            
            if (stateUnit == null || stateTarget == null)
            {
                return;
            }

            // Simulate the attack - a simplified damage calculation
            if (unit.GetComponent<AttackAbility>() != null)
            {
                // Calculate attack damage (simplified)
                float damage = CalculateDamage(stateUnit, stateTarget);
                
                // Apply damage to target
                stateTarget.HitPoints -= (int)damage;
                
                // If target is defeated, remove it from the game state
                if (stateTarget.HitPoints <= 0)
                {
                    // If there's a cell reference, remove the unit from it
                    if (stateTarget.Cell != null)
                    {
                        stateTarget.Cell.CurrentUnits.Remove(stateTarget);
                    }
                    
                    // Remove from the state's unit list
                    state.Units.Remove(stateTarget);
                }
                
                // Consume action points
                stateUnit.ActionPoints = Mathf.Max(0, stateUnit.ActionPoints - 1);
            }
        }
        
        private float CalculateDamage(Unit attacker, Unit defender)
        {
            // Basic damage calculation formula - adjust according to your game's rules
            float baseDamage = attacker.AttackFactor;
            float mitigation = defender.DefenceFactor * 0.5f; // Defense reduces damage by half its value
            
            float finalDamage = Mathf.Max(1, baseDamage - mitigation); // Minimum 1 damage
            
            // Randomly vary damage slightly for simulation purposes
            float randomMultiplier = Random.Range(0.9f, 1.1f);
            return finalDamage * randomMultiplier;
        }
        
        // Add GetActionIndex method for minimax
        public override int GetActionIndex()
        {
            return 2; // Unique ID for attack action
        }
    }
}
