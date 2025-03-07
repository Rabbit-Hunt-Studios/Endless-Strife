using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveAbility : MonoBehaviour
{
    public int player;

    [System.Serializable]
    public class PlayerData
    {
        public float totalMoney = 0.0f;
        public int totalMergeCount = 0;
        public float totalPlayTime = 0.0f;
        public float totalUnitsProduced = 0;
    }
    public PlayerData playerData;

    // Start is called before the first frame update
    void Start()
    {
        if (!System.IO.File.Exists("player" + player.ToString() + "Data.json"))
        {
            PlayerData newPlayerData = new PlayerData();
            save_data(newPlayerData);
        }
        playerData = load_data();
    }

    void OnApplicationQuit()
    {
        save_data(playerData);
    }

    private void save_data(PlayerData player)
    {
        string json = JsonUtility.ToJson(player);
        System.IO.File.WriteAllText("player" + player.ToString() + "Data.json", json);
    }

    private PlayerData load_data()
    {
        string json = System.IO.File.ReadAllText("player" + player.ToString() + "Data.json");

        PlayerData loadedPlayer = JsonUtility.FromJson<PlayerData>(json);

        // Now you can access the loaded player data
        float totalMoney = loadedPlayer.totalMoney;

        // Use the loaded data in your game
        Debug.Log($"JSON Player Money: {totalMoney} ");
        return loadedPlayer;
    }
}
