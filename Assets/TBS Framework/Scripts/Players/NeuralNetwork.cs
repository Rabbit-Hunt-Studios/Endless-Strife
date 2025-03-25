using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class NeuralNetwork
{
    private int inputSize;
    private int outputSize;
    private int hiddenSize;
    
    private float[,] weights1; // Input to hidden
    private float[,] weights2; // Hidden to output
    private float[] bias1;
    private float[] bias2;
    
    // Metrics tracking
    private float totalLoss = 0f;
    private int trainingSteps = 0;
    private List<float> recentLosses = new List<float>(100); // Track last 100 losses
    
    // Public property for input size
    public int InputSize => inputSize;
    
    // Public access to training metrics
    public float AverageLoss => trainingSteps > 0 ? totalLoss / trainingSteps : 0f;
    public float RecentAverageLoss => recentLosses.Count > 0 ? recentLosses.Average() : 0f;
    
    [Serializable]
    private class ModelData
    {
        public int inputSize;
        public int outputSize;
        public int hiddenSize;
        public float[][] weights1Flat;
        public float[][] weights2Flat;
        public float[] bias1;
        public float[] bias2;
        public int trainingSteps;
    }
    
    public NeuralNetwork(int inputSize, int outputSize, int hiddenSize = 128)
    {
        this.inputSize = inputSize;
        this.outputSize = outputSize;
        this.hiddenSize = hiddenSize;
        
        // Initialize weights with small random values
        weights1 = new float[hiddenSize, inputSize];
        weights2 = new float[outputSize, hiddenSize];
        bias1 = new float[hiddenSize];
        bias2 = new float[outputSize];
        
        InitializeWeights();
    }
    
    private void InitializeWeights()
    {
        System.Random random = new System.Random();
        
        // Xavier initialization for better training
        float w1Scale = Mathf.Sqrt(6.0f / (inputSize + hiddenSize));
        float w2Scale = Mathf.Sqrt(6.0f / (hiddenSize + outputSize));
        
        for (int i = 0; i < hiddenSize; i++)
        {
            for (int j = 0; j < inputSize; j++)
            {
                weights1[i, j] = (float)((random.NextDouble() * 2 - 1) * w1Scale);
            }
            bias1[i] = 0;
        }
        
        for (int i = 0; i < outputSize; i++)
        {
            for (int j = 0; j < hiddenSize; j++)
            {
                weights2[i, j] = (float)((random.NextDouble() * 2 - 1) * w2Scale);
            }
            bias2[i] = 0;
        }
    }
    
    public float[] Predict(float[] input)
    {
        if (input.Length != inputSize)
        {
            Debug.LogError($"Input size mismatch. Expected {inputSize}, got {input.Length}");
            return new float[outputSize];
        }
        
        // Forward pass
        float[] hidden = new float[hiddenSize];
        float[] output = new float[outputSize];
        
        // Input to hidden
        for (int i = 0; i < hiddenSize; i++)
        {
            hidden[i] = bias1[i];
            for (int j = 0; j < inputSize; j++)
            {
                hidden[i] += input[j] * weights1[i, j];
            }
            // ReLU activation
            hidden[i] = Mathf.Max(0, hidden[i]);
        }
        
        // Hidden to output
        for (int i = 0; i < outputSize; i++)
        {
            output[i] = bias2[i];
            for (int j = 0; j < hiddenSize; j++)
            {
                output[i] += hidden[j] * weights2[i, j];
            }
        }
        
        return output;
    }
    
    public TrainingResult Train(float[] input, float[] target, float learningRate)
    {
        // Forward pass
        float[] hidden = new float[hiddenSize];
        float[] output = new float[outputSize];
        float[] hiddenRaw = new float[hiddenSize]; // Pre-activation for derivative
        
        // Input to hidden
        for (int i = 0; i < hiddenSize; i++)
        {
            hiddenRaw[i] = bias1[i];
            for (int j = 0; j < inputSize; j++)
            {
                hiddenRaw[i] += input[j] * weights1[i, j];
            }
            // ReLU activation
            hidden[i] = Mathf.Max(0, hiddenRaw[i]);
        }
        
        // Hidden to output
        for (int i = 0; i < outputSize; i++)
        {
            output[i] = bias2[i];
            for (int j = 0; j < hiddenSize; j++)
            {
                output[i] += hidden[j] * weights2[i, j];
            }
        }
        
        // Calculate loss
        float loss = 0;
        float maxTDError = 0;
        
        // Backward pass
        // Calculate output layer errors
        float[] outputErrors = new float[outputSize];
        for (int i = 0; i < outputSize; i++)
        {
            outputErrors[i] = target[i] - output[i];
            float error = Math.Abs(outputErrors[i]);
            loss += error * error; // MSE loss
            maxTDError = Mathf.Max(maxTDError, error);
        }
        loss /= outputSize; // Average loss
        
        // Calculate hidden layer errors
        float[] hiddenErrors = new float[hiddenSize];
        for (int i = 0; i < hiddenSize; i++)
        {
            hiddenErrors[i] = 0;
            for (int j = 0; j < outputSize; j++)
            {
                hiddenErrors[i] += outputErrors[j] * weights2[j, i];
            }
            // ReLU derivative
            if (hiddenRaw[i] <= 0)
                hiddenErrors[i] = 0;
        }
        
        // Update weights and biases
        // Hidden to output
        for (int i = 0; i < outputSize; i++)
        {
            bias2[i] += learningRate * outputErrors[i];
            for (int j = 0; j < hiddenSize; j++)
            {
                weights2[i, j] += learningRate * outputErrors[i] * hidden[j];
            }
        }
        
        // Input to hidden
        for (int i = 0; i < hiddenSize; i++)
        {
            bias1[i] += learningRate * hiddenErrors[i];
            for (int j = 0; j < inputSize; j++)
            {
                weights1[i, j] += learningRate * hiddenErrors[i] * input[j];
            }
        }
        
        // Update metrics
        trainingSteps++;
        totalLoss += loss;
        
        // Track recent losses
        if (recentLosses.Count >= 100)
            recentLosses.RemoveAt(0);
        recentLosses.Add(loss);
        
        return new TrainingResult
        {
            Loss = loss,
            TDError = maxTDError,
            QValues = output
        };
    }
    
    public void SaveModel(string filename)
    {
        string path = Path.Combine("./RL/Model", filename);
        
        ModelData data = new ModelData
        {
            inputSize = this.inputSize,
            outputSize = this.outputSize,
            hiddenSize = this.hiddenSize,
            bias1 = this.bias1,
            bias2 = this.bias2,
            weights1Flat = new float[hiddenSize][],
            weights2Flat = new float[outputSize][],
            trainingSteps = this.trainingSteps
        };
        
        // Flatten 2D arrays for serialization
        for (int i = 0; i < hiddenSize; i++)
        {
            data.weights1Flat[i] = new float[inputSize];
            for (int j = 0; j < inputSize; j++)
            {
                data.weights1Flat[i][j] = weights1[i, j];
            }
        }
        
        for (int i = 0; i < outputSize; i++)
        {
            data.weights2Flat[i] = new float[hiddenSize];
            for (int j = 0; j < hiddenSize; j++)
            {
                data.weights2Flat[i][j] = weights2[i, j];
            }
        }
        
        string json = JsonUtility.ToJson(data);
        File.WriteAllText(path, json);
        
        // Also save a versioned backup
        string backupPath = Path.Combine("./RL/Model", $"{Path.GetFileNameWithoutExtension(filename)}_v{trainingSteps}.json");
        File.WriteAllText(backupPath, json);
        
        Debug.Log("Model saved to " + path);
    }
    
    public void LoadModel(string filename)
    {
        string path = Path.Combine("./RL/Model", filename);
        
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            ModelData data = JsonUtility.FromJson<ModelData>(json);
            
            this.inputSize = data.inputSize;
            this.outputSize = data.outputSize;
            this.hiddenSize = data.hiddenSize;
            this.bias1 = data.bias1;
            this.bias2 = data.bias2;
            this.trainingSteps = data.trainingSteps;
            
            this.weights1 = new float[hiddenSize, inputSize];
            this.weights2 = new float[outputSize, hiddenSize];
            
            // Convert flat arrays back to 2D
            for (int i = 0; i < hiddenSize; i++)
            {
                for (int j = 0; j < inputSize; j++)
                {
                    weights1[i, j] = data.weights1Flat[i][j];
                }
            }
            
            for (int i = 0; i < outputSize; i++)
            {
                for (int j = 0; j < hiddenSize; j++)
                {
                    weights2[i, j] = data.weights2Flat[i][j];
                }
            }
            
            Debug.Log($"Model loaded from {path}, with {trainingSteps} prior training steps");
        }
        else
        {
            throw new System.Exception($"Model file not found at {path}");
        }
    }
    
    // Get weight statistics for visualization
    public WeightStats GetWeightStats()
    {
        WeightStats stats = new WeightStats();
        
        // Process first layer weights
        for (int i = 0; i < hiddenSize; i++)
        {
            for (int j = 0; j < inputSize; j++)
            {
                float w = weights1[i, j];
                stats.minWeight = Mathf.Min(stats.minWeight, w);
                stats.maxWeight = Mathf.Max(stats.maxWeight, w);
                stats.sumWeight += w;
                stats.sumSquaredWeight += w * w;
                stats.count++;
            }
        }
        
        // Process second layer weights
        for (int i = 0; i < outputSize; i++)
        {
            for (int j = 0; j < hiddenSize; j++)
            {
                float w = weights2[i, j];
                stats.minWeight = Mathf.Min(stats.minWeight, w);
                stats.maxWeight = Mathf.Max(stats.maxWeight, w);
                stats.sumWeight += w;
                stats.sumSquaredWeight += w * w;
                stats.count++;
            }
        }
        
        // Calculate mean and standard deviation
        stats.meanWeight = stats.count > 0 ? stats.sumWeight / stats.count : 0;
        float variance = stats.count > 0 ? (stats.sumSquaredWeight / stats.count) - (stats.meanWeight * stats.meanWeight) : 0;
        stats.stdDevWeight = Mathf.Sqrt(variance);
        
        return stats;
    }
}

public class TrainingResult
{
    public float Loss;
    public float TDError;
    public float[] QValues;
}

public class WeightStats
{
    public float minWeight = float.MaxValue;
    public float maxWeight = float.MinValue;
    public float sumWeight = 0;
    public float sumSquaredWeight = 0;
    public int count = 0;
    public float meanWeight = 0;
    public float stdDevWeight = 0;
}