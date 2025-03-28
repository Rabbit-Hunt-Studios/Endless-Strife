using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SideBar : MonoBehaviour
{
    public Slider bgmSlider;
    public Slider sfxSlider;

    public void start()
    {
        if (PlayerPrefs.HasKey("BGMVolume")) {
            bgmSlider.value = PlayerPrefs.GetInt("BGMVolume");
        }
        if (PlayerPrefs.HasKey("SFXVolume")) {
            bgmSlider.value = PlayerPrefs.GetInt("SFXVolume");
        }
    }

    public void Settings()
    {
        SceneManager.LoadScene("Settings");
    }

    public void ChangeVolume(Slider slider)
    {
        PlayerPrefs.SetInt(slider.name, (int) slider.value);
        PlayerPrefs.SetInt("changeVolume", 1);
    }

    public void Guide()
    {
        Debug.Log("show guide");
    }

    public void Back()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
