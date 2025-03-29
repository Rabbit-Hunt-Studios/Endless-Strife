using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TbsFramework.Cells;
using TbsFramework.Grid;
using TbsFramework.Grid.GridStates;
using TbsFramework.Players.AI;
using TbsFramework.Players.AI.Actions;
using TbsFramework.Units;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;

namespace TbsFramework.Players
{
    public class RLMinimaxPlayer : AIPlayer
    {
        // Minimax Settings
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
        
        // RL model parameters
        [Header("RL Parameters")]
        public float learningRate = 0.001f;
        public float discountFactor = 0.99f;
        public float explorationRate = 0.1f;
        public int batchSize = 32;
        public int replayBufferCapacity = 10000;
        public int hiddenLayerSize = 256;
        
        // Training mode
        [Header("Training Settings")]
        public bool isTraining = true;
        public bool loadModelOnStart = false;
        public string modelFilename = "rl_minimax_model.json";
        public bool debugMode = false;
        
        // Visualization and data collection
        [Header("Data Collection")]
        public bool collectTrainingData = true;
        public int saveMetricsInterval = 10; // Save metrics every X games
        public bool exportWeightHistograms = true;
        
        // Reward weights
        [Header("Reward Weights")]
        public float damageDealtRewardWeight = 0.5f;
        public float unitKillRewardWeight = 3.0f;
        public float outpostCaptureRewardWeight = 5.0f;
        public float objectiveCaptureRewardWeight = 10.0f;
        public float baseCaptureRewardWeight = 50.0f;
        public float objectiveControlTurnRewardWeight = 2.0f;
        public float baseUpgradeRewardWeight = 15.0f;
        public float unitProductionRewardWeight = 2.0f;
        public float winRewardWeight = 100.0f;
        public float loseRewardWeight = -100.0f;
        
        // RL system
        private NeuralNetwork policyNetwork;
        private ReplayBuffer replayBuffer;
        private float[] RLcurrentState;
        private TrainingMetrics metrics;
        
        // Minimax system
        private Dictionary<string, float> stateEvaluationCache = new Dictionary<string, float>();
        private List<MinimaxAIActionPlan> currentTurnPlan;
        
        // Game state tracking
        private Dictionary<Unit, float> unitHP;
        private HashSet<ESUnit> capturedStructures;
        private int objectiveControlTurns = 0;
        public float totalReward = 0f;
        private float currentEpisodeReward = 0f;
        private Dictionary<ESUnit, int> baseUpgradeLevels = new Dictionary<ESUnit, int>();
        private Dictionary<ESUnit, List<ESUnit>> spawnedUnits = new Dictionary<ESUnit, List<ESUnit>>();
        private int structuresCapturedThisGame = 0;
        
        // References
        private EconomyController economyController;
        private int enemyPlayerNumber;
        
        public override void Initialize(CellGrid cellGrid)
        {
            base.Initialize(cellGrid);
            cellGrid.GameEnded += OnGameEnded;
            cellGrid.TurnEnded += OnTurnEnded;
            cellGrid.LevelLoadingDone += OnLevelLoadingDone;
            
            // Initialize tracking structures
            unitHP = new Dictionary<Unit, float>();
            capturedStructures = new HashSet<ESUnit>();
            spawnedUnits = new Dictionary<ESUnit, List<ESUnit>>();
            
            economyController = UnityEngine.Object.FindObjectOfType<EconomyController>();
            
            // Try to load existing metrics
            string metricsPath = Path.Combine(Application.persistentDataPath, $"training_metrics_{PlayerNumber}.json");
            if (File.Exists(metricsPath))
            {
                Debug.LogWarning($"Loading training metrics from {metricsPath}");
                try
                {
                    string metricsJson = File.ReadAllText(metricsPath);
                    metrics = JsonConvert.DeserializeObject<TrainingMetrics>(metricsJson);
                    Debug.Log($"Loaded training metrics from {metricsPath}");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to load training metrics: {ex.Message}");
                    metrics = new TrainingMetrics();
                }
            }
            else
            {
                Debug.LogWarning($"No training metrics found at {metricsPath}");
                metrics = new TrainingMetrics();
            }
        }
        
        private void OnLevelLoadingDone(object sender, EventArgs e)
        {
            currentEpisodeReward = 0f;
            structuresCapturedThisGame = 0;
            
            var cellGrid = sender as CellGrid;
            enemyPlayerNumber = cellGrid.Players.First(p => p.PlayerNumber != PlayerNumber).PlayerNumber;
            
            // Calculate state and action sizes
            int stateSize = CalculateStateSize(cellGrid);
            int actionSize = CalculateActionSize(cellGrid);
            
            // Initialize neural network
            policyNetwork = new NeuralNetwork(stateSize, actionSize, hiddenLayerSize);
            
            if (loadModelOnStart)
            {
                try
                {
                    policyNetwork.LoadModel(modelFilename);
                    Debug.Log($"Model loaded from {modelFilename}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Could not load model: {modelFilename} {ex.Message}. Using new model.");
                }
            }
            
            replayBuffer = new ReplayBuffer(replayBufferCapacity);

            // Track initial state
            foreach (var unit in cellGrid.Units)
            {
                if (unit == null || !unit.isActiveAndEnabled)
                    continue;
                ESUnit esUnit = unit as ESUnit;
                
                // Initialize tracking for base units
                if (esUnit != null && esUnit.PlayerNumber == PlayerNumber && esUnit.isStructure)
                {
                    baseUpgradeLevels[esUnit] = 0;
                    spawnedUnits[esUnit] = new List<ESUnit>();
                }
                
                // Track initial health
                unitHP[unit] = unit.HitPoints;
            }
            
            if (debugMode)
            {
                Debug.Log("RLMinimaxPlayer initialization completed.");
            }
        }
        
