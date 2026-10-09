using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;

    [Header("Scene Name")]
    public string gameScene = "PlayerScene";

    private GameObject currentPanel;

    void Start()
    {
        ShowPanel(mainMenuPanel);
    }

    // EASY
    public void Easy()
    {
        PlayerPrefs.SetString("Difficulty", "Easy");
        LoadGame();
    }

    // MEDIUM
    public void Medium()
    {
        PlayerPrefs.SetString("Difficulty", "Medium");
        LoadGame();
    }

    // HARD
    public void Hard()
    {
        PlayerPrefs.SetString("Difficulty", "Hard");
        LoadGame();
    }

    // LOAD GAME
    void LoadGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameScene);
    }

    // SHOW ANY PANEL
    public void ShowPanel(GameObject panel)
    {
        if (currentPanel != null)
            currentPanel.SetActive(false);

        panel.SetActive(true);
        currentPanel = panel;
    }

    // BACK BUTTON
    public void BackToMenu()
    {
        ShowPanel(mainMenuPanel);
    }

    // QUIT GAME
    public void QuitGame()
    {
        Debug.Log("Quit Game");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}