using UnityEngine;
using UnityEngine.UI; // Needed for the Image
using UnityEngine.SceneManagement;
using System.Collections;

public class TeleportTransition : MonoBehaviour
{
    [Header("References")]
    public AudioSource bgMusic;    // Drag your music here
    public Image fadeImage;        // Drag your black UI Image here

    [Header("Settings")]
    public float fadeSpeed = 1.0f; // How many seconds to fade
    public string destinationScene; // Name of the scene to load

    private void OnTriggerEnter(Collider other)
    {
        // Check if the thing hitting the door is the Player
        if (other.CompareTag("Player"))
        {
            StartCoroutine(BeginTransition());
        }
    }

    IEnumerator BeginTransition()
    {
        float timer = 0;

        while (timer < fadeSpeed)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeSpeed;

            // 1. Fade Music DOWN (1 to 0)
            if (bgMusic != null)
                bgMusic.volume = Mathf.Lerp(1f, 0f, progress);

            // 2. Fade Black Image UP (0 to 1)
            if (fadeImage != null)
            {
                Color tempColor = fadeImage.color;
                tempColor.a = Mathf.Lerp(0f, 1f, progress);
                fadeImage.color = tempColor;
            }

            yield return null; // Wait for next frame
        }

        // 3. Load the new area!
        SceneManager.LoadScene(destinationScene);
    }
}