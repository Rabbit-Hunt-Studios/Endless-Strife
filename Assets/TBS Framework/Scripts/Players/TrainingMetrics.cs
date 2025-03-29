using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace TbsFramework.Players
{
    [Serializable]
    public class TrainingMetrics
    {
        // Metric tracking
        public List<float> episodeRewards = new List<float>();
        public List<float> cumulativeRewards = new List<float>();
        public List<float> losses = new List<float>();
        public List<float> explorationRates = new List<float>();
        public List<int> actionCounts = new List<int>();
        public List<float> averageQValues = new List<float>();
        public List<float> tdErrors = new List<float>();
        public List<int> gameLength = new List<int>();
        public List<int> winCount = new List<int>();
        
        // Per action type metrics
        public Dictionary<int, int> actionTypeDistribution = new Dictionary<int, int>();
        public Dictionary<int, float> actionTypeRewards = new Dictionary<int, float>();
        
        // Strategy metrics
        public List<float> objectiveControlTime = new List<float>();
        public List<int> unitCountDifference = new List<int>();
        public List<float> resourceDifference = new List<float>();
        public List<int> structuresCaptured = new List<int>();
        
        // Session info
        public DateTime sessionStartTime;
        public int gamesPlayed = 0;
        public int totalTurns = 0;
        public int winsCount = 0;
        public float winRate => gamesPlayed > 0 ? (float)winsCount / gamesPlayed : 0f;
        public string sessionId;
        
        // Constructor
        public TrainingMetrics()
        {
            sessionStartTime = DateTime.Now;
            sessionId = $"session_{sessionStartTime.ToString("yyyyMMdd_HHmmss")}";
            
            // Initialize action type dictionaries
            for (int i = 0; i < 6; i++) // Assuming 7 action types
            {
                actionTypeDistribution[i] = 0;
                actionTypeRewards[i] = 0f;
            }
        }
        
        // Record a completed game
        public void RecordGameResult(bool win, int turnCount)
        {
            gamesPlayed++;
            totalTurns += turnCount;
            gameLength.Add(turnCount);
            
            if (win)
            {
                winsCount++;
                winCount.Add(1);
            }
            else
            {
                winCount.Add(0);
            }
        }
        
        // Record a training step
        public void RecordTrainingStep(float loss, float tdError, float[] qValues)
        {
            losses.Add(loss);
            tdErrors.Add(tdError);
            averageQValues.Add(qValues.Average());
        }
        
        // Record an action
        public void RecordAction(int actionType, float reward)
        {
            if (!actionTypeDistribution.ContainsKey(actionType))
            {
                actionTypeDistribution[actionType] = 0;
                actionTypeRewards[actionType] = 0f;
            }
            
            actionTypeDistribution[actionType]++;
            actionTypeRewards[actionType] += reward;
        }
        
        // Record episode metrics
        public void RecordEpisodeMetrics(float episodeReward, float cumulativeReward, float explorationRate)
        {
            episodeRewards.Add(episodeReward);
            cumulativeRewards.Add(cumulativeReward);
            explorationRates.Add(explorationRate);
        }
        
        // Record strategic metrics
        public void RecordStrategicMetrics(float objectiveControl, int unitDiff, float resourceDiff, int capturedStructures)
        {
            objectiveControlTime.Add(objectiveControl);
            unitCountDifference.Add(unitDiff);
            resourceDifference.Add(resourceDiff);
            structuresCaptured.Add(capturedStructures);
        }
        
        // Save metrics to CSV
        public void SaveMetricsToCSV(string playerNumber)
        {
            string basePath = Path.Combine(".\\RL\\Data", $"RL_Training_Data_{playerNumber}");
            Directory.CreateDirectory(basePath);
            
            // Save overall metrics
            AppendListToCSV(Path.Combine(basePath, "episode_metrics.csv"), 
                new List<string> { "SessionID", "Episode", "Reward", "CumulativeReward", "ExplorationRate" },
                (i) => new List<string> { 
                    sessionId,
                    i.ToString(), 
                    i < episodeRewards.Count ? episodeRewards[i].ToString() : "",
                    i < cumulativeRewards.Count ? cumulativeRewards[i].ToString() : "",
                    i < explorationRates.Count ? explorationRates[i].ToString() : ""
                },
                Math.Max(episodeRewards.Count, Math.Max(cumulativeRewards.Count, explorationRates.Count))
            );
            
            // Save training metrics
            AppendListToCSV(Path.Combine(basePath, "training_metrics.csv"), 
                new List<string> { "SessionID", "Step", "Loss", "TDError", "AvgQValue" },
                (i) => new List<string> { 
                    sessionId,
                    i.ToString(), 
                    i < losses.Count ? losses[i].ToString() : "",
                    i < tdErrors.Count ? tdErrors[i].ToString() : "",
                    i < averageQValues.Count ? averageQValues[i].ToString() : ""
                },
                Math.Max(losses.Count, Math.Max(tdErrors.Count, averageQValues.Count))
            );
            
            // Save game results
            AppendListToCSV(Path.Combine(basePath, "game_results.csv"), 
                new List<string> { "SessionID", "Game", "Win", "Turns" },
                (i) => new List<string> { 
                    sessionId,
                    i.ToString(), 
                    i < winCount.Count ? winCount[i].ToString() : "",
                    i < gameLength.Count ? gameLength[i].ToString() : ""
                },
                Math.Max(winCount.Count, gameLength.Count)
            );
            
            // Save action type distribution
            AppendDictionaryToCSV(Path.Combine(basePath, "action_distribution.csv"),
                actionTypeDistribution, actionTypeRewards);
            
            // Save strategic metrics
            AppendListToCSV(Path.Combine(basePath, "strategic_metrics.csv"), 
                new List<string> { "SessionID", "Episode", "ObjectiveControl", "UnitCountDiff", "ResourceDiff", "StructuresCaptured" },
                (i) => new List<string> { 
                    sessionId,
                    i.ToString(), 
                    i < objectiveControlTime.Count ? objectiveControlTime[i].ToString() : "",
                    i < unitCountDifference.Count ? unitCountDifference[i].ToString() : "",
                    i < resourceDifference.Count ? resourceDifference[i].ToString() : "",
                    i < structuresCaptured.Count ? structuresCaptured[i].ToString() : ""
                },
                Math.Max(objectiveControlTime.Count, 
                    Math.Max(unitCountDifference.Count, 
                        Math.Max(resourceDifference.Count, structuresCaptured.Count)))
            );
            
            // Save session summary
            string summaryPath = Path.Combine(basePath, "session_summary.txt");
            using (StreamWriter writer = new StreamWriter(summaryPath, true)) // Append to the file
            {
                writer.WriteLine($"Session ID: {sessionId}");
                writer.WriteLine($"Start Time: {sessionStartTime}");
                writer.WriteLine($"End Time: {DateTime.Now}");
                writer.WriteLine($"Games Played: {gamesPlayed}");
                writer.WriteLine($"Wins: {winsCount} ({winRate * 100:F1}%)");
                writer.WriteLine($"Total Turns: {totalTurns}");
                writer.WriteLine($"Average Game Length: {(gamesPlayed > 0 ? totalTurns / (float)gamesPlayed : 0):F1} turns");
                
                if (episodeRewards.Count > 0)
                {
                    writer.WriteLine($"Average Episode Reward: {episodeRewards.Average():F2}");
                    writer.WriteLine($"Final Exploration Rate: {explorationRates.Last():F4}");
                }
                
                writer.WriteLine("\nAction Distribution:");
                foreach (var kvp in actionTypeDistribution)
                {
                    string actionName = GetActionName(kvp.Key);
                    float avgReward = kvp.Value > 0 ? actionTypeRewards[kvp.Key] / kvp.Value : 0;
                    writer.WriteLine($"  {actionName}: {kvp.Value} times, Avg Reward: {avgReward:F2}");
                }
                writer.WriteLine("--------------------------------------------------");
            }
            
            Debug.Log($"Training metrics saved to {basePath}");
        }
        
        // Helper to map action index to name
        private string GetActionName(int actionIndex)
        {
            switch (actionIndex)
            {
                case 0: return "Skip/NoAction";
                case 1: return "Move";
                case 2: return "Attack";
                case 3: return "Capture";
                case 4: return "SpawnUnit";
                case 5: return "UpgradeBase";
                default: return $"Action{actionIndex}";
            }
        }
        
        private void AppendListToCSV(string path, List<string> headers, Func<int, List<string>> rowGenerator, int count)
        {
            bool fileExists = File.Exists(path);
            using (StreamWriter writer = new StreamWriter(path, true)) // Append to the file
            {
                // Write headers if file does not exist
                if (!fileExists)
                {
                    writer.WriteLine(string.Join(",", headers));
                }
                
                // Write data rows
                for (int i = 0; i < count; i++)
                {
                    writer.WriteLine(string.Join(",", rowGenerator(i)));
                }
            }
        }
        
        private void AppendDictionaryToCSV(string path, Dictionary<int, int> counts, Dictionary<int, float> rewards)
        {
            bool fileExists = File.Exists(path);
            using (StreamWriter writer = new StreamWriter(path, true)) // Append to the file
            {
                if (!fileExists)
                {
                    writer.WriteLine("SessionID,ActionType,ActionName,Count,TotalReward,AverageReward");
                }
                
                foreach (var kvp in counts)
                {
                    int actionType = kvp.Key;
                    int count = kvp.Value;
                    float totalReward = rewards[actionType];
                    float avgReward = count > 0 ? totalReward / count : 0;
                    
                    writer.WriteLine($"{sessionId},{actionType},{GetActionName(actionType)},{count},{totalReward:F2},{avgReward:F2}");
                }
            }
        }
    }
}