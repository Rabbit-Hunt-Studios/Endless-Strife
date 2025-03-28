using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioController : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource MusicSource;
    public AudioSource SFXSource;

    [Header("Audio Clips")]
    public AudioClip BGMusic;
    public AudioClip ButtonClick;
    public AudioClip ButtonHover;
    public AudioClip UnitMove;
    public AudioClip UnitSlashed;
    public AudioClip UnitBow;
    public AudioClip UnitShoot;
    public AudioClip UnitMagic;

    private void Start()
    {
        MusicSource.clip = BGMusic;
        MusicSource.loop = true;

        if (PlayerPrefs.HasKey("BGMVolume")) {
            // Debug.Log(PlayerPrefs.GetInt("BGMVolume"));
            // Debug.Log((float) PlayerPrefs.GetInt("BGMVolume") / 100);
            MusicSource.volume = (float) PlayerPrefs.GetInt("BGMVolume") / 100;
        }
        if (PlayerPrefs.HasKey("SFXVolume")) {
            SFXSource.volume = (float) PlayerPrefs.GetInt("SFXVolume") / 100;
        }

        MusicSource.Play();
    }

    public void Update()
    {
        if (PlayerPrefs.HasKey("BGMVolume") && PlayerPrefs.GetInt("changeVolume") == 1) {
            MusicSource.volume = (float) PlayerPrefs.GetInt("BGMVolume") / 100;
            PlayerPrefs.SetInt("changeVolume", 0);
        }
        if (PlayerPrefs.HasKey("SFXVolume") && PlayerPrefs.GetInt("changeVolume") == 1) {
            SFXSource.volume = (float) PlayerPrefs.GetInt("SFXVolume") / 100;
            PlayerPrefs.SetInt("changeVolume", 0);
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        SFXSource.PlayOneShot(clip);
    }
}
