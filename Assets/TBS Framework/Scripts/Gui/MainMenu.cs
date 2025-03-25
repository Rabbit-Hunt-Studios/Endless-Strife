using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        // SceneManager.LoadScene("MainMap" + Random.Range(0,3).ToString());
        SceneManager.LoadScene("MainMapTest");
    }

    public void PlayGameAI()
    {
        Debug.Log("load scene with AI player");
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