        private void OnTurnEnded(object sender, bool isNetworkInvoked)
        {
            // Update objective control tracking
            var cellGrid = sender as CellGrid;
            
            if (IsControllingObjective(cellGrid))
            {
                objectiveControlTurns++;
            }
            
            if (collectTrainingData)
            {
                // Record strategic metrics at the end of each turn
                int unitDiff = CountPlayerUnits(cellGrid, PlayerNumber) - CountPlayerUnits(cellGrid, enemyPlayerNumber);
                float resourceDiff = 0;
                if (economyController != null)
                {
                    resourceDiff = economyController.GetValue(PlayerNumber) - economyController.GetValue(enemyPlayerNumber);
                }
                
                metrics.RecordStrategicMetrics(
                    objectiveControlTurns,
                    unitDiff,
                    resourceDiff,
                    structuresCapturedThisGame
                );
            }
        }
        
        private int CountPlayerUnits(CellGrid cellGrid, int playerNumber)
        {
            return cellGrid.Units.Count(u => u.PlayerNumber == playerNumber && 
                                      u.isActiveAndEnabled && 
                                      !(u is ESUnit esUnit && esUnit.isStructure));
        }
        
        public override void Play(CellGrid cellGrid)
        {
            cellGrid.cellGridState = new CellGridStateAITurn(cellGrid, this);
            
            // Capture the current state for RL
            RLcurrentState = GetStateRepresentation(cellGrid);
            
            // Run minimax to get the best action plan
            currentTurnPlan = RunMinimax(cellGrid, PlayerNumber, maxDepth);
            
            StartCoroutine(PlayRLMinimaxCoroutine(cellGrid));
        }
        
        private IEnumerator PlayRLMinimaxCoroutine(CellGrid cellGrid)
        {
            float turnReward = 0f;
            
            if (currentTurnPlan != null && currentTurnPlan.Count > 0)
            {
                foreach (var actionPlan in currentTurnPlan)
                {
                    Unit unit = actionPlan.Unit;
                    MinimaxAIAction action = actionPlan.Action;
                    
                    if (unit == null || !unit.isActiveAndEnabled)
                        continue;
                        
                    ESUnit esUnit = unit as ESUnit;
                    
                    unit.MarkAsSelected();
                    yield return new WaitForSeconds(0.2f);
                    
                    if (debugMode)
                    {
                        Debug.Log($"Executing planned action: {action.GetType().Name} for unit: {unit.name}");
                    }
                    
                    // Record unit state before action
                    Dictionary<Unit, float> preActionUnitHP = new Dictionary<Unit, float>();
                    foreach (var u in cellGrid.Units)
                    {
                        if (u != null && u.isActiveAndEnabled)
                            preActionUnitHP[u] = u.HitPoints;
                    }
                    
                    // Record structure state before action
                    HashSet<ESUnit> preActionCapturedStructures = new HashSet<ESUnit>(capturedStructures);
                    bool wasControllingObjective = IsControllingObjective(cellGrid);
                    Cell unitDestinationCell = null;
                    
                    // Special handling for move actions to track destination
                    if (action is MinimaxMoveToPositionAIAction moveAction)
                    {
                        moveAction.Precalculate(this, unit, cellGrid);
                        unitDestinationCell = moveAction.TopDestination;
                    }
                    
                    // Execute the action
                    yield return StartCoroutine(action.Execute(this, unit, cellGrid));
                    
                    // Calculate rewards based on action outcome
                    float actionReward = CalculateActionReward(
                        esUnit, 
                        action, 
                        cellGrid, 
                        preActionUnitHP, 
                        preActionCapturedStructures,
                        wasControllingObjective,
                        unitDestinationCell
                    );
                    
                    turnReward += actionReward;
                    totalReward += actionReward;
                    currentEpisodeReward += actionReward;
                    
                    // Update metrics for this action
                    if (collectTrainingData)
                    {
                        metrics.RecordAction(action.GetActionIndex(), actionReward);
                    }
                    
                    // Record for RL system
                    if (isTraining)
                    {
                        // Get new state and reward
                        float[] newState = GetStateRepresentation(cellGrid);
                        
                        // Store experience in replay buffer
                        replayBuffer.AddExperience(RLcurrentState, action.GetActionIndex(), actionReward, newState, cellGrid.GameFinished);

                        // Learn from batch of experiences
                        if (replayBuffer.Size() > batchSize)
                        {
                            LearnFromExperiences();
                        }
                        
                        // Update current state
                        RLcurrentState = newState;
                    }
                    
                    action.CleanUp(this, unit, cellGrid);
                    
                    if (debugMode)
                    {
                        Debug.Log($"Action: {action.GetType().Name}, Reward: {actionReward:F2}");
                    }
                    
                    unit.MarkAsFriendly();
                    yield return new WaitForSeconds(0.3f);
                }
            }
            else
            {
                // Fallback to simpler behavior if minimax couldn't find a plan
                yield return StartCoroutine(PlayFallbackCoroutine(cellGrid));
            }
            
            if (debugMode)
            {
                Debug.Log($"Player {PlayerNumber} - Turn complete, Turn reward: {turnReward:F2}, Episode reward: {currentEpisodeReward:F2}, Total reward: {totalReward:F2}");
            }
            
            // Update training metrics
            if (collectTrainingData)
            {
                metrics.RecordEpisodeMetrics(
                    turnReward,  // Reward for this turn/episode
                    totalReward, // Cumulative reward
                    explorationRate // Current exploration rate
                );
            }
            
            cellGrid.EndTurn();
            yield return null;
        }
        
