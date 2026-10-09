using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Mixer")]
    public AudioMixer audioMixer;

    [Header("Music Fade")]
    public float musicFadeSpeed = 1.5f;

    private float targetMusicVolume = 1f;
    private float currentMusicVolume = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadVolumeSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        currentMusicVolume = Mathf.MoveTowards(
            currentMusicVolume,
            targetMusicVolume,
            musicFadeSpeed * Time.unscaledDeltaTime
        );

        ApplyMusicMixerVolume(currentMusicVolume);
    }

    public void SetMasterVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);

        if (audioMixer != null)
        {
            audioMixer.SetFloat(
                "MasterVolume",
                Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20f
            );
        }

        PlayerPrefs.SetFloat(
            "MasterVolume",
            volume
        );
    }

    public void SetMusicVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);

        targetMusicVolume = volume;

        PlayerPrefs.SetFloat(
            "MusicVolume",
            volume
        );
    }

    private void ApplyMusicMixerVolume(float volume)
    {
        if (audioMixer != null)
        {
            audioMixer.SetFloat(
                "MusicVolume",
                Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20f
            );
        }
    }

    public void SetSFXVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);

        if (audioMixer != null)
        {
            audioMixer.SetFloat(
                "SFXVolume",
                Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20f
            );
        }

        PlayerPrefs.SetFloat(
            "SFXVolume",
            volume
        );
    }

    public void LoadVolumeSettings()
    {
        float master =
            PlayerPrefs.GetFloat(
                "MasterVolume",
                1f
            );

        float music =
            PlayerPrefs.GetFloat(
                "MusicVolume",
                1f
            );

        float sfx =
            PlayerPrefs.GetFloat(
                "SFXVolume",
                1f
            );

        currentMusicVolume = music;
        targetMusicVolume = music;

        SetMasterVolume(master);

        ApplyMusicMixerVolume(
            music
        );

        SetSFXVolume(sfx);

        PlayerPrefs.Save();
    }
}