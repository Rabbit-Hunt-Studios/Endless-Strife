using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SideBar : MonoBehaviour
{
    public Slider bgmSlider;
    public Slider sfxSlider;

    public void Start()
    {
        if (bgmSlider != null)
        {
            if (PlayerPrefs.HasKey("BGMVolume")) {
            bgmSlider.value = (float) PlayerPrefs.GetInt("BGMVolume");
            }
            if (PlayerPrefs.HasKey("SFXVolume")) {
                sfxSlider.value = (float) PlayerPrefs.GetInt("SFXVolume");
            }
        }
    }

    public void Settings()
    {
        SceneManager.LoadScene("Settings");
    }

    public void ChangeVolume(Slider slider)
    {
        PlayerPrefs.SetInt(slider.name, (int) slider.value);
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
