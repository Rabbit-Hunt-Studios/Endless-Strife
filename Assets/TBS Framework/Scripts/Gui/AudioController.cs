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
        MusicSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        SFXSource.PlayOneShot(clip);
    }
}
