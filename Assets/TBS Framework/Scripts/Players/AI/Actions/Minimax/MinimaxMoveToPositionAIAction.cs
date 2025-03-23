using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using TbsFramework.Cells;
using TbsFramework.Grid;
using TbsFramework.Grid.GridStates;
using TbsFramework.Players.AI;
using TbsFramework.Players.AI.Evaluators;
using TbsFramework.Units;
using TbsFramework.Units.Abilities;
using UnityEngine;

namespace TbsFramework.Players.AI.Actions
{
    public class MinimaxMoveToPositionAIAction : MinimaxAIAction
    {
        public bool ShouldMoveAllTheWay = true;
        
        // Keep a separate reference for simulation vs execution
        private Cell _simulationDestination = null;
        private Cell _executionDestination = null;
        
        // Property to access the appropriate destination
        public Cell TopDestination 
        { 
            get { return _executionDestination; }
            private set { _executionDestination = value; }
        }

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
            // Reset destinations
            _simulationDestination = null;
            _executionDestination = null;
            
            // Continue with normal initialization
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
                UnityEngine.Debug.Log("Unit does not have a MoveAbility component attached.");
                return false;
            }

            var evaluators = GetComponents<CellEvaluator>();
            foreach (var e in evaluators)
            {
                stopWatch.Start();
                e.Precalculate(unit, player, cellGrid);
                stopWatch.Stop();

                executionTime["precalculate"][e.GetType().Name] = stopWatch.ElapsedMilliseconds;
                executionTime["evaluate"][e.GetType().Name] = 0;

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
                if ((player as MinimaxAIPlayer)?.DebugMode == true)
                {
                    cellMetadata[c] += string.Format("{0} * {1} = {2} : {3}\n", e.Weight.ToString("+0.00;-0.00"), score.ToString("+0.00;-0.00"), weightedScore.ToString("+0.00;-0.00"), e.GetType().ToString());
                }

                cellScoresDict[c] += weightedScore;
                return weightedScore;
            }).DefaultIfEmpty(0f).Aggregate((result, next) => result + next))).OrderByDescending(x => x.value);

            var movableCells = cellScores.Where(o => unit.IsCellMovableTo(o.cell));
            if (!movableCells.Any())
            {
                UnityEngine.Debug.Log("No movable cells found.");
                return false;
            }

            var (topCell, maxValue) = movableCells.First();
            var currentCellVal = cellScoresDict[unit.Cell];
            UnityEngine.Debug.Log($"Current cell value: {currentCellVal}, Top cell value: {maxValue}");

            if (maxValue >= currentCellVal)
            {
                // Store both destinations separately
                _simulationDestination = topCell;
                _executionDestination = topCell; // This will be refined in Precalculate
                UnityEngine.Debug.Log($"Moving to cell {_executionDestination.name} : {_executionDestination.OffsetCoord}");
                return true;
            }

            // Stay in place
            _simulationDestination = unit.Cell;
            _executionDestination = unit.Cell;
            UnityEngine.Debug.Log("No better cell found.");
            return false;
        }
        
        public override void Precalculate(Player player, Unit unit, CellGrid cellGrid)
        {
            UnityEngine.Debug.Log("Precalculating...");
            UnityEngine.Debug.Log("Current cell: " + unit.Cell.name + " " + unit.Cell.OffsetCoord);
            
            if (_executionDestination == null)
            {
                UnityEngine.Debug.LogError("TopDestination is null in Precalculate!");
                return;
            }
            
            UnityEngine.Debug.Log("Destination: " + _executionDestination.name + " " + _executionDestination.OffsetCoord);
            
            var path = unit.FindPath(cellGrid.Cells, _executionDestination);
            UnityEngine.Debug.Log("Path length: " + path.Count);
            UnityEngine.Debug.Log("Path:");
            foreach (var cell in path)
            {
                UnityEngine.Debug.Log(cell.OffsetCoord + ":" + cell.MovementCost);
            }
            
            List<Cell> selectedPath = new List<Cell>();
            float cost = 0;

            for (int i = path.Count - 1; i >= 0; i--)
            {
                var cell = path[i];
                cost += cell.MovementCost;
                UnityEngine.Debug.Log($"Checking cell {cell.OffsetCoord} with cost {cell.MovementCost} (total: {cost})");
                UnityEngine.Debug.Log($"Unit movement points: {unit.MovementPoints}");
                if (cost <= unit.MovementPoints)
                {
                    UnityEngine.Debug.Log($"Adding cell {cell.OffsetCoord} to selected path");
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
                _executionDestination = ShouldMoveAllTheWay ? selectedPath[0] : selectedPath.OrderByDescending(c => cellScoresDict[c]).First();
                UnityEngine.Debug.Log($"Final execution destination: {_executionDestination.OffsetCoord}");
            }
        }
        
        public override IEnumerator Execute(Player player, Unit unit, CellGrid cellGrid)
        {
            // Double check that the execution destination is valid
            if (_executionDestination == null)
            {
                UnityEngine.Debug.LogError("TopDestination is null in Execute!");
                yield break;
            }
            
            // If the unit is already at the destination (which shouldn't happen), do nothing
            if (unit.Cell == _executionDestination)
            {
                UnityEngine.Debug.LogWarning("Unit is already at destination. Skipping move action.");
                yield break;
            }
            
            // Extra verification to ensure the destination is movable to
            if (!unit.IsCellMovableTo(_executionDestination))
            {
                UnityEngine.Debug.LogError($"Cannot move to destination {_executionDestination.OffsetCoord}!");
                yield break;
            }
            
            UnityEngine.Debug.Log($"Moving unit {unit.UnitID} to {_executionDestination.OffsetCoord}");
            
            // Execute the move
            var moveAbility = unit.GetComponent<MoveAbility>();
            moveAbility.Destination = _executionDestination;
            yield return moveAbility.AIExecute(cellGrid);
        }
        
        public override void CleanUp(Player player, Unit unit, CellGrid cellGrid)
        {
            foreach (var cell in cellGrid.Cells)
            {
                cell.UnMark();
            }
            
            // Clear both destinations
            _simulationDestination = null;
            _executionDestination = null;
            
            if (cellGrid.cellGridState is CellGridStateAITurn aiTurnState)
            {
                aiTurnState.CellDebugInfo = null;
            }
        }
        
        public override void ShowDebugInfo(Player player, Unit unit, CellGrid cellGrid)
        {
            if (!(cellGrid.cellGridState is CellGridStateAITurn aiTurnState))
            {
                return;
            }

            Dictionary<Cell, DebugInfo> cellDebugInfo = new Dictionary<Cell, DebugInfo>();

            var maxScore = cellScores.Max(x => x.value);
            var minScore = cellScores.Min(x => x.value);

            maxScore = float.IsNaN(maxScore) ? 0 : maxScore;
            minScore = float.IsNaN(minScore) ? 0 : minScore;

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

            if (_executionDestination != null && cellDebugInfo.ContainsKey(_executionDestination))
            {
                cellDebugInfo[_executionDestination] = new DebugInfo(cellMetadata[_executionDestination], Color.magenta);
            }
            
            aiTurnState.CellDebugInfo = cellDebugInfo;

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

        // MINIMAX EXTENSION - Simulation method for minimax
        public override void SimulateAction(GameState state, Unit unit)
        {
            // Use the simulation destination, not the execution destination
            if (_simulationDestination == null || _simulationDestination == unit.Cell)
            {
                return;
            }
            
            // Find the unit in the state
            Unit stateUnit = state.Units.FirstOrDefault(u => u.UnitID == unit.UnitID);
            if (stateUnit == null)
            {
                return;
            }
            
            // Find destination cell in state's grid
            Vector2Int destPos = new Vector2Int(
                (int)_simulationDestination.OffsetCoord.x, 
                (int)_simulationDestination.OffsetCoord.y
            );
            
            Cell stateDestCell = state.CellGrid.Cells.FirstOrDefault(c => 
                c.OffsetCoord.x == destPos.x && c.OffsetCoord.y == destPos.y
            );
            
            if (stateDestCell == null)
            {
                return;
            }
            
            // Store original cell
            Cell stateOriginalCell = stateUnit.Cell;
            
            // Important: Only modify the unit and cells in the state, not the actual game objects
            if (stateOriginalCell != null)
            {
                // Create a new list to avoid modifying the original when removing
                stateOriginalCell.CurrentUnits = new List<Unit>(stateOriginalCell.CurrentUnits);
                stateOriginalCell.CurrentUnits.Remove(stateUnit);
            }
            
            // Add to new cell (again create a new list to avoid modifying the original)
            stateDestCell.CurrentUnits = new List<Unit>(stateDestCell.CurrentUnits);
            stateDestCell.CurrentUnits.Add(stateUnit);
            stateUnit.Cell = stateDestCell; // This only modifies the state's unit
            
            // Check for structure capture
            foreach (var esUnit in stateDestCell.CurrentUnits.OfType<ESUnit>().Where(u => u.isStructure))
            {
                if (esUnit.PlayerNumber != stateUnit.PlayerNumber)
                {
                    Vector2Int structurePos = new Vector2Int(
                        (int)stateDestCell.OffsetCoord.x, 
                        (int)stateDestCell.OffsetCoord.y
                    );
                    
                    // Find and update structure in state
                    foreach (var structure in state.Structures)
                    {
                        if (structure.Position.x == structurePos.x && structure.Position.y == structurePos.y)
                        {
                            structure.OwnerPlayerNumber = stateUnit.PlayerNumber;
                            
                            if (structure.Type == StructureType.Objective)
                            {
                                if (!state.ObjectiveControlTurns.ContainsKey(stateUnit.PlayerNumber))
                                {
                                    state.ObjectiveControlTurns[stateUnit.PlayerNumber] = 0;
                                }
                            }
                            break;
                        }
                    }
                }
            }
            
            // Consume action points
            stateUnit.ActionPoints = Mathf.Max(0, stateUnit.ActionPoints - 1);
            stateUnit.MovementPoints = 0;
            unit.Cell = stateOriginalCell;
            unit.ActionPoints = stateUnit.TotalActionPoints;
            unit.MovementPoints = stateUnit.TotalMovementPoints;
        }
        
        // Add GetActionIndex method for minimax
        public override int GetActionIndex()
        {
            return 1; // Unique ID for move action
        }
    }
}
