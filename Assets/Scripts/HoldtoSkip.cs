using UnityEngine;
using UnityEngine.SceneManagement;

public class HoldtoSkip : MonoBehaviour
{
    [Header("Skip Settings")]
    public float lockDuration = 2.0f;
    public string nextSceneName;

    [Header("UI")]
    public GameObject skipUI;

    private float levelTimer = 0f;
    private bool canSkip = false;
    private bool hasSkipped = false;

    void Start()
    {
        // Show skip UI if assigned
        if (skipUI != null)
        {
            skipUI.SetActive(true);
        }

        // Wait before allowing the player to skip
        levelTimer = 0f;
        canSkip = false;
    }

    void Update()
    {
        // Timer
        levelTimer += Time.deltaTime;

        // Enable skipping after the lock duration
        if (!canSkip && levelTimer >= lockDuration)
        {
            canSkip = true;
        }

        // Do nothing until skipping is allowed
        if (!canSkip || hasSkipped)
            return;

        // Mouse click
        if (Input.GetMouseButtonDown(0))
        {
            LoadNextLevel();
            return;
        }

        // Space
        if (Input.GetKeyDown(KeyCode.Space))
        {
            LoadNextLevel();
            return;
        }

        // Enter
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            LoadNextLevel();
            return;
        }
    }

    void LoadNextLevel()
    {
        if (hasSkipped)
            return;

        hasSkipped = true;

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}