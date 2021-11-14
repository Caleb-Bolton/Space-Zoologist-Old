﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AudioUI : MonoBehaviour
{
    [SerializeField] Slider vol;
    [SerializeField] Slider musicVol;
    [SerializeField] Slider sfxVol;
    // Start is called before the first frame update
    void Start()
    {
        AudioManager audioManager = AudioManager.instance;
        vol.value = audioManager.MasterVolume;
        musicVol.value = audioManager.MusicVolume;
        sfxVol.value = audioManager.SfxVolume;
    }
}