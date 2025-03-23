using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TbsFramework.Grid;
using TbsFramework.Grid.GridStates;
using TbsFramework.Players.AI;
using TbsFramework.Players.AI.Actions;
using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.Players
{
    public class MinimaxAIPlayer : AIPlayer
    {
        [Header("Minimax Settings")]
        public int maxDepth = 3;
        public bool useAlphaBetaPruning = true;
        
        [Header("Evaluation Weights")]
        public float unitHealthWeight = 2.0f;
        public float unitCountWeight = 5.0f;
        public float unitPositionWeight = 1.0f;
        public float resourcesWeight = 0.5f;
        public float structureControlWeight = 10.0f;
        public float objectiveControlWeight = 20.0f;
        
        // Cache for game state evaluation
        private Dictionary<string, float> stateEvaluationCache = new Dictionary<string, float>();
        
        // Action plan for current turn
        private List<MinimaxAIActionPlan> currentTurnPlan;
        
        private EconomyController economyController;
        private int enemyPlayerNumber;
        
        public override void Initialize(CellGrid cellGrid)
        {
            base.Initialize(cellGrid);
            cellGrid.LevelLoadingDone += OnLevelLoadingDone;
            
            economyController = UnityEngine.Object.FindObjectOfType<EconomyController>();
        }

        private void OnLevelLoadingDone(object sender, EventArgs e)
        {
            var cellGrid = sender as CellGrid;
            enemyPlayerNumber = cellGrid.Players.First(p => p.PlayerNumber != PlayerNumber).PlayerNumber;
        }
        
        public override void Play(CellGrid cellGrid)
        {
            cellGrid.cellGridState = new CellGridStateAITurn(cellGrid, this);
            
            // Run minimax to get the best action plan
            currentTurnPlan = RunMinimax(cellGrid, PlayerNumber, maxDepth);
            
            StartCoroutine(PlayMinimaxCoroutine(cellGrid));
        }
        
        private IEnumerator PlayMinimaxCoroutine(CellGrid cellGrid)
        {
            if (currentTurnPlan != null && currentTurnPlan.Count > 0)
            {
                foreach (var actionPlan in currentTurnPlan)
                {
                    Unit unit = actionPlan.Unit;
                    MinimaxAIAction action = actionPlan.Action;
                    
                    unit.MarkAsSelected();
                    yield return new WaitForSeconds(0.2f);
                    
                    if (DebugMode)
                    {
                        Debug.Log($"Executing planned action: {action.GetType().Name} for unit: {unit.name}");
                    }
                    
                    action.Precalculate(this, unit, cellGrid);
                    yield return StartCoroutine(action.Execute(this, unit, cellGrid));
                    action.CleanUp(this, unit, cellGrid);
                    
                    unit.MarkAsFriendly();
                    yield return new WaitForSeconds(0.3f);
                }
            }
            else
            {
                // Fallback to default AI behavior if minimax couldn't find a plan
                yield return StartCoroutine(PlayCoroutine(cellGrid));
            }
            
            cellGrid.EndTurn();
            yield return null;
        }
        
        private List<MinimaxAIActionPlan> RunMinimax(CellGrid cellGrid, int playerNumber, int depth)
        {
            // Clone the current state for simulation
            GameState currentState = new GameState(cellGrid, playerNumber);
            
            // Run minimax to find the best move
            float bestValue = float.MinValue;
            List<MinimaxAIActionPlan> bestPlan = null;
            
            // Get all possible action plans for this player
            List<List<MinimaxAIActionPlan>> allPossiblePlans = GetAllPossibleActionPlans(cellGrid, playerNumber);
            
            if (DebugMode)
            {
                Debug.Log($"Found {allPossiblePlans.Count} possible action plans");
            }
            
            // Evaluate each plan with minimax
            foreach (var plan in allPossiblePlans)
            {
                // Clone the state to apply this plan
                GameState clonedState = currentState.Clone();
                
                // Apply the plan to the cloned state
                ApplyActionPlan(clonedState, plan);
                
                // Evaluate with minimax
                float moveValue;
                if (useAlphaBetaPruning)
                {
                    moveValue = MinimaxAlphaBeta(clonedState, depth - 1, float.MinValue, float.MaxValue, false);
                }
                else
                {
                    moveValue = Minimax(clonedState, depth - 1, false);
                }
                
                if (moveValue > bestValue)
                {
                    bestValue = moveValue;
                    bestPlan = plan;
                }
            }
            
            if (DebugMode && bestPlan != null)
            {
                Debug.Log($"Selected best plan with value: {bestValue}, actions: {bestPlan.Count}");
                foreach (var action in bestPlan)
                {
                    Debug.Log($"  - {action.Action.GetType().Name} for {action.Unit.name}");
                }
            }
            
            return bestPlan;
        }
        
        private float Minimax(GameState state, int depth, bool isMaximizingPlayer)
        {
            // Terminal conditions
            if (depth == 0 || state.IsGameOver())
            {
                return EvaluateGameState(state);
            }
            
            int currentPlayer = isMaximizingPlayer ? PlayerNumber : enemyPlayerNumber;
            
            if (isMaximizingPlayer)
            {
                float maxEval = float.MinValue;
                List<List<MinimaxAIActionPlan>> allPossiblePlans = GetAllPossibleActionPlans(state.CellGrid, currentPlayer);
                
                foreach (var plan in allPossiblePlans)
                {
                    GameState clonedState = state.Clone();
                    ApplyActionPlan(clonedState, plan);
                    float eval = Minimax(clonedState, depth - 1, false);
                    maxEval = Mathf.Max(maxEval, eval);
                }
                
                return maxEval;
            }
            else
            {
                float minEval = float.MaxValue;
                List<List<MinimaxAIActionPlan>> allPossiblePlans = GetAllPossibleActionPlans(state.CellGrid, currentPlayer);
                
                foreach (var plan in allPossiblePlans)
                {
                    GameState clonedState = state.Clone();
                    ApplyActionPlan(clonedState, plan);
                    float eval = Minimax(clonedState, depth - 1, true);
                    minEval = Mathf.Min(minEval, eval);
                }
                
                return minEval;
            }
        }
        
        private float MinimaxAlphaBeta(GameState state, int depth, float alpha, float beta, bool isMaximizingPlayer)
        {
            // Terminal conditions
            if (depth == 0 || state.IsGameOver())
            {
                return EvaluateGameState(state);
            }
            
            int currentPlayer = isMaximizingPlayer ? PlayerNumber : enemyPlayerNumber;
            
            if (isMaximizingPlayer)
            {
                float maxEval = float.MinValue;
                List<List<MinimaxAIActionPlan>> allPossiblePlans = GetAllPossibleActionPlans(state.CellGrid, currentPlayer);
                
                foreach (var plan in allPossiblePlans)
                {
                    GameState clonedState = state.Clone();
                    ApplyActionPlan(clonedState, plan);
                    float eval = MinimaxAlphaBeta(clonedState, depth - 1, alpha, beta, false);
                    maxEval = Mathf.Max(maxEval, eval);
                    alpha = Mathf.Max(alpha, eval);
                    if (beta <= alpha)
                        break;
                }
                
                return maxEval;
            }
            else
            {
                float minEval = float.MaxValue;
                List<List<MinimaxAIActionPlan>> allPossiblePlans = GetAllPossibleActionPlans(state.CellGrid, currentPlayer);
                
                foreach (var plan in allPossiblePlans)
                {
                    GameState clonedState = state.Clone();
                    ApplyActionPlan(clonedState, plan);
                    float eval = MinimaxAlphaBeta(clonedState, depth - 1, alpha, beta, true);
                    minEval = Mathf.Min(minEval, eval);
                    beta = Mathf.Min(beta, eval);
                    if (beta <= alpha)
                        break;
                }
                
                return minEval;
            }
        }
        
        private List<List<MinimaxAIActionPlan>> GetAllPossibleActionPlans(CellGrid cellGrid, int playerNumber)
        {
            List<List<MinimaxAIActionPlan>> allPlans = new List<List<MinimaxAIActionPlan>>();
            
            // Get units for this player
            List<Unit> units = cellGrid.Units.Where(u => u.PlayerNumber == playerNumber).ToList();
            
            // Collect all possible actions for each unit
            Dictionary<Unit, List<MinimaxAIAction>> unitActions = new Dictionary<Unit, List<MinimaxAIAction>>();
            foreach (var unit in units)
            {
                if (unit == null || !unit.isActiveAndEnabled)
                {
                    continue;
                }
                Debug.Log($"Getting possible actions for unit {unit.name}");
                List<MinimaxAIAction> possibleActions = new List<MinimaxAIAction>();
                var actions = unit.GetComponentsInChildren<MinimaxAIAction>();
                
                foreach (var action in actions)
                {   
                    Debug.Log($"Checking action {action.GetType().Name}");
                    action.InitializeAction(this, unit, cellGrid);
                    if (action.ShouldExecute(this, unit, cellGrid))
                    {
                        Debug.Log($"Action {action.GetType().Name} is possible");
                        possibleActions.Add(action);
                    }
                }
                
                if (possibleActions.Count > 0)
                {
                    unitActions[unit] = possibleActions;
                }
            }
            // Generate all possible action combinations
            GenerateActionPlans(unitActions, new List<MinimaxAIActionPlan>(), allPlans, units);
            
            // If no plans were generated, create a "do nothing" plan
            if (allPlans.Count == 0)
            {
                allPlans.Add(new List<MinimaxAIActionPlan>());
            }
            
            return allPlans;
        }
        
        private void GenerateActionPlans(
            Dictionary<Unit, List<MinimaxAIAction>> unitActions, 
            List<MinimaxAIActionPlan> currentPlan, 
            List<List<MinimaxAIActionPlan>> allPlans,
            List<Unit> remainingUnits)
        {
            // Base case: no more units to process
            if (remainingUnits.Count == 0)
            {
                allPlans.Add(new List<MinimaxAIActionPlan>(currentPlan));
                return;
            }
            
            // Get the next unit
            Unit nextUnit = remainingUnits[0];
            List<Unit> newRemainingUnits = new List<Unit>(remainingUnits);
            newRemainingUnits.RemoveAt(0);
            
            // If this unit has no actions, skip it
            if (!unitActions.ContainsKey(nextUnit))
            {
                GenerateActionPlans(unitActions, currentPlan, allPlans, newRemainingUnits);
                return;
            }
            
            // Try each possible action for this unit
            foreach (var action in unitActions[nextUnit])
            {
                Debug.Log($"Trying action {action.GetType().Name} for unit {nextUnit.name}");
                List<MinimaxAIActionPlan> newPlan = new List<MinimaxAIActionPlan>(currentPlan);
                newPlan.Add(new MinimaxAIActionPlan { Unit = nextUnit, Action = action });
                GenerateActionPlans(unitActions, newPlan, allPlans, newRemainingUnits);
            }
            
            // Also try skipping this unit (not taking any action)
            GenerateActionPlans(unitActions, currentPlan, allPlans, newRemainingUnits);
        }
        
        private void ApplyActionPlan(GameState state, List<MinimaxAIActionPlan> plan)
        {
            // Apply each action in the plan to the state
            foreach (var actionPlan in plan)
            {
                // Simulate the action's effect on the game state
                actionPlan.Action.SimulateAction(state, actionPlan.Unit);
            }
        }
        
        private float EvaluateGameState(GameState state)
        {
            // Check if this state is already in the cache
            string stateHash = state.GetHashCode().ToString();
            if (stateEvaluationCache.ContainsKey(stateHash))
            {
                return stateEvaluationCache[stateHash];
            }
            
            float score = 0;
            
            // Game outcome (highest priority)
            if (state.IsGameOver())
            {
                if (state.Winner == PlayerNumber)
                    return float.MaxValue;
                else if (state.Winner == enemyPlayerNumber)
                    return float.MinValue;
            }
            
            // Unit count and health
            float allyUnitValue = 0;
            float enemyUnitValue = 0;
            
            foreach (var unit in state.Units)
            {
                if (unit == null || !unit.isActiveAndEnabled)
                    continue;
                float unitValue = unit.HitPoints * unitHealthWeight;
                
                // // Add value for merged units
                // var mergedUnits = unit.GetComponent<MergedUnits>();
                // if (mergedUnits != null)
                // {
                //     unitValue += mergedUnits.GetMergedUnitCount() * 10;
                // }
                
                if (unit.PlayerNumber == PlayerNumber)
                    allyUnitValue += unitValue;
                else if (unit.PlayerNumber == enemyPlayerNumber)
                    enemyUnitValue += unitValue;
            }
            
            score += (allyUnitValue - enemyUnitValue) * unitCountWeight;
            
            // Resources
            if (state.Resources.ContainsKey(PlayerNumber) && state.Resources.ContainsKey(enemyPlayerNumber))
            {
                float resourceDiff = state.Resources[PlayerNumber] - state.Resources[enemyPlayerNumber];
                score += resourceDiff * resourcesWeight;
            }
            
            // Structure control
            foreach (var structure in state.Structures)
            {
                // Outposts
                if (structure.Type == StructureType.Outpost)
                {
                    if (structure.OwnerPlayerNumber == PlayerNumber)
                        score += structureControlWeight;
                    else if (structure.OwnerPlayerNumber == enemyPlayerNumber)
                        score -= structureControlWeight;
                }
                // Enemy base
                else if (structure.Type == StructureType.Base && 
                         structure.OriginalOwnerPlayerNumber == enemyPlayerNumber)
                {
                    if (structure.OwnerPlayerNumber == PlayerNumber)
                        score += structureControlWeight * 5;
                }
                // Main objective
                else if (structure.Type == StructureType.Objective)
                {
                    if (structure.OwnerPlayerNumber == PlayerNumber)
                    {
                        score += objectiveControlWeight;
                        score += state.ObjectiveControlTurns[PlayerNumber] * objectiveControlWeight;
                    }
                    else if (structure.OwnerPlayerNumber == enemyPlayerNumber)
                    {
                        score -= objectiveControlWeight;
                        score -= state.ObjectiveControlTurns[enemyPlayerNumber] * objectiveControlWeight;
                    }
                }
            }
            
            // Unit positioning relative to objectives
            float allyPositionValue = 0;
            float enemyPositionValue = 0;
            
            // Find objective position
            Vector2Int objectivePos = new Vector2Int(state.CellGrid.Cells.Count / 2, state.CellGrid.Cells.Count / 2);
            foreach (var structure in state.Structures)
            {
                if (structure.Type == StructureType.Objective)
                {
                    objectivePos = new Vector2Int(structure.Position.x, structure.Position.y);
                    break;
                }
            }
            
            // Calculate positional advantage
            foreach (var unit in state.Units)
            {
                if (unit == null || !unit.isActiveAndEnabled)
                    continue;
                    
                if (unit is ESUnit esUnit && esUnit.isStructure)
                    continue;
                
                float distance = Vector2Int.Distance(
                    new Vector2Int((int)unit.Cell.OffsetCoord.x, (int)unit.Cell.OffsetCoord.y), 
                    objectivePos);
                
                float positionValue = 10 / (distance + 1); // Closer is better
                
                if (unit.PlayerNumber == PlayerNumber)
                    allyPositionValue += positionValue;
                else if (unit.PlayerNumber == enemyPlayerNumber)
                    enemyPositionValue += positionValue;
            }
            
            score += (allyPositionValue - enemyPositionValue) * unitPositionWeight;
            
            // Cache the evaluation
            stateEvaluationCache[stateHash] = score;
            
            return score;
        }
    }
    
    public class MinimaxAIActionPlan
    {
        public Unit Unit;
        public MinimaxAIAction Action;
    }
}