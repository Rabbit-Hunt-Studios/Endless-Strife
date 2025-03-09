using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveAbility : MonoBehaviour
{
    public int player;
    private PlayerData playerData;
    private string path;

    [System.Serializable]
    public class PlayerData
    {
        public int totalMoney = 0;
        public int totalMergeCount = 0;
        public float totalPlayTime = 0.0f;
        public int totalUnitsProduced = 0;
        public int totalWins = 0;
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
            PlayerData newPlayerData = new PlayerData();
            SaveData(newPlayerData);
        }
        playerData = LoadData();
    }

    void OnApplicationQuit()
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
    }

    private void SaveData(PlayerData playerToSave)
    {
        playerToSave.totalPlayTime = playerToSave.totalPlayTime + Time.time;
        string json = JsonUtility.ToJson(playerToSave);
        System.IO.File.WriteAllText(path, json);
    }

    private PlayerData LoadData()
    {
        string json = System.IO.File.ReadAllText(path);

        PlayerData loadedPlayer = JsonUtility.FromJson<PlayerData>(json);

        // Now you can access the loaded player data
        float totalPlayTime = loadedPlayer.totalPlayTime;

        // Use the loaded data in your game
        Debug.Log($"JSON Player playtime: {totalPlayTime} ");
        return loadedPlayer;
    }
}
