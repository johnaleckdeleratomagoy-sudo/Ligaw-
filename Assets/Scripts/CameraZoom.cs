using UnityEngine;
using UnityEngine.SceneManagement; // Needed to load the next scene

public class CameraZoom : MonoBehaviour
{
    [Header("Targets")]
    public Transform defaultTarget;   // Your current "Main Menu" view
    public Transform settingsTarget;
    public Transform quitTarget;
    public Transform playTarget;     // NEW: Create an empty object inside the Play Portal

    [Header("Settings")]
    public float speed = 2f;
    public string gameSceneName = "GameScene"; // The name of your actual game level

    private Transform currentTarget;
    private bool isZooming = false;
    private bool startingGame = false; // To check if we should load the scene

    void Start()
    {
        // Start at the Main Menu view immediately
        if (defaultTarget != null)
        {
            currentTarget = defaultTarget;
            transform.position = defaultTarget.position;
            transform.rotation = defaultTarget.rotation;
        }
    }

    void Update()
    {
        if (isZooming && currentTarget != null)
        {
            transform.position = Vector3.Lerp(transform.position, currentTarget.position, Time.deltaTime * speed);
            transform.rotation = Quaternion.Slerp(transform.rotation, currentTarget.rotation, Time.deltaTime * speed);

            // Check if we are close enough to the target
            if (Vector3.Distance(transform.position, currentTarget.position) < 0.1f)
            {
                isZooming = false;

                // If this zoom was triggered by the Play button, load the scene
                if (startingGame)
                {
                    SceneManager.LoadScene(gameSceneName);
                }
            }
        }
    }

    // Call this from your "PLAY" Button's OnClick event
    public void StartGameZoom()
    {
        if (playTarget != null)
        {
            currentTarget = playTarget;
            isZooming = true;
            startingGame = true; // Tell the script to load the scene when finished
        }
        else
        {
            // If you haven't set a playTarget, just load the scene immediately
            SceneManager.LoadScene(gameSceneName);
        }
    }

    public void ZoomToSettings() { startingGame = false; currentTarget = settingsTarget; isZooming = true; }
    public void ZoomToQuit() { startingGame = false; currentTarget = quitTarget; isZooming = true; }
    public void ZoomToDefault() { startingGame = false; currentTarget = defaultTarget; isZooming = true; }
}