using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsUI : MonoBehaviour
{
    [Header("Audio Sliders")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("Mouse Sensitivity")]
    public Slider mouseSensitivitySlider;

    [Header("Settings Panel")]
    public GameObject settingsPanel;

    [Header("Main Menu")]
    public GameObject mainMenuPanel;

    [Header("Menu Type")]
    public bool openedFromPauseMenu = false;

    private float savedMaster;
    private float savedMusic;
    private float savedSFX;
    private float savedMouseSensitivity;

    private const float DefaultMouseSensitivity = 2f;

    private void Start()
    {
        LoadSavedSettings();

        // Live preview while dragging (still saved only on Apply)
        if (masterSlider != null)
            masterSlider.onValueChanged.AddListener(PreviewMaster);

        if (musicSlider != null)
            musicSlider.onValueChanged.AddListener(PreviewMusic);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(PreviewSFX);

        if (mouseSensitivitySlider != null)
            mouseSensitivitySlider.onValueChanged.AddListener(ChangeMouseSensitivity);
    }

    private void OnDisable()
    {
        PlayerPrefs.Save();
    }

    private void ChangeMouseSensitivity(float value)
    {
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();

        if (player != null)
            player.SetMouseSensitivity(value);
        else
            PlayerPrefs.SetFloat("MouseSensitivity", value);
    }

    private void OnDestroy()
    {
        if (masterSlider != null)
            masterSlider.onValueChanged.RemoveListener(PreviewMaster);

        if (musicSlider != null)
            musicSlider.onValueChanged.RemoveListener(PreviewMusic);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(PreviewSFX);

        if (mouseSensitivitySlider != null)
            mouseSensitivitySlider.onValueChanged.RemoveListener(ChangeMouseSensitivity);
    }

    private void PreviewMaster(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMasterVolume(value);
    }

    private void PreviewMusic(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(value);
    }

    private void PreviewSFX(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(value);
    }

    private void RevertAudioToSaved()
    {
        if (AudioManager.Instance == null) return;

        AudioManager.Instance.SetMasterVolume(savedMaster);
        AudioManager.Instance.SetMusicVolume(savedMusic);
        AudioManager.Instance.SetSFXVolume(savedSFX);
    }

    // ==========================================
    // OPEN SETTINGS FROM MAIN MENU
    // ==========================================

    public void OpenSettingsFromMainMenu()
    {
        openedFromPauseMenu = false;

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }

        LoadSavedSettings();
    }

    // ==========================================
    // OPEN SETTINGS FROM PAUSE MENU
    // ==========================================

    public void OpenSettingsFromPauseMenu()
    {
        openedFromPauseMenu = true;

        if (PauseMenu.Instance != null)
        {
            PauseMenu.Instance.OpenSettings();
        }
        else
        {
            Debug.LogError("PauseMenu.Instance is NULL!");
        }

        LoadSavedSettings();
    }

    // ==========================================
    // LOAD SAVED SETTINGS
    // ==========================================

    private void LoadSavedSettings()
    {
        savedMaster =
            PlayerPrefs.GetFloat(
                "MasterVolume",
                1f
            );

        savedMusic =
            PlayerPrefs.GetFloat(
                "MusicVolume",
                1f
            );

        savedSFX =
            PlayerPrefs.GetFloat(
                "SFXVolume",
                1f
            );

        savedMouseSensitivity =
            PlayerPrefs.GetFloat(
                "MouseSensitivity",
                DefaultMouseSensitivity
            );

        if (masterSlider != null)
        {
            masterSlider.SetValueWithoutNotify(
                savedMaster
            );
        }

        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(
                savedMusic
            );
        }

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(
                savedSFX
            );
        }

        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.SetValueWithoutNotify(
                savedMouseSensitivity
            );
        }
    }

    // ==========================================
    // APPLY
    // ==========================================

    public void ApplySettings()
    {
        float master =
            masterSlider != null
            ? masterSlider.value
            : savedMaster;

        float music =
            musicSlider != null
            ? musicSlider.value
            : savedMusic;

        float sfx =
            sfxSlider != null
            ? sfxSlider.value
            : savedSFX;

        float mouseSensitivity =
            mouseSensitivitySlider != null
            ? mouseSensitivitySlider.value
            : savedMouseSensitivity;

        // Save the new values as the applied values
        savedMaster = master;
        savedMusic = music;
        savedSFX = sfx;
        savedMouseSensitivity = mouseSensitivity;

        // Apply audio
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(master);
            AudioManager.Instance.SetMusicVolume(music);
            AudioManager.Instance.SetSFXVolume(sfx);
        }
        else
        {
            Debug.LogError(
                "AudioManager.Instance is NULL!"
            );
        }

        // Apply mouse sensitivity
        PlayerMovement player =
            FindFirstObjectByType<PlayerMovement>();

        if (player != null)
        {
            player.SetMouseSensitivity(
                mouseSensitivity
            );
        }
        else
        {
            Debug.LogWarning(
                "PlayerMovement was not found. Mouse sensitivity will be saved and loaded when the player starts."
            );

            PlayerPrefs.SetFloat(
                "MouseSensitivity",
                mouseSensitivity
            );
        }

        // Save everything
        PlayerPrefs.SetFloat(
            "MasterVolume",
            master
        );

        PlayerPrefs.SetFloat(
            "MusicVolume",
            music
        );

        PlayerPrefs.SetFloat(
            "SFXVolume",
            sfx
        );

        PlayerPrefs.SetFloat(
            "MouseSensitivity",
            mouseSensitivity
        );

        PlayerPrefs.Save();

        Debug.Log(
            "Audio and mouse sensitivity settings applied."
        );
    }

    // ==========================================
    // CANCEL
    // ==========================================

    public void CancelSettings()
    {
        if (masterSlider != null)
        {
            masterSlider.SetValueWithoutNotify(
                savedMaster
            );
        }

        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(
                savedMusic
            );
        }

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(
                savedSFX
            );
        }

        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.SetValueWithoutNotify(
                savedMouseSensitivity
            );
        }

        RevertAudioToSaved();

        Debug.Log(
            "Audio and mouse sensitivity settings cancelled."
        );
    }

    // ==========================================
    // BACK
    // ==========================================

    public void BackButton()
    {
        Debug.Log(
            "Settings Back Button pressed."
        );

        PlayerPrefs.Save();

        // Pause Menu
        if (openedFromPauseMenu)
        {
            if (PauseMenu.Instance != null)
            {
                PauseMenu.Instance.BackFromSettings();
            }
            else
            {
                Debug.LogError(
                    "PauseMenu.Instance is NULL!"
                );
            }

            return;
        }

        // Main Menu
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }
        else
        {
            Debug.LogError(
                "Main Menu Panel is NOT assigned!"
            );
        }
    }
}