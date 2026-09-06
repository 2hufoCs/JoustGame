using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.LowLevel;

public enum SoundType { RUN, SKID, FLAP, BOUNCE, GAINSCORE, PLAYERDEATH, ENEMYDEATH, NEXTWAVE, GAMEOVER }

[RequireComponent(typeof(AudioSource)), ExecuteInEditMode]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] AudioSource mainAudioSource;
    [SerializeField] AudioSource musicAudioSource;

    [Header("Sound List")]
    [SerializeField] SoundList[] soundList;
    [SerializeField] SoundList musicList;

    [SerializeField] List<AudioSource> loopAudioSources = new();
    List<float> loopInitialVolumes = new();

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(Instance);
        Instance = this;
        if (Application.isPlaying) DontDestroyOnLoad(this);
    }

    /// <summary>
    /// Play oneshot sound
    /// </summary>
    /// <param name="soundType"></param>
    /// <param name="volume"></param>
    public static void PlaySound(SoundType soundType, float volume = 1)
    {
        AudioClip[] clips = Instance.soundList[(int)soundType].Sounds;
        AudioClip randomClip = clips[UnityEngine.Random.Range(0, clips.Length)];

        //volume *= SettingsDataManager.SettingsData.SFXVolume * SettingsDataManager.SettingsData.masterVolume;
        Instance.mainAudioSource.PlayOneShot(randomClip, volume * .6f);
    }

    /// <summary>
    /// Play looping sound
    /// </summary>
    /// <param name="soundType"></param>
    /// <param name="volume"></param>
    public static void PlayLoop(SoundType soundType, float volume = 1)
    {
        foreach (AudioSource loop in Instance.loopAudioSources)
        {
            if (Array.IndexOf(Instance.soundList[(int)soundType].Sounds, loop.clip) > -1) return;
        }

        Instance.loopInitialVolumes.Add(volume);

        AudioSource newAudioSource = Instance.gameObject.AddComponent<AudioSource>();
        //volume *= SettingsDataManager.SettingsData.SFXVolume * SettingsDataManager.SettingsData.masterVolume;
        newAudioSource.volume = volume;
        newAudioSource.loop = true;

        AudioClip[] clips = Instance.soundList[(int)soundType].Sounds;
        AudioClip randomClip = clips[UnityEngine.Random.Range(0, clips.Length)];
        newAudioSource.clip = randomClip;
        newAudioSource.Play();

        Instance.loopAudioSources.Add(newAudioSource);
    }

    /// <summary>
    /// Stop looping sound
    /// </summary>
    /// <param name="soundType"></param>
    public static void StopLoop(SoundType soundType)
    {
        foreach (AudioSource audioSource in Instance.loopAudioSources)
        {
            if (Array.IndexOf(Instance.soundList[(int)soundType].Sounds, audioSource.clip) > -1)
            {
                Instance.loopInitialVolumes.Remove(Instance.loopAudioSources.IndexOf(audioSource));
                Instance.loopAudioSources.Remove(audioSource);
                Destroy(audioSource);
                return;
            }
        }
        //Debug.Log($"tried to stop audio loop of type {soundType}, but none was found");
    }

    float initialMusicVolume;

    public static void PlayMusic(AudioClip music, float volume = 1)
    {
        Instance.initialMusicVolume = volume;
        //volume *= SettingsDataManager.SettingsData.musicVolume * SettingsDataManager.SettingsData.masterVolume;
        
        Instance.musicAudioSource.clip = music;
        Instance.musicAudioSource.volume = volume;

        Instance.musicAudioSource.Play();
    }

    public static void SetNewVolume(float masterVolume, float musicVolume, float SFXVolume)
    {
        Instance.musicAudioSource.volume = Instance.initialMusicVolume * musicVolume * masterVolume;

        for (int i = 0; i < Instance.loopAudioSources.Count; i++)
        {
            Instance.loopAudioSources[i].volume = Instance.loopInitialVolumes[i] * SFXVolume * masterVolume;
        }
    }

    /*
    /// <summary>
    /// Change volume of air fall
    /// </summary>
    /// <param name="volume"></param>
    public static void ChangeAirVolume(float volume)
    {
        if (!Instance.airSpeedAudioSource) return; 
        //volume *= SettingsDataManager.SettingsData.SFXVolume * SettingsDataManager.SettingsData.masterVolume;
        Instance.airSpeedAudioSource.volume = volume;
    }
    */


#if UNITY_EDITOR
    private void OnEnable()
    {
        string[] names = Enum.GetNames(typeof(SoundType));
        Array.Resize(ref soundList, names.Length);
        for (int i = 0; i < soundList.Length; i++)
            soundList[i].name = names[i];
    }
#endif
}

[Serializable]
public struct SoundList
{
    public readonly AudioClip[] Sounds { get; }
    [HideInInspector] public string name;
    [SerializeField] private AudioClip[] sounds;
}