using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using TbsFramework.Cells;
using TbsFramework.Grid;
using TbsFramework.Grid.GridStates;
using TbsFramework.Players.AI.Evaluators;
using TbsFramework.Units;
using TbsFramework.Units.Abilities;
using UnityEngine;

namespace TbsFramework.Players.AI.Actions
{
    public class MoveToPositionAIAction : AIAction
    {
        public bool ShouldMoveAllTheWay = true;
        private Cell TopDestination = null;

        private Dictionary<Cell, string> cellMetadata;
        private IEnumerable<(Cell cell, float value)> cellScores;
        private Dictionary<Cell, float> cellScoresDict;
        private Dictionary<string, Dictionary<string, float>> executionTime;

        private Gradient DebugGradient;
        private Stopwatch stopWatch = new Stopwatch();

        private void Awake()
        {
            var colorKeys = new GradientColorKey[3];

            colorKeys[0] = new GradientColorKey(Color.red, 0.2f);
            colorKeys[1] = new GradientColorKey(Color.yellow, 0.5f);
            colorKeys[2] = new GradientColorKey(Color.green, 0.8f);

            DebugGradient = new Gradient();
            DebugGradient.SetKeys(colorKeys, new GradientAlphaKey[0]);
        }

        public override void InitializeAction(Player player, Unit unit, CellGrid cellGrid)
        {
            unit.GetComponent<MoveAbility>().OnAbilitySelected(cellGrid);

            cellMetadata = new Dictionary<Cell, string>();
            cellScoresDict = new Dictionary<Cell, float>();
            cellGrid.Cells.ForEach(c =>
            {
                cellMetadata[c] = "";
                cellScoresDict[c] = 0f;
            });

            executionTime = new Dictionary<string, Dictionary<string, float>>();
            executionTime.Add("precalculate", new Dictionary<string, float>());
            executionTime.Add("evaluate", new Dictionary<string, float>());
        }

        public override bool ShouldExecute(Player player, Unit unit, CellGrid cellGrid)
        {
            if (unit.GetComponent<MoveAbility>() == null)
            {
                return false;
            }

            var evaluators = GetComponents<CellEvaluator>();
            foreach (var e in evaluators)
            {
                stopWatch.Start();
                e.Precalculate(unit, player, cellGrid);
                stopWatch.Stop();

                executionTime["precalculate"].Add(e.GetType().Name, stopWatch.ElapsedMilliseconds);
                executionTime["evaluate"].Add(e.GetType().Name, 0);

                stopWatch.Reset();
            }

            cellScores = cellGrid.Cells.Select(c => (cell: c, value: evaluators.Select(e =>
            {
                stopWatch.Start();
                var score = e.Evaluate(c, unit, player, cellGrid);
                stopWatch.Stop();
                executionTime["evaluate"][e.GetType().Name] += stopWatch.ElapsedMilliseconds;
                stopWatch.Reset();

                var weightedScore = score * e.Weight;
                if ((player as AIPlayer).DebugMode)
                {
                    cellMetadata[c] += string.Format("{0} * {1} = {2} : {3}\n", e.Weight.ToString("+0.00;-0.00"), score.ToString("+0.00;-0.00"), weightedScore.ToString("+0.00;-0.00"), e.GetType().ToString());
                }

                cellScoresDict[c] += weightedScore;
                return weightedScore;
            }).DefaultIfEmpty(0f).Aggregate((result, next) => result + next))).OrderByDescending(x => x.value);

            var (topCell, maxValue) = cellScores.Where(o => unit.IsCellMovableTo(o.cell))
                                                .First();

            var currentCellVal = cellScoresDict[unit.Cell];

            if (maxValue > currentCellVal)
            {
                TopDestination = topCell;
                return true;
            }

            TopDestination = unit.Cell;
            return false;
        }
        public override void Precalculate(Player player, Unit unit, CellGrid cellGrid)
        {
            var path = unit.FindPath(cellGrid.Cells, TopDestination);
            List<Cell> selectedPath = new List<Cell>();
            float cost = 0;

            for (int i = path.Count - 1; i >= 0; i--)
            {
                var cell = path[i];
                cost += cell.MovementCost;
                if (cost <= unit.MovementPoints)
                {
                    selectedPath.Add(cell);
                }
                else
                {
                    for (int j = selectedPath.Count - 1; j >= 0; j--)
                    {
                        if (!unit.IsCellMovableTo(selectedPath[j]))
                        {
                            selectedPath.RemoveAt(j);
                        }
                        else
                        {
                            break;
                        }
                    }
                    break;
                }
            }
            selectedPath.Reverse();

            if (selectedPath.Count != 0)
            {
                TopDestination = ShouldMoveAllTheWay ? selectedPath[0] : selectedPath.OrderByDescending(c => cellScoresDict[c]).First();
            }
        }
        public override IEnumerator Execute(Player player, Unit unit, CellGrid cellGrid)
        {
            unit.GetComponent<MoveAbility>().Destination = TopDestination;
            yield return unit.GetComponent<MoveAbility>().AIExecute(cellGrid);
        }
        public override void CleanUp(Player player, Unit unit, CellGrid cellGrid)
        {
            foreach (var cell in cellGrid.Cells)
            {
                cell.UnMark();
            }
            TopDestination = null;
            (cellGrid.cellGridState as CellGridStateAITurn).CellDebugInfo = null;
        }
        public override void ShowDebugInfo(Player player, Unit unit, CellGrid cellGrid)
        {
            Dictionary<Cell, DebugInfo> cellDebugInfo = new Dictionary<Cell, DebugInfo>();

            var maxScore = cellScores.Max(x => x.value);
            var minScore = cellScores.Min(x => x.value);

            maxScore = maxScore.Equals(float.NaN) ? 0 : maxScore;
            minScore = minScore.Equals(float.NaN) ? 0 : minScore;

            var cellScoresEnumerator = cellScores.GetEnumerator();
            cellScoresEnumerator.MoveNext();
            var (topCell, _) = cellScoresEnumerator.Current;
            cellDebugInfo[topCell] = new DebugInfo(cellMetadata[topCell], Color.blue);

            while (cellScoresEnumerator.MoveNext())
            {
                var (cell, value) = cellScoresEnumerator.Current;

                var color = DebugGradient.Evaluate((value - minScore) / (Mathf.Abs(maxScore - minScore) + float.Epsilon));
                cellMetadata[cell] += string.Format("Total: {0}", cellScoresDict[cell].ToString("0.00"));
                cellDebugInfo[cell] = new DebugInfo(cellMetadata[cell], color);
            }

            cellScoresEnumerator.Dispose();

            cellDebugInfo[TopDestination].Color = Color.magenta;
            (cellGrid.cellGridState as CellGridStateAITurn).CellDebugInfo = cellDebugInfo;

            var evaluators = GetComponents<CellEvaluator>();
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
