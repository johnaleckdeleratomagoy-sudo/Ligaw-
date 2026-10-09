using UnityEngine;
using UnityEngine.SceneManagement;

public class QuitConfirmation : MonoBehaviour
{
    [Header("Quit Confirmation Panel")]
    public GameObject quitPanel;

    [Header("Main Menu Scene")]
    public string mainMenuSceneName = "MainMenu";

    private void Start()
    {
        if (quitPanel != null)
        {
            quitPanel.SetActive(false);
        }
    }

    public void ShowQuitPanel()
    {
        if (quitPanel != null)
        {
            quitPanel.SetActive(true);
        }
    }

    public void HideQuitPanel()
    {
        if (quitPanel != null)
        {
            quitPanel.SetActive(false);
        }
    }

    // Quit the game
    public void ConfirmQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Go back to the Main Menu scene
    public void BackToMainMenu()
    {
        if (quitPanel != null)
        {
            quitPanel.SetActive(false);
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }
}