        private IEnumerator PlayFallbackCoroutine(CellGrid cellGrid)
        {
            // Simple fallback if minimax fails
            var unitsOrdered = GetComponent<UnitSelection>().SelectNext(() => cellGrid.GetCurrentPlayerUnits(), cellGrid);
            
            foreach (var unit in unitsOrdered)
            {
                if (unit == null || !unit.isActiveAndEnabled)
                {
                    continue;
                }
                unit.MarkAsSelected();
                
                var AIActions = unit.GetComponentsInChildren<MinimaxAIAction>();
                foreach (var aiAction in AIActions)
                {
                    yield return null;
                    
                    aiAction.InitializeAction(this, unit, cellGrid);
                    
                    if (aiAction.ShouldExecute(this, unit, cellGrid))
                    {
                        aiAction.Precalculate(this, unit, cellGrid);
                        yield return StartCoroutine(aiAction.Execute(this, unit, cellGrid));
                    }
                    
                    aiAction.CleanUp(this, unit, cellGrid);
                }
                
                unit.MarkAsFriendly();
                yield return new WaitForSeconds(0.2f);
            }
        }
        
        private List<MinimaxAIActionPlan> RunMinimax(CellGrid cellGrid, int playerNumber, int depth)
        {
            // Check if we're using RL to guide minimax search
            bool useRLGuidance = policyNetwork != null && (isTraining ? UnityEngine.Random.value > explorationRate : true);
            
            // Clone the current state for simulation
            GameState currentState = new GameState(cellGrid, playerNumber);
            
            // Run minimax to find the best move
            float bestValue = float.MinValue;
            List<MinimaxAIActionPlan> bestPlan = null;
            
            // Get all possible action plans for this player
            List<List<MinimaxAIActionPlan>> allPossiblePlans = GetAllPossibleActionPlans(cellGrid, playerNumber);
            
            if (debugMode)
            {
                Debug.Log($"Found {allPossiblePlans.Count} possible action plans");
            }
            
            // If using RL guidance, sort plans by policy prediction
            if (useRLGuidance && allPossiblePlans.Count > 0)
            {
                RLcurrentState = GetStateRepresentation(cellGrid);
                float[] actionValues = policyNetwork.Predict(RLcurrentState);
                
                if (collectTrainingData)
                {
                    // Record the Q-values for visualization
                    metrics.averageQValues.Add(actionValues.Average());
                    metrics.actionCounts.Add(actionValues.Length);
                }
                
                // Define a scoring function for plans based on RL policy
                float GetPlanScore(List<MinimaxAIActionPlan> plan)
                {
                    float score = 0;
                    foreach (var actionPlan in plan)
                    {
                        int actionIndex = actionPlan.Action.GetActionIndex();
                        if (actionIndex >= 0 && actionIndex < actionValues.Length)
                        {
                            score += actionValues[actionIndex];
                        }
                    }
                    return score;
                }
                
                // Sort plans by RL policy prediction
                allPossiblePlans = allPossiblePlans.OrderByDescending(GetPlanScore).ToList();
                
                // Optionally, prune the search space to only consider top N plans
                int topPlansToConsider = Mathf.Min(5, allPossiblePlans.Count);
                allPossiblePlans = allPossiblePlans.Take(topPlansToConsider).ToList();
            }
            
            // Evaluate each plan with minimax
            foreach (var plan in allPossiblePlans)
            {
                // Clone the state to apply this plan
                GameState clonedState = currentState.Clone();
                
                // Apply the plan to the cloned state
                ApplyActionPlan(clonedState, plan);
                
                // Evaluate with minimax or alpha-beta pruning
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
            
            if (debugMode && bestPlan != null)
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
            List<Unit> units = cellGrid.Units.Where(u => u.PlayerNumber == playerNumber && u.isActiveAndEnabled).ToList();
            
            // Collect all possible actions for each unit
            Dictionary<Unit, List<MinimaxAIAction>> unitActions = new Dictionary<Unit, List<MinimaxAIAction>>();
            foreach (var unit in units)
            {
                if (unit == null || !unit.isActiveAndEnabled)
                {
                    continue;
                }
                if (debugMode)
                {
                    Debug.Log($"Getting possible actions for unit {unit.name}");
                }
                
                List<MinimaxAIAction> possibleActions = new List<MinimaxAIAction>();
                var actions = unit.GetComponentsInChildren<MinimaxAIAction>();
                
                foreach (var action in actions)
                {
                    if (debugMode)
                    {
                        Debug.Log($"Checking action {action.GetType().Name}");
                    }
                    
                    action.InitializeAction(this, unit, cellGrid);
                    if (action.ShouldExecute(this, unit, cellGrid))
                    {
                        if (debugMode)
                        {
                            Debug.Log($"Action {action.GetType().Name} is possible");
                        }
                        possibleActions.Add(action);
                    }
                }
                
                if (possibleActions.Count > 0)
                {
                    unitActions[unit] = possibleActions;
                }
            }
            
            // Generate all possible action combinations (limited to prevent combinatorial explosion)
            const int MAX_UNITS_TO_CONSIDER = 3; // Limit to prevent too many combinations
            
            if (units.Count > MAX_UNITS_TO_CONSIDER)
            {
                // Sort units by importance
                units = units.OrderByDescending(u => {
                    ESUnit esUnit = u as ESUnit;
                    float importance = 0;
                    
                    // Prioritize damaged units, high-value units, and units near objectives
                    if (esUnit != null && !esUnit.isStructure)
                    {
                        importance += (1 - (u.HitPoints / u.TotalHitPoints)) * 20; // Damaged units
                        importance += (esUnit.AttackFactor + esUnit.DefenceFactor) * 0.5f; // Strong units
                        
                        // Find distance to objective
                        Cell objectiveCell = FindObjectiveCell(cellGrid);
                        if (objectiveCell != null)
                        {
                            float distance = Vector3.Distance(u.Cell.transform.position, objectiveCell.transform.position);
                            importance += 10 / (distance + 1);
                        }
                        
                        // Prioritize units that can take actions
                        if (unitActions.ContainsKey(u))
                        {
                            importance += 5;
                        }
                    }
                    
                    return importance;
                }).Take(MAX_UNITS_TO_CONSIDER).ToList();
            }
            
            // Generate action plans with the selected units
            GenerateActionPlans(unitActions, new List<MinimaxAIActionPlan>(), allPlans, units);
            
            // If no plans were generated, create a "do nothing" plan
            if (allPlans.Count == 0)
            {
                allPlans.Add(new List<MinimaxAIActionPlan>());
            }
            
            return allPlans;
        }
        
        private Cell FindObjectiveCell(CellGrid cellGrid)
        {
            foreach (var cell in cellGrid.Cells)
            {
                foreach (var unit in cell.CurrentUnits)
                {
                    if (unit == null || !unit.isActiveAndEnabled)
                        continue;
                    ESUnit esUnit = unit as ESUnit;
                    if (esUnit != null && esUnit.UnitName.Contains("Objective"))
                    {
                        return cell;
                    }
                }
            }
            return null;
        }
        
        private void GenerateActionPlans(
            Dictionary<Unit, List<MinimaxAIAction>> unitActions,
            List<MinimaxAIActionPlan> currentPlan,
            List<List<MinimaxAIActionPlan>> allPlans,
            List<Unit> remainingUnits)
        {
            // Limit the total number of plans to prevent explosion
            if (allPlans.Count >= 100)
            {
                return;
            }
            
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
                if (debugMode)
                {
                    Debug.Log($"Trying action {action.GetType().Name} for unit {nextUnit.name}");
                }
                
                List<MinimaxAIActionPlan> newPlan = new List<MinimaxAIActionPlan>(currentPlan);
                newPlan.Add(new MinimaxAIActionPlan { Unit = nextUnit, Action = action });
                GenerateActionPlans(unitActions, newPlan, allPlans, newRemainingUnits);
                
                // If we've generated enough plans, stop
                if (allPlans.Count >= 100)
                {
                    return;
                }
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
                
                ESUnit esUnit = unit as ESUnit;
                if (esUnit != null && esUnit.isStructure)
                    continue; // Don't count structures in unit valuation
                    
                float unitValue = unit.HitPoints * unitHealthWeight;
                    
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
                        
                        if (state.ObjectiveControlTurns.ContainsKey(PlayerNumber))
                        {
                            score += state.ObjectiveControlTurns[PlayerNumber] * objectiveControlWeight;
                        }
                    }
                    else if (structure.OwnerPlayerNumber == enemyPlayerNumber)
                    {
                        score -= objectiveControlWeight;
                        
                        if (state.ObjectiveControlTurns.ContainsKey(enemyPlayerNumber))
                        {
                            score -= state.ObjectiveControlTurns[enemyPlayerNumber] * objectiveControlWeight;
                        }
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
                    
                ESUnit esUnit = unit as ESUnit;
                if (esUnit != null && esUnit.isStructure)
                    continue;
                
                Vector2Int unitPos = new Vector2Int((int)unit.Cell.OffsetCoord.x, (int)unit.Cell.OffsetCoord.y);
                float distance = Vector2.Distance(unitPos, objectivePos);
                
                float positionValue = 10 / (distance + 1); // Closer is better
                
                if (unit.PlayerNumber == PlayerNumber)
                    allyPositionValue += positionValue;
                else if (unit.PlayerNumber == enemyPlayerNumber)
                    enemyPositionValue += positionValue;
            }
            
            score += (allyPositionValue - enemyPositionValue) * unitPositionWeight;
            
            // Calculate additional strategic factors using RL policy if available
            if (policyNetwork != null && isTraining)
            {
                // Use policy network to evaluate this state from an RL perspective
                float[] rlState = ConvertGameStateToRLState(state);
                float[] actionValues = policyNetwork.Predict(rlState);
                
                // The average action value can be an indicator of state quality
                float avgActionValue = actionValues.Average();
                score += avgActionValue * 5; // Weight for RL contribution
            }
            
            // Cache the evaluation
            stateEvaluationCache[stateHash] = score;
            
            return score;
        }
        
