using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class ReplayBuffer
{
    private int capacity;
    private List<Experience> buffer;
    private System.Random random;
    
    // Stats tracking
    private int totalExperiencesAdded = 0;
    private float totalReward = 0;
    private Dictionary<int, int> actionCounts = new Dictionary<int, int>();
    
    public ReplayBuffer(int capacity)
    {
        this.capacity = capacity;
        buffer = new List<Experience>(capacity);
        random = new System.Random();
        
        // Initialize action counts
        for (int i = 0; i < 7; i++) // Assuming 7 action types
        {
            actionCounts[i] = 0;
        }
    }
    
    public void AddExperience(float[] state, int action, float reward, float[] nextState, bool done)
    {
        Experience experience = new Experience(state, action, reward, nextState, done);
        
        // Update stats
        totalExperiencesAdded++;
        totalReward += reward;
        
        if (!actionCounts.ContainsKey(action))
        {
            actionCounts[action] = 0;
        }
        actionCounts[action]++;
        
        // If buffer is full, remove oldest element
        if (buffer.Count >= capacity)
        {
            buffer.RemoveAt(0);
        }
        
        buffer.Add(experience);
    }
    
    public List<Experience> SampleBatch(int batchSize)
    {
        batchSize = Mathf.Min(batchSize, buffer.Count);
        List<Experience> batch = new List<Experience>(batchSize);
        
        // Random sampling with replacement
        for (int i = 0; i < batchSize; i++)
        {
            int index = random.Next(0, buffer.Count);
            batch.Add(buffer[index]);
        }
        
        return batch;
    }
    
    public int Size()
    {
        return buffer.Count;
    }
    
    public void Clear()
    {
        buffer.Clear();
        totalExperiencesAdded = 0;
        totalReward = 0;
        
        foreach (var key in actionCounts.Keys.ToList())
        {
            actionCounts[key] = 0;
        }
    }
    
    // Get action distribution
    public Dictionary<int, int> GetActionDistribution()
    {
        return new Dictionary<int, int>(actionCounts);
    }
    
    // Get reward statistics
    public ReplayStats GetStats()
    {
        ReplayStats stats = new ReplayStats();
        stats.bufferSize = buffer.Count;
        stats.totalExperiencesAdded = totalExperiencesAdded;
        stats.actionDistribution = new Dictionary<int, int>(actionCounts);
        
        if (buffer.Count > 0)
        {
            float sum = 0;
            float min = float.MaxValue;
            float max = float.MinValue;
            
            foreach (var exp in buffer)
            {
                sum += exp.reward;
                min = Mathf.Min(min, exp.reward);
                max = Mathf.Max(max, exp.reward);
            }
            
            stats.averageReward = sum / buffer.Count;
            stats.minReward = min;
            stats.maxReward = max;
        }
        
        return stats;
    }
    
    // Export a sample of experiences to CSV for analysis
    public void ExportSampleToCSV(string filename, int sampleSize = 100)
    {
        string path = Path.Combine(Application.persistentDataPath, "RL_Training_Data", filename);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        
        sampleSize = Mathf.Min(sampleSize, buffer.Count);
        List<Experience> sample = SampleBatch(sampleSize);
        
        using (StreamWriter writer = new StreamWriter(path))
        {
            writer.WriteLine("Action,Reward,Done,StateSize,NextStateSize");
            
            foreach (var exp in sample)
            {
                writer.WriteLine($"{exp.action},{exp.reward},{exp.done},{exp.state.Length},{exp.nextState.Length}");
            }
        }
        
        Debug.Log($"Experience sample exported to {path}");
    }
}

public struct Experience
{
    public float[] state;
    public int action;
    public float reward;
    public float[] nextState;
    public bool done;
    
    public Experience(float[] state, int action, float reward, float[] nextState, bool done)
    {
        this.state = state;
        this.action = action;
        this.reward = reward;
        this.nextState = nextState;
        this.done = done;
    }
}

public class ReplayStats
{
    public int bufferSize;
    public int totalExperiencesAdded;
    public float averageReward;
    public float minReward;
    public float maxReward;
    public Dictionary<int, int> actionDistribution;
}