using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Game Scene")]
    [SerializeField]
    private string gameSceneName = "Forest";

    [Header("Settings")]
    [SerializeField]
    private GameObject settingsPanel;

    // =========================================================
    // NEW GAME
    // =========================================================

    public void OnClickNewGame()
    {
        Debug.Log("[MainMenu] Starting NEW GAME.");

        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.DeleteSave();
            Debug.Log("[MainMenu] Previous player save deleted.");
        }

        if (WorldPickupSave.Instance != null)
        {
            WorldPickupSave.Instance.ClearAllWorldPickups();
            Debug.Log("[MainMenu] World pickup save cleared.");
        }

        Time.timeScale = 1f;

        SceneManager.LoadScene(gameSceneName);
    }
    public void OnClickResume()
    {
        Debug.Log(
            "[MainMenu] RESUME clicked."
        );

        if (SaveSystem.Instance == null)
        {
            Debug.LogError(
                "[MainMenu] SaveSystem.Instance is NULL!"
            );

            return;
        }

        if (!SaveSystem.Instance.HasSaveGame())
        {
            Debug.LogWarning(
                "[MainMenu] NO SAVE GAME FOUND."
            );

            return;
        }

        SaveSystem.SaveData data =
            SaveSystem.Instance.LoadSaveData();

        if (data == null)
        {
            Debug.LogError(
                "[MainMenu] Save data is NULL."
            );

            return;
        }

        Debug.Log(
            "===================================="
        );

        Debug.Log(
            "[MainMenu] RESUMING SAVED GAME"
        );

        Debug.Log(
            "[MainMenu] Saved Scene: " +
            data.sceneName
        );

        if (data.playerData != null)
        {
            Debug.Log(
                "[MainMenu] Saved Position: " +
                new Vector3(
                    data.playerData.positionX,
                    data.playerData.positionY,
                    data.playerData.positionZ
                )
            );
        }

        Debug.Log(
            "===================================="
        );

        Time.timeScale = 1f;

        SaveSystem.Instance.ResumeSavedGame();
    }

    // =========================================================
    // SETTINGS
    // =========================================================

    public void OnClickSettings()
    {
        Debug.Log(
            "[MainMenu] Settings opened."
        );

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    public void OnClickCloseSettings()
    {
        Debug.Log(
            "[MainMenu] Settings closed."
        );

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    // =========================================================
    // QUIT
    // =========================================================

    public void OnClickQuit()
    {
        Debug.Log(
            "[MainMenu] Quit clicked."
        );

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying =
            false;
#else
        Application.Quit();
#endif
    }
}