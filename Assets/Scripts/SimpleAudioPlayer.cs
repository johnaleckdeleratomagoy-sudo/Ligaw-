using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SimpleAudioPlayer : MonoBehaviour
{
    public AudioClip windClip;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        if (windClip != null)
        {
            audioSource.clip = windClip;
            audioSource.loop = true;
            audioSource.spatialBlend = 0f; // 2D sound so distance doesn't matter
            audioSource.volume = 1f;       // Full volume
            audioSource.Play();

            Debug.Log("Playing wind audio loop!");
        }
        else
        {
            Debug.LogError("No AudioClip assigned to SimpleAudioPlayer!");
        }
    }
}