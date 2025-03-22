using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class SaveAbility : MonoBehaviour
{
    public int player;
    public PlayerData playerData { get; private set; }
    public bool inGame;
    private string path;

    [System.Serializable]
    public class PlayerData
    {
        public int totalMoney = 0;
        public int totalMergeCount = 0;
        public float totalPlayTime = 0.0f;
        public int totalUnitsProduced = 0;
        public int totalWins = 0;
        public List<int> turnsPerSession = new List<int>();
        public Dictionary<string, int> mergeCombinations = new Dictionary<string, int>();
    }

    // Start is called before the first frame update
    void Start()
    {
        path = $"./PlayerData/player{player}_data.json";
        if (!System.IO.Directory.Exists("./PlayerData"))
        {
            System.IO.Directory.CreateDirectory("./PlayerData");
        }
        if (!System.IO.File.Exists(path))
        {
            bool tmp = inGame;
            inGame = true;
            PlayerData newPlayerData = new PlayerData();
            SaveData(newPlayerData);
            inGame = tmp;
        }
        playerData = LoadData();
    }

    void OnApplicationQuit()
    {
        SaveData(playerData);
    }

    void OnDestroy()
    {
        SaveData(playerData);
    }

    public void UpdateValues(PlayerData playerValues)
    {
        playerData.totalMoney += playerValues.totalMoney;
        playerData.totalMergeCount += playerValues.totalMergeCount;
        playerData.totalPlayTime += playerValues.totalPlayTime;
        playerData.totalUnitsProduced += playerValues.totalUnitsProduced;
        playerData.totalWins += playerValues.totalWins;
        if (playerValues.turnsPerSession.Count > 0)
        {
            playerData.turnsPerSession.Add(playerValues.turnsPerSession[0]);
        }
        foreach (KeyValuePair<string, int> entry in playerValues.mergeCombinations)
        {
            if (playerData.mergeCombinations.ContainsKey(entry.Key))
            {
                playerData.mergeCombinations[entry.Key] = playerData.mergeCombinations[entry.Key] + 1;
            }
            else
            {
                playerData.mergeCombinations[entry.Key] = 1;
            }
            Debug.Log($"merge count: {playerData.mergeCombinations[entry.Key]}");
        }
    }

    private void SaveData(PlayerData playerToSave)
    {
        if (inGame)
        {
            playerToSave.totalPlayTime = playerToSave.totalPlayTime + Time.time;
            string json = JsonConvert.SerializeObject(playerToSave, Formatting.Indented);
            System.IO.File.WriteAllText(path, json);
        }
    }

    private PlayerData LoadData()
    {
        string json = System.IO.File.ReadAllText(path);

        PlayerData loadedPlayer = JsonConvert.DeserializeObject<PlayerData>(json);

        // Now you can access the loaded player data
        float totalPlayTime = loadedPlayer.totalPlayTime;

        // Use the loaded data in your game
        Debug.Log($"JSON Player playtime: {totalPlayTime} ");
        return loadedPlayer;
    }
}
