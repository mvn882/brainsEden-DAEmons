﻿using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public AudioMixer Mixer;

    public enum Sound
    {
        HeadExplosion,
        MenuInteraction
    }

    public AudioClip MenuBackgroundMusicClip;
    public AudioClip GameBackgroundMusicClip;
    private AudioSource _backgroundMusicSource;

    public AudioClip MenuInteraction;
    private AudioSource _menuInteractionSource;

    public AudioClip HeadExplosion;
    private AudioSource _headExplosionSource;

    private bool _musicMuted = false;
    private float _lastMusicVolumeLinear;
    //private bool _sfxMuted = false;
    //private float _lastSFXVolumeLinear;

    public float MasterVolume
    {
        get
        {
            float vol; Mixer.GetFloat("Master Volume", out vol);
            return vol;
        }
        set
        {
            Mixer.SetFloat("Master Volume", LinearToDecibel(value));
        }
    }
    public float SFXVolume
    {
        get
        {
            float vol; Mixer.GetFloat("SFX Volume", out vol);
            return vol;
        }
        set
        {
            Mixer.SetFloat("SFX Volume", LinearToDecibel(value));
        }
    }

    private float _musicVolume;
    public float GetMusicVolumeLinear()
    {
        float vol; Mixer.GetFloat("Music Volume", out vol);
        return DecibelToLinear(vol);
    }

    public float GetMusicVolumeDecibel()
    {
        float vol; Mixer.GetFloat("Music Volume", out vol);
        return vol;
    }

    public void SetMusicVolumeLinear(float linearValue)
    {
        Mixer.SetFloat("Music Volume", LinearToDecibel(linearValue));
    }

    public void SetMusicVolumeDecibel(float decibelVolume)
    {
        Mixer.SetFloat("Music Volume", decibelVolume);
    }

    private float LinearToDecibel(float linear)
    {
        float dB;

        if (linear != 0.0f)
        {
            dB = 20.0f * Mathf.Log10(linear);
        }
        else
        {
            dB = -144.0f;
        }

        return dB;
    }

    private float DecibelToLinear(float decibel)
    {
        float dB;

        dB = Mathf.Pow(decibel, decibel / 20.0f);

        return dB;
    }

    void Awake()
    {
        DontDestroyOnLoad(transform.gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoad;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoad;
    }

    private void Start()
    {
        CreateAndInitializeSource(ref _backgroundMusicSource, null);
        SetSourceMixerGroupMixerGroup("Master/Music", _backgroundMusicSource);
        _backgroundMusicSource.loop = true;
        // TODO: Use once we have a menu
        //if (GameManager.CurrentGameScene == GameManager.GameScene.MainMenu)
        //{
        //    _backgroundMusicSource.clip = MenuBackgroundMusicClip;
        //}
        //else if (GameManager.CurrentGameScene == GameManager.GameScene.Game)
        //{
            _backgroundMusicSource.clip = GameBackgroundMusicClip;
        //}

        CreateAndInitializeSource(ref _menuInteractionSource, MenuInteraction);
        SetSourceMixerGroupMixerGroup("Master/SFX/Menu Interaction", _menuInteractionSource);

        CreateAndInitializeSource(ref _headExplosionSource, HeadExplosion);
        SetSourceMixerGroupMixerGroup("Master/SFX/Head Explosion", _headExplosionSource);

        _backgroundMusicSource.Play();
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.M))
        {
            _musicMuted = !_musicMuted;

            if (_musicMuted)
            {
                _lastMusicVolumeLinear = GetMusicVolumeLinear();
                SetMusicVolumeLinear(0.0f);
            }
            else
            {
                SetMusicVolumeLinear(_lastMusicVolumeLinear);
            }
        }
    }

    private void CreateAndInitializeSource(ref AudioSource source, AudioClip clip)
    {
        source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = false;
        source.playOnAwake = false;
    }

    private AudioMixerGroup SetSourceMixerGroupMixerGroup(string subPath, AudioSource source)
    {
        AudioMixerGroup group = Mixer.FindMatchingGroups(subPath)[0];
        source.outputAudioMixerGroup = group;
        return group;
    }

    private void OnSceneLoad(Scene scene, LoadSceneMode mode)
    {
        
    }

    private void OnGamePause(bool paused)
    {
        if (paused)
        {
            
        }
        else
        {
           
        }
    }

    public void PlaySound(Sound sound, bool restart = true)
    {
        switch (sound)
        {
            case Sound.MenuInteraction:
                if (restart || !_menuInteractionSource.isPlaying) _menuInteractionSource.Play();
                break;
            case Sound.HeadExplosion:
                if (restart || !_headExplosionSource.isPlaying) _headExplosionSource.Play();
                break;
            default:
                break;
        }
    }

    public void StopSound(Sound sound)
    {
        switch (sound)
        {
            case Sound.MenuInteraction:
                _menuInteractionSource.Stop();
                break;
            case Sound.HeadExplosion:
                _headExplosionSource.Stop();
                break;
            default:
                break;
        }
    }
}