        private float[] ConvertGameStateToRLState(GameState state)
        {
            // A simplified version - in production you'd want to match your actual RL state representation
            List<float> rlState = new List<float>();
            
            // Add basic game state info
            rlState.Add(state.CurrentPlayerNumber);
            rlState.Add(state.CellGrid.Turns[state.CurrentPlayerNumber, 0]);
            
            // Add objective control information
            int objectiveControlTurnsForPlayer = 0;
            if (state.ObjectiveControlTurns.ContainsKey(PlayerNumber))
            {
                objectiveControlTurnsForPlayer = state.ObjectiveControlTurns[PlayerNumber];
            }
            rlState.Add(objectiveControlTurnsForPlayer);
            
            // Add resource information
            if (state.Resources.ContainsKey(PlayerNumber))
            {
                rlState.Add(state.Resources[PlayerNumber]);
            }
            else
            {
                rlState.Add(0);
            }
            
            if (state.Resources.ContainsKey(enemyPlayerNumber))
            {
                rlState.Add(state.Resources[enemyPlayerNumber]);
            }
            else
            {
                rlState.Add(0);
            }
            
            // Add simplified unit and structure info
            int allyUnitCount = 0;
            int enemyUnitCount = 0;
            int allyStructureCount = 0;
            int enemyStructureCount = 0;
            
            foreach (var unit in state.Units)
            {
                if (unit == null || !unit.isActiveAndEnabled)
                    continue;
                if (unit.PlayerNumber == PlayerNumber)
                {
                    ESUnit esUnit = unit as ESUnit;
                    if (esUnit != null && esUnit.isStructure)
                    {
                        allyStructureCount++;
                    }
                    else
                    {
                        allyUnitCount++;
                    }
                }
                else if (unit.PlayerNumber == enemyPlayerNumber)
                {
                    ESUnit esUnit = unit as ESUnit;
                    if (esUnit != null && esUnit.isStructure)
                    {
                        enemyStructureCount++;
                    }
                    else
                    {
                        enemyUnitCount++;
                    }
                }
            }
            
            rlState.Add(allyUnitCount);
            rlState.Add(enemyUnitCount);
            rlState.Add(allyStructureCount);
            rlState.Add(enemyStructureCount);
            
            return rlState.ToArray();
        }
        
