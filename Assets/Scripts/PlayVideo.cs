using UnityEngine;
using UnityEngine.Video;

public class PlayVideo : MonoBehaviour
{
    public VideoPlayer myVideoPlayer;

    void Start()
    {
        // Start playback
        myVideoPlayer.Play();
    }

    public void PauseVideo()
    {
        if (myVideoPlayer.isPlaying)
            myVideoPlayer.Pause();
    }
}