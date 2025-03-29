using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TbsFramework.Players;

public class SelectDifficulty : MonoBehaviour
{
    public void PlayGame(int difficulty)
    {
        PlayerPrefs.SetInt("MinimaxDepth", difficulty);
        SceneManager.LoadScene("MainMap" + Random.Range(0,7).ToString() + " AI");
    }
    public void BackButton()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