        private float[] GetStateRepresentation(CellGrid cellGrid)
        {
            List<float> state = new List<float>();
            
            // Game turn information
            state.Add(cellGrid.CurrentPlayerNumber);
            state.Add(cellGrid.Turns[cellGrid.CurrentPlayerNumber, 0]);
            
            // Add objective control information
            state.Add(objectiveControlTurns);
            
            // Add player resource information
            EconomyController economy = UnityEngine.Object.FindObjectOfType<EconomyController>();
            if (economy != null)
            {
                state.Add(economy.GetValue(PlayerNumber));
                
                // Enemy resources (all other players)
                float enemyResourcesTotal = 0;
                int enemyCount = 0;
                foreach (var player in cellGrid.Players)
                {
                    if (player.PlayerNumber != PlayerNumber)
                    {
                        enemyResourcesTotal += economy.GetValue(player.PlayerNumber);
                        enemyCount++;
                    }
                }
                
                state.Add(enemyCount > 0 ? enemyResourcesTotal / enemyCount : 0);
            }
            else
            {
                // No economy system, use placeholders
                state.Add(0);
                state.Add(0);
            }
            
            // Encode each cell on the grid
            foreach (var cell in cellGrid.Cells)
            {
                // Position
                state.Add(cell.OffsetCoord.x);
                state.Add(cell.OffsetCoord.y);
                
                // Structure information
                state.Add(GetStructureType(cell));
                state.Add(GetStructureOwner(cell));
                
                // Unit information
                ESUnit unit = cell.CurrentUnits
                    .Where(u => u != null && u.gameObject != null && u.isActiveAndEnabled)
                    .Select(u => u.GetComponent<ESUnit>())
                    .OfType<ESUnit>()
                    .FirstOrDefault();
                    
                if (unit != null)
                {
                    // Basic unit info
                    state.Add(unit.PlayerNumber);
                    state.Add(GetUnitType(unit));
                    state.Add(unit.HitPoints);
                    state.Add(unit.ActionPoints);
                    state.Add(unit.AttackFactor);
                    state.Add(unit.DefenceFactor);
                    state.Add(unit.MovementPoints);
                }
                else
                {
                    // No unit placeholder values (7 basic values)
                    for (int i = 0; i < 7; i++)
                    {
                        state.Add(0);
                    }
                }
            }
            
            // Unit count and distribution information
            Dictionary<int, int> unitTypeCount = new Dictionary<int, int>();
            int allyUnitCount = 0;
            int enemyUnitCount = 0;
            
            foreach (var unit in cellGrid.Units)
            {
                if (unit == null || !unit.isActiveAndEnabled)
                    continue;
                var esUnit = unit.GetComponent<ESUnit>();
                if (esUnit != null && !esUnit.isStructure)
                {
                    int unitType = GetUnitType(esUnit);
                    
                    // Track unit type distribution for own units
                    if (unit.PlayerNumber == PlayerNumber)
                    {
                        allyUnitCount++;
                        if (!unitTypeCount.ContainsKey(unitType))
                            unitTypeCount[unitType] = 0;
                            
                        unitTypeCount[unitType]++;
                    }
                    else
                    {
                        enemyUnitCount++;
                    }
                }
            }
            
            // Add unit count info
            state.Add(allyUnitCount);
            state.Add(enemyUnitCount);
            
            // Add unit type distribution (up to 5 unit types)
            for (int typeId = 0; typeId < 5; typeId++)
            {
                state.Add(unitTypeCount.ContainsKey(typeId) ? unitTypeCount[typeId] : 0);
            }
            
            return state.ToArray();
        }
        
