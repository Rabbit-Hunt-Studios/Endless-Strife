using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TbsFramework.Cells;
using TbsFramework.Grid;
using TbsFramework.Grid.GridStates;
using TbsFramework.Players.AI;
using TbsFramework.Players.AI.Actions;
using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.Players
{
    public class RLPlayer : Player
    {
        // RL model parameters
        private NeuralNetwork policyNetwork;
        private ReplayBuffer replayBuffer;
        
        // State representation
        private float[] currentState;
        
        // Learning parameters
        public float learningRate = 0.001f;
        public float discountFactor = 0.99f;
        public float explorationRate = 0.1f;
        public int batchSize = 32;
        public int replayBufferCapacity = 10000;
        public int hiddenLayerSize = 256;
        
        // Training mode
        public bool isTraining = true;
        public bool loadModelOnStart = false;
        public string modelFilename = "rl_model.json";
        
        // Reward weights
        public float damageDealtRewardWeight = 0.5f;
        public float unitKillRewardWeight = 3.0f;
        public float outpostCaptureRewardWeight = 5.0f;
        public float objectiveCaptureRewardWeight = 10.0f;
        public float baseCaptureRewardWeight = 50.0f;
        public float objectiveControlTurnRewardWeight = 2.0f;
        public float winRewardWeight = 100.0f;
        public float loseRewardWeight = -100.0f;
        
        // Base action reward weights
        public float baseUpgradeRewardWeight = 15.0f;
        public float unitProductionRewardWeight = 2.0f;
        
        // Debug
        public bool debugMode = false;
        
        // Game state tracking
        private Dictionary<Unit, float> unitHP;
        private HashSet<ESUnit> capturedStructures;
        private int objectiveControlTurns = 0;
        public float totalReward = 0f;
        private Dictionary<ESUnit, int> baseUpgradeLevels = new Dictionary<ESUnit, int>();
        private Dictionary<ESUnit, List<ESUnit>> spawnedUnits = new Dictionary<ESUnit, List<ESUnit>>();
        private int currentTurn = 0;
        
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
        }

        private void OnLevelLoadingDone(object sender, EventArgs e)
        {
            // Calculate state and action sizes
            var cellGrid = sender as CellGrid;
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
                    Debug.LogWarning($"Could not load model: {ex.Message}. Using new model.");
                }
            }
            
            replayBuffer = new ReplayBuffer(replayBufferCapacity);
            
            // Track initial state
            foreach (var unit in cellGrid.Units)
            {
                ESUnit esUnit = unit as ESUnit;
                unitHP[unit] = unit.HitPoints;
                
                // Initialize tracking for base units
                if (esUnit != null && esUnit.PlayerNumber == PlayerNumber && esUnit.isStructure)
                {
                    baseUpgradeLevels[esUnit] = 0;
                    spawnedUnits[esUnit] = new List<ESUnit>();
                    
                    // Add base AI actions if missing
                    SetupBaseRLActions(esUnit);
                }
            }
            
            if (debugMode)
            {
                Debug.Log("RL Player initialization completed.");
            }
        }

        private void SetupBaseRLActions(ESUnit baseUnit)
        {
            // Add spawn action
            if (baseUnit.GetComponent<SpawnAbility>() != null && 
                baseUnit.GetComponent<SpawnRLAction>() == null)
            {
                baseUnit.gameObject.AddComponent<SpawnRLAction>();
            }
            
            // Add upgrade action
            if (baseUnit.GetComponent<UpgradeAbility>() != null && 
                baseUnit.GetComponent<UpgradeRLAction>() == null)
            {
                baseUnit.gameObject.AddComponent<UpgradeRLAction>();
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
        }
        
        public override void Play(CellGrid cellGrid)
        {
            cellGrid.cellGridState = new CellGridStateRLTurn(cellGrid, this);
            StartCoroutine(PlayRLCoroutine(cellGrid));
        }
        
        private IEnumerator PlayRLCoroutine(CellGrid cellGrid)
        {
            // Capture current state
            currentState = GetStateRepresentation(cellGrid);
            
            var unitsOrdered = GetComponent<UnitSelection>().SelectNext(() => cellGrid.GetCurrentPlayerUnits(), cellGrid);
            float episodeReward = 0f;
            
            foreach (var unit in unitsOrdered)
            {
                ESUnit esUnit = unit.GetComponent<ESUnit>();
                unit.MarkAsSelected();
                
                // Get all possible actions for this unit
                var possibleActions = GetPossibleActions(unit, cellGrid);
                
                // Select action using policy (with exploration if training)
                (RLAction selectedAction, int actionIndex) = SelectAction(unit, possibleActions, cellGrid);
                
                float actionReward = 0f;
                
                if (selectedAction != null)
                {
                    // Execute the selected action
                    selectedAction.InitializeAction(this, unit, cellGrid);
                    selectedAction.Precalculate(this, unit, cellGrid);
                    
                    // Record unit state before action
                    Dictionary<Unit, float> preActionUnitHP = new Dictionary<Unit, float>();
                    foreach (var u in cellGrid.Units)
                    {
                        preActionUnitHP[u] = u.HitPoints;
                    }
                    
                    // Record structure state before action
                    HashSet<ESUnit> preActionCapturedStructures = new HashSet<ESUnit>(capturedStructures);
                    bool wasControllingObjective = IsControllingObjective(cellGrid);
                    Cell unitDestinationCell = null;
                    
                    // Special handling for move actions to track destination
                    if (selectedAction is MoveToPositionRLAction moveAction)
                    {
                        moveAction.Precalculate(this, unit, cellGrid);
                        unitDestinationCell = moveAction.TopDestination;
                    }
                    
                    // Execute the action
                    yield return StartCoroutine(selectedAction.Execute(this, unit, cellGrid));
                    
                    // Calculate rewards based on action outcome
                    actionReward = CalculateActionReward(
                        esUnit, 
                        selectedAction, 
                        cellGrid, 
                        preActionUnitHP, 
                        preActionCapturedStructures,
                        wasControllingObjective,
                        unitDestinationCell
                    );
                    
                    episodeReward += actionReward;
                    totalReward += actionReward;
                    
                    selectedAction.CleanUp(this, unit, cellGrid);
                    
                    // If training, record experience and learn
                    if (isTraining)
                    {
                        // Get new state and reward
                        float[] newState = GetStateRepresentation(cellGrid);
                        
                        // Store experience in replay buffer
                        replayBuffer.AddExperience(currentState, actionIndex, actionReward, newState, cellGrid.GameFinished);
                        
                        // Learn from batch of experiences
                        if (replayBuffer.Size() > batchSize)
                        {
                            LearnFromExperiences();
                        }
                        
                        // Update current state
                        currentState = newState;
                    }
                    
                    if (debugMode)
                    {
                        Debug.Log($"Action: {selectedAction.GetType().Name}, Reward: {actionReward:F2}");
                    }
                }
                
                unit.MarkAsFriendly();
                yield return new WaitForSeconds(0.2f);
            }
            
            if (debugMode)
            {
                Debug.Log($"Player {PlayerNumber} - Turn complete, Episode reward: {episodeReward:F2}, Total reward: {totalReward:F2}");
            }
            
            cellGrid.EndTurn();
            yield return null;
        }
        
        private List<(RLAction action, int index)> GetPossibleActions(Unit unit, CellGrid cellGrid)
        {
            List<(RLAction action, int index)> possibleActions = new List<(RLAction action, int index)>();
            var actions = unit.GetComponentsInChildren<RLAction>();

            foreach (var action in actions)
            {
                if (debugMode)
                {
                    Debug.Log($"Player {PlayerNumber} - Unit {unit.name} checking action: {action.GetType().Name}");
                }
                action.InitializeAction(this, unit, cellGrid);
                if (action.ShouldExecute(this, unit, cellGrid))
                {
                    possibleActions.Add((action, action.GetActionIndex()));
                    if (debugMode) 
                    {
                        Debug.Log($"Player {PlayerNumber} - Unit {unit.name} can perform action: {action.GetType().Name} ({action.GetActionIndex()})");
                    }
                }
            }
            if (debugMode)
            {
                Debug.Log($"Player {PlayerNumber} - Unit {unit.name} has {possibleActions.Count} possible actions");
            }
            return possibleActions;
        }
        
        private (RLAction, int) SelectAction(Unit unit, List<(RLAction action, int index)> possibleActions, CellGrid cellGrid)
        {
            if (possibleActions.Count == 0)
                return (null, -1);
                
            // Exploration: random action
            if (isTraining && UnityEngine.Random.value < explorationRate)
            {
                var randomChoice = possibleActions[UnityEngine.Random.Range(0, possibleActions.Count)];
                return (randomChoice.action, randomChoice.index);
            }
            
            // Exploitation: use policy network
            float[] actionValues = policyNetwork.Predict(currentState);
            
            // Find best valid action
            float bestValue = float.MinValue;
            RLAction bestAction = null;
            int bestActionIndex = -1;
            
            foreach (var (action, index) in possibleActions)
            {
                if (actionValues[index] > bestValue)
                {
                    bestValue = actionValues[index];
                    bestAction = action;
                    bestActionIndex = index;
                }
            }
            
            return (bestAction, bestActionIndex);
        }
        
        private float[] GetStateRepresentation(CellGrid cellGrid)
        {
            List<float> state = new List<float>();
            
            // Game turn information
            state.Add(cellGrid.CurrentPlayerNumber);
            currentTurn++;
            state.Add(currentTurn);
            
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
                    // No unit placeholder values (12 values: 7 basic + 5 for merged)
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
                var esUnit = unit.GetComponent<ESUnit>();
                if (!esUnit.isStructure)
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
            RLAction action, 
            CellGrid cellGrid, 
            Dictionary<Unit, float> preActionUnitHP,
            HashSet<ESUnit> preActionCapturedStructures,
            bool wasControllingObjective,
            Cell unitDestinationCell)
        {
            float reward = 0f;
            
            // Reward for attacking and damaging/killing enemy units
            if (action is AttackRLAction)
            {
                reward += CalculateAttackReward(unit, cellGrid, preActionUnitHP);
            }
            
            // Reward for capturing structures
            if (action is MoveToPositionRLAction && unitDestinationCell != null)
            {
                reward += CalculateStructureCaptureReward(unit, unitDestinationCell, preActionCapturedStructures);
            }
            
            // Reward for unit spawning
            if (action is SpawnRLAction spawnAction)
            {
                GameObject selectedPrefab = spawnAction.SelectedPrefab;
                reward += CalculateSpawnReward(selectedPrefab);
                
                // Track spawned unit
                if (selectedPrefab != null && !spawnedUnits.ContainsKey(unit))
                {
                    spawnedUnits[unit] = new List<ESUnit>();
                }
            }
            
            // Reward for base upgrading
            if (action is UpgradeRLAction)
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
        
        private float CalculateAttackReward(Unit attackingUnit, CellGrid cellGrid, Dictionary<Unit, float> preActionUnitHP)
        {
            float reward = 0f;
            
            // Check all enemy units to see if they took damage
            foreach (var unit in cellGrid.GetEnemyUnits(this))
            {
                if (preActionUnitHP.ContainsKey(unit))
                {
                    float damageDone = preActionUnitHP[unit] - unit.HitPoints;
                    
                    if (damageDone > 0)
                    {
                        // Reward for damage dealt
                        reward += damageDone * damageDealtRewardWeight;
                        
                        // Extra reward for killing a unit
                        if (unit.HitPoints <= 0)
                        {
                            reward += unitKillRewardWeight;
                        }
                    }
                }
            }
            
            return reward;
        }
        
        private float CalculateStructureCaptureReward(Unit unit, Cell destination, HashSet<ESUnit> preActionCapturedStructures)
        {
            float reward = 0f;
            
            // Check if the unit captured a structure
            if (destination.CurrentUnits.Select(u => u.GetComponent<ESUnit>()).Any(u => u != null && u.isStructure && u.PlayerNumber != unit.PlayerNumber))
            {
                var structureUnit = destination.CurrentUnits.FirstOrDefault() as ESUnit;
                if (structureUnit != null && 
                    structureUnit.isStructure && 
                    structureUnit.PlayerNumber != unit.PlayerNumber &&
                    !preActionCapturedStructures.Contains(structureUnit))
                {
                    // Add to captured structures
                    capturedStructures.Add(structureUnit);
                    
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
            
            return reward;
        }
        
        private float CalculateSpawnReward(GameObject unitPrefab)
        {
            if (unitPrefab == null)
                return 0;
                
            var esUnit = unitPrefab.GetComponent<ESUnit>();
            if (esUnit == null)
                return 0;
                
            float unitValue = esUnit.AttackFactor + esUnit.DefenceFactor + esUnit.HitPoints / 5.0f + 
                            esUnit.MovementPoints / 2.0f;
            
            if (esUnit.AttackRange > 1)
            {
                unitValue += esUnit.AttackRange;
            }
                            
            // Scale reward based on unit quality
            return unitValue * unitProductionRewardWeight;
        }
        
        private void LearnFromExperiences()
        {
            // Get batch of experiences
            var batch = replayBuffer.SampleBatch(batchSize);
            
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
                
                // Update Q-value for taken action
                currentQ[action] = targetQ;
                
                // Train network
                policyNetwork.Train(state, currentQ, learningRate);
            }
            
            // Decrease exploration rate over time
            explorationRate = Mathf.Max(0.05f, explorationRate * 0.999f);
        }
        
        private void OnGameEnded(object sender, System.EventArgs e)
        {
            StopAllCoroutines();
            
            // Calculate final game reward
            var cellGrid = sender as CellGrid;
            float finalReward = IsWinner(this, cellGrid) ? winRewardWeight : loseRewardWeight;
            totalReward += finalReward;
            
            if (debugMode)
            {
                Debug.Log($"Game ended: Player {PlayerNumber} " + 
                          $"{(IsWinner(this, cellGrid) ? "won" : "lost")}, " +
                          $"Final reward: {finalReward}, Total reward: {totalReward:F2}");
            }
            
            // Save the trained model if in training mode
            if (isTraining)
            {
                policyNetwork.SaveModel(modelFilename);
                if (debugMode)
                {
                    Debug.Log($"Model saved to {modelFilename}");
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
        
        private float GetPlayerResources(Player player)
        {
            var economy = UnityEngine.Object.FindObjectOfType<EconomyController>();
            if (economy != null)
            {
                return economy.GetValue(player.PlayerNumber);
            }
            return 0;
        }
        
        private Player GetOpponent(CellGrid cellGrid)
        {
            // Find opponent player
            foreach (var player in cellGrid.Players)
            {
                if (player.PlayerNumber != this.PlayerNumber)
                {
                    return player;
                }
            }
            return null;
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
                var esUnit = unit.GetComponent<ESUnit>();
                if (esUnit.isStructure)
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
                        ESUnit esUnit = unit.GetComponent<ESUnit>();
                        if (esUnit.UnitName.Contains("Objective") && esUnit.PlayerNumber == PlayerNumber)
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
                        ESUnit esUnit = unit.GetComponent<ESUnit>();
                        if (esUnit.UnitName.Contains("Base") && 
                            esUnit.PlayerNumber == player.PlayerNumber)
                        {
                            baseAmount++;
                            
                        }
                    }
                }
            }
            if (baseAmount == 2)
                return true;
            return false;
        }
        
        #endregion
    }
}
