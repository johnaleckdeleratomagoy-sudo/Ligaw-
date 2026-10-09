using UnityEngine;

public class CameraSingleton : MonoBehaviour
{
    private static CameraSingleton instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Keep this camera forever
        }
        else
        {
            // If a new camera spawns (because we loaded the Menu scene again)
            // we kill the new one so the old one stays in control.
            Destroy(gameObject);
        }
    }
}