        private float CalculateActionReward(
            ESUnit unit, 
            MinimaxAIAction action, 
            CellGrid cellGrid, 
            Dictionary<Unit, float> preActionUnitHP,
            HashSet<ESUnit> preActionCapturedStructures,
            bool wasControllingObjective,
            Cell unitDestinationCell)
        {
            float reward = 0f;
            
            // Reward for attacking and damaging/killing enemy units
            if (action is MinimaxAttackAIAction attackAction)
            {
                Unit target = attackAction.Target;
                if (target != null && preActionUnitHP.ContainsKey(target))
                {
                    float damageDone = preActionUnitHP[target] - target.HitPoints;
                    
                    if (damageDone > 0)
                    {
                        // Reward for damage dealt
                        reward += damageDone * damageDealtRewardWeight;
                        
                        // Extra reward for killing a unit
                        if (target.HitPoints <= 0)
                        {
                            reward += unitKillRewardWeight;
                        }
                    }
                }
            }
            
            // Reward for capturing structures via movement
            if (action is MinimaxMoveToPositionAIAction moveAction && unitDestinationCell != null)
            {
                // Check if the unit captured a structure
                if (unitDestinationCell.CurrentUnits.Where(u => u != null && u.gameObject != null).Select(u => u.GetComponent<ESUnit>()).Any(u => u != null && u.isStructure && u.PlayerNumber != unit.PlayerNumber))
                {
                    var structureUnit = unitDestinationCell.CurrentUnits
                        .Where(u => u != null && u.gameObject != null)
                        .Select(u => u.GetComponent<ESUnit>())
                        .FirstOrDefault(u => u != null && u.isStructure);
                        
                    if (structureUnit != null && 
                        structureUnit.isStructure && 
                        structureUnit.PlayerNumber != unit.PlayerNumber &&
                        !preActionCapturedStructures.Contains(structureUnit))
                    {
                        // Add to captured structures
                        capturedStructures.Add(structureUnit);
                        structuresCapturedThisGame++;
                        
                        // Assign reward based on structure type
                        if (structureUnit.UnitName.Contains("Outpost"))
                        {
                            reward += outpostCaptureRewardWeight;
                        }
                        else if (structureUnit.UnitName.Contains("Base"))
                        {
                            reward += baseCaptureRewardWeight;
                        }
                        else if (structureUnit.UnitName.Contains("Objective"))
                        {
                            reward += objectiveCaptureRewardWeight;
                        }
                    }
                }
            }
            
            // Reward for unit spawning
            if (action is MinimaxSpawnUnitAction spawnAction)
            {
                GameObject selectedPrefab = spawnAction.SelectedPrefab;
                if (selectedPrefab != null)
                {
                    var esUnitPrefab = selectedPrefab.GetComponent<ESUnit>();
                    if (esUnitPrefab != null)
                    {
                        float unitValue = esUnitPrefab.AttackFactor + esUnitPrefab.DefenceFactor + 
                                         esUnitPrefab.HitPoints / 5.0f + esUnitPrefab.MovementPoints / 2.0f;
                        
                        if (esUnitPrefab.AttackRange > 1)
                        {
                            unitValue += esUnitPrefab.AttackRange;
                        }
                        
                        // Scale reward based on unit quality
                        reward += unitValue * unitProductionRewardWeight;
                    }
                }
            }
            
            // Reward for base upgrading
            if (action is MinimaxUpgradeBaseAction)
            {
                reward += baseUpgradeRewardWeight;
                
                // Track upgrade level
                if (!baseUpgradeLevels.ContainsKey(unit))
                {
                    baseUpgradeLevels[unit] = 0;
                }
                baseUpgradeLevels[unit]++;
            }
            
            // Reward for maintaining objective control
            if (!wasControllingObjective && IsControllingObjective(cellGrid))
            {
                reward += objectiveCaptureRewardWeight;
            }
            else if (IsControllingObjective(cellGrid))
            {
                reward += objectiveControlTurnRewardWeight;
            }
            
            // Reward/penalty for game outcome
            if (cellGrid.GameFinished)
            {
                reward += IsWinner(this, cellGrid) ? winRewardWeight : loseRewardWeight;
            }
            
            return reward;
        }
        
