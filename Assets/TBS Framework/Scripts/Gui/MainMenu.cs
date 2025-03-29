using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadScene("MainMap" + Random.Range(0,7).ToString());
    }

    public void PlayGameAI()
    {
        // Debug.Log("load scene with AI player");
        SceneManager.LoadScene("SelectDifficulty");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void StatsPage()
    {
        SceneManager.LoadScene("PlayerStats");
    }
}
