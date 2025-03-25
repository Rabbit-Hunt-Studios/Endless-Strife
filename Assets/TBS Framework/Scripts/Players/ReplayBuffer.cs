using System;
using System.Collections.Generic;
using UnityEngine;

public class ReplayBuffer
{
    private int capacity;
    private List<Experience> buffer;
    private System.Random random;
    
    public ReplayBuffer(int capacity)
    {
        this.capacity = capacity;
        buffer = new List<Experience>(capacity);
        random = new System.Random();
    }
    
    public void AddExperience(float[] state, int action, float reward, float[] nextState, bool done)
    {
        Experience experience = new Experience(state, action, reward, nextState, done);
        
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