        private void LearnFromExperiences()
        {
            // Get batch of experiences
            var batch = replayBuffer.SampleBatch(batchSize);
            
            float totalLoss = 0f;
            float totalTDError = 0f;

            // Q-learning update
            foreach (var experience in batch)
            {
                float[] state = experience.state;
                int action = experience.action;
                float reward = experience.reward;
                float[] nextState = experience.nextState;
                bool done = experience.done;
                
                // Current Q-values
                float[] currentQ = policyNetwork.Predict(state);
                
                // Target Q-value
                float targetQ;
                if (done)
                {
                    targetQ = reward;
                }
                else
                {
                    float[] nextQ = policyNetwork.Predict(nextState);
                    targetQ = reward + discountFactor * nextQ.Max();
                }
                
                // Calculate TD error
                float tdError = Mathf.Abs(targetQ - currentQ[action]);
                
                // Update Q-value for taken action
                currentQ[action] = targetQ;
                
                // Train network
                var result = policyNetwork.Train(state, currentQ, learningRate);
                totalLoss += result.Loss;
                totalTDError += result.TDError;
                
                if (collectTrainingData)
                {
                    metrics.RecordTrainingStep(result.Loss, result.TDError, result.QValues);
                }
            }
            
            // Update metrics
            if (debugMode)
            {
                Debug.Log($"Training batch - Avg Loss: {totalLoss/batch.Count:F4}, Avg TD Error: {totalTDError/batch.Count:F4}");
            }
            
            // Decrease exploration rate over time
            explorationRate = Mathf.Max(0.05f, explorationRate * 0.999f);
        }
        
