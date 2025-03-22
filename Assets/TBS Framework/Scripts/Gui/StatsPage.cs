using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TbsFramework.Units;

public class StatsPage : MonoBehaviour
{
    public GameObject MoneyValue;
    public GameObject MergeValue;
    public GameObject PlaytimeValue;
    public GameObject UnitsValue;
    public GameObject WinsValue;
    public SaveAbility playerStats;
    private bool isDone = false;

    void Update()
    {
        if (playerStats.playerData != null && !isDone)
        {
            MoneyValue.GetComponent<Text>().text = playerStats.playerData.totalMoney.ToString();
            MergeValue.GetComponent<Text>().text = playerStats.playerData.totalMergeCount.ToString();
            PlaytimeValue.GetComponent<Text>().text = string.Format("{0:0.00}", (playerStats.playerData.totalPlayTime / 3600));
            UnitsValue.GetComponent<Text>().text = playerStats.playerData.totalUnitsProduced.ToString();
            WinsValue.GetComponent<Text>().text = playerStats.playerData.totalWins.ToString();
            isDone = true;
        }
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
