using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using TbsFramework.Grid;
using TbsFramework.Grid.GridStates;
using TbsFramework.Players.AI.Evaluators;
using TbsFramework.Units;
using TbsFramework.Units.Abilities;
using UnityEngine;

namespace TbsFramework.Players.AI.Actions
{
    public class AttackAIAction : AIAction
    {
        private Unit Target;
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
            var isEnemyinRange = enemyUnits.Select(u => unit.IsUnitAttackable(u, unit.Cell))
                                           .Aggregate((result, next) => result || next);

            return isEnemyinRange && unit.ActionPoints > 0;
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

                executionTime["precalculate"].Add(e.GetType().Name, stopWatch.ElapsedMilliseconds);
                executionTime["evaluate"].Add(e.GetType().Name, 0);

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
                unitDebugInfo[u] += string.Format("{0:+0.00;-0.00} * {1:+0.00;-0.00} = {2:+0.00;-0.00} : {3}\n", e.Weight, score, weightedScore, e.GetType().ToString());

                return weightedScore;
            }).DefaultIfEmpty(0f).Aggregate((result, next) => result + next))).ToList();
            unitScores.ToList().ForEach(s => unitDebugInfo[s.unit] += string.Format("Total: {0:0.00}", s.value));

            var (topUnit, maxValue) = unitScores.OrderByDescending(o => o.value)
                                                .First();

            Target = topUnit;
        }
        public override IEnumerator Execute(Player player, Unit unit, CellGrid cellGrid)
        {
            unit.GetComponent<AttackAbility>().UnitToAttack = Target;
            unit.GetComponent<AttackAbility>().UnitToAttackID = Target.UnitID;
            yield return StartCoroutine(unit.GetComponent<AttackAbility>().AIExecute(cellGrid));
            yield return new WaitForSeconds(0.5f);
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
            (cellGrid.cellGridState as CellGridStateAITurn).UnitDebugInfo = unitDebugInfo;

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
    }
}