        private void OnGameEnded(object sender, System.EventArgs e)
        {
            StopAllCoroutines();
            
            // Calculate final game reward
            var cellGrid = sender as CellGrid;
            bool isWinner = IsWinner(this, cellGrid);
            float finalReward = isWinner ? winRewardWeight : loseRewardWeight;
            totalReward += finalReward;
            currentEpisodeReward += finalReward;
            
            if (debugMode)
            {
                Debug.Log($"Game ended: Player {PlayerNumber} " + 
                          $"{(isWinner ? "won" : "lost")}, " +
                          $"Final reward: {finalReward}, Total reward: {totalReward:F2}");
            }
            
            // Update metrics
            if (collectTrainingData)
            {
                // Record game outcome
                metrics.RecordGameResult(isWinner, cellGrid.Turns[PlayerNumber, 0]);
                Debug.Log($"Game {metrics.gamesPlayed} - Player {PlayerNumber} " + 
                          $"{(isWinner ? "won" : "lost")} in {cellGrid.Turns[PlayerNumber, 0]} turns" + ", Win count: " + metrics.winsCount);
                // Save metrics periodically
                if (metrics.gamesPlayed % saveMetricsInterval == 0)
                {
                    metrics.SaveMetricsToCSV(PlayerNumber.ToString());
                    
                    // Export experience samples
                    replayBuffer.ExportSampleToCSV($"experiences_game{metrics.gamesPlayed}.csv");
                    
                    // Export weight histograms if enabled
                    if (exportWeightHistograms && policyNetwork != null)
                    {
                        ExportWeightHistogram(metrics.gamesPlayed);
                    }
                }
            }
            
            // Save the trained model if in training mode
            if (isTraining)
            {
                policyNetwork.SaveModel(modelFilename);
                if (debugMode)
                {
                    Debug.Log($"Model saved to {modelFilename}");
                }
                // Save metrics
                string metricsJson = JsonConvert.SerializeObject(metrics, Formatting.Indented);
                File.WriteAllText(Path.Combine(Application.persistentDataPath, $"training_metrics_{PlayerNumber}.json"), metricsJson);
                
                if (debugMode)
                {
                    Debug.Log("Saved metrics to persistent storage");
                }
            }
            
            // Clear caches and reset for next game
            stateEvaluationCache.Clear();
            currentEpisodeReward = 0f;
            structuresCapturedThisGame = 0;
            if (isTraining)
            {    
                // Reload the scene
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
        
        private void ExportWeightHistogram(int gameNumber)
        {
            // Get weight statistics
            WeightStats stats = policyNetwork.GetWeightStats();
            
            // Create a histogram of weights
            int bucketCount = 20;
            float bucketWidth = (stats.maxWeight - stats.minWeight) / bucketCount;
            int[] histogram = new int[bucketCount];
            
            // Export the histogram data
            string basePath = Path.Combine(Application.persistentDataPath, "RL_Training_Data", metrics.sessionId);
            string filePath = Path.Combine(basePath, $"weights_game{gameNumber}.csv");
            Directory.CreateDirectory(basePath);
            
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine("Statistic,Value");
                writer.WriteLine($"MinWeight,{stats.minWeight}");
                writer.WriteLine($"MaxWeight,{stats.maxWeight}");
                writer.WriteLine($"MeanWeight,{stats.meanWeight}");
                writer.WriteLine($"StdDevWeight,{stats.stdDevWeight}");
                writer.WriteLine($"WeightCount,{stats.count}");
                
                // Export learning metrics
                writer.WriteLine();
                writer.WriteLine("LearningMetric,Value");
                writer.WriteLine($"ExplorationRate,{explorationRate}");
                writer.WriteLine($"AverageLoss,{policyNetwork.AverageLoss}");
                writer.WriteLine($"RecentAverageLoss,{policyNetwork.RecentAverageLoss}");
                
                // Add weight histogram
                writer.WriteLine();
                writer.WriteLine("BucketMin,BucketMax,Count");
                
                for (int i = 0; i < bucketCount; i++)
                {
                    float min = stats.minWeight + i * bucketWidth;
                    float max = min + bucketWidth;
                    writer.WriteLine($"{min},{max},{histogram[i]}");
                }
            }
        }
        
        #region Helper Methods
        
        private int CalculateStateSize(CellGrid cellGrid)
        {
            // Calculate size of state representation:
            // 2 for game info (current player, turn)
            // 1 for objective control turns
            // 2 for player resources
            // For each cell:
            //   2 for position
            //   2 for structure info (type & owner)
            //   7 for basic unit info
            // 7 for unit count and type distribution info
            
            int cellCount = cellGrid.Cells.Count;
            return 5 + (cellCount * (2 + 2 + 7)) + 7;
        }
        
        private int CalculateActionSize(CellGrid cellGrid)
        {
            // We need to account for all possible action types
            // This depends on your implementation, but here are common actions:
            // 0: No action / Skip
            // 1: Move to position
            // 2: Attack unit
            // 3: Capture structure
            // 4: Spawn unit
            // 5: Upgrade base
            return 6; // Adjust based on your actual action types
        }
        
        private int GetStructureType(Cell cell)
        {
            // Encode structure types as integers
            // 0: No structure
            // 1: Outpost
            // 2: Base
            // 3: Objective
            
            var units = cell.CurrentUnits;
            foreach (var unit in units)
            {
                if (unit == null || !unit.isActiveAndEnabled)
                    continue;
                var esUnit = unit.GetComponent<ESUnit>();
                if (esUnit != null && esUnit.isActiveAndEnabled && esUnit.isStructure)
                {
                    if (esUnit.UnitName.Contains("Outpost"))
                        return 1;
                    if (esUnit.UnitName.Contains("Base"))
                        return 2;
                    if (esUnit.UnitName.Contains("Objective"))
                        return 3;
                }
            }
            
            return 0;
        }
        
        private int GetStructureOwner(Cell cell)
        {
            // Return player number of structure owner, or 0 if no owner
            var units = cell.CurrentUnits;
            foreach (var unit in units)
            {
                if (unit == null || !unit.isActiveAndEnabled)
                    continue;
                if (unit is ESUnit esUnit && esUnit.isStructure)
                {
                    return unit.PlayerNumber;
                }
            }
            
            return 0;
        }
        
        private int GetUnitType(ESUnit unit)
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
        
        private bool IsControllingObjective(CellGrid cellGrid)
        {
            // Check if this player controls the main objective
            foreach (var cell in cellGrid.Cells)
            {
                if (cell.CurrentUnits.Count > 0)
                {
                    foreach (var unit in cell.CurrentUnits)
                    {
                        if (unit == null || !unit.isActiveAndEnabled)
                            continue;
                        ESUnit esUnit = unit.GetComponent<ESUnit>();
                        if (esUnit != null && esUnit.UnitName.Contains("Objective") && esUnit.PlayerNumber == PlayerNumber)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        
        private bool IsWinner(Player player, CellGrid cellGrid)
        {
            // Determine if this player is the winner
            // In your game, winning conditions are:
            // 1. Controlling the objective for 5 turns
            // 2. Capturing enemy base
            
            // Check objective control
            if (objectiveControlTurns >= 5)
                return true;
                
            // Check if enemy base is captured
            int baseAmount = 0;
            foreach (var cell in cellGrid.Cells)
            {
                // Find enemy base cells
                if (cell.CurrentUnits.Count > 0)
                {
                    foreach (var unit in cell.CurrentUnits)
                    {
                        if (unit == null || !unit.isActiveAndEnabled)
                            continue;
                        ESUnit esUnit = unit.GetComponent<ESUnit>();
                        if (esUnit != null && 
                            esUnit.UnitName.Contains("Base") && 
                            esUnit.PlayerNumber == player.PlayerNumber)
                        {
                            baseAmount++;
                        }
                    }
                }
            }
            if (baseAmount == 2)  // Player controls all bases (their own + enemy's)
                return true;
                
            return false;
        }
        
        #endregion
    }
    
}