using System;
using System.Collections.Generic;
using System.IO;
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
    
    public void Train(float[] input, float[] target, float learningRate)
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
        
        // Backward pass
        // Calculate output layer errors
        float[] outputErrors = new float[outputSize];
        for (int i = 0; i < outputSize; i++)
        {
            outputErrors[i] = target[i] - output[i];
        }
        
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
    }
    
    public void SaveModel(string filename)
    {
        string path = Path.Combine("./", filename);
        
        ModelData data = new ModelData
        {
            inputSize = this.inputSize,
            outputSize = this.outputSize,
            hiddenSize = this.hiddenSize,
            bias1 = this.bias1,
            bias2 = this.bias2,
            weights1Flat = new float[hiddenSize][],
            weights2Flat = new float[outputSize][]
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
        
        Debug.Log("Model saved to " + path);
    }
    
    public void LoadModel(string filename)
    {
        string path = Path.Combine("./", filename);
        
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            ModelData data = JsonUtility.FromJson<ModelData>(json);
            
            this.inputSize = data.inputSize;
            this.outputSize = data.outputSize;
            this.hiddenSize = data.hiddenSize;
            this.bias1 = data.bias1;
            this.bias2 = data.bias2;
            
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
            
            Debug.Log("Model loaded from " + path);
        }
        else
        {
            throw new System.Exception($"Model file not found at {path}");
        }
    }
}