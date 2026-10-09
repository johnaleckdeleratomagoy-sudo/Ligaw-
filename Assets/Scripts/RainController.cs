using UnityEngine;

public class RainController : MonoBehaviour
{
    [Header("Debug Settings")]
    [Tooltip("Check this if you want rain to play automatically when you press Play!")]
    public bool autoStartRain = true;

    [Header("Particle References")]
    public ParticleSystem rainParticles;

    [Header("Audio References")]
    public AudioSource rainAudio;
    public AudioSource windAudio;

    [Header("Rain Settings")]
    public float maxEmissionRate = 1000f;
    public float maxRainVolume = 0.8f;

    [Header("Wind Settings")]
    [Tooltip("Normal volume level for wind when it is NOT raining.")]
    public float maxWindVolume = 0.5f;

    [Header("Transition Settings")]
    [Tooltip("Speed at which rain and wind blend between each other.")]
    public float fadeSpeed = 0.5f;

    private ParticleSystem.EmissionModule _emission;
    private float _targetIntensity = 0f; // 0 = Clear/Windy, 1 = Heavy Rain
    private float _currentIntensity = 0f;

    private void Start()
    {
        if (rainParticles != null)
        {
            _emission = rainParticles.emission;
        }
        else
        {
            Debug.LogWarning("[RainController] Rain Particles reference is missing in the Inspector!");
        }

        // Set initial state based on autoStartRain checkbox
        if (autoStartRain)
        {
            _targetIntensity = 1f;
            _currentIntensity = 1f; // Jump straight to 1 for instant testing
        }

        // Ensure wind is initialized properly
        if (windAudio != null && !windAudio.isPlaying)
        {
            windAudio.loop = true;
            windAudio.Play();
        }
    }

    private void Update()
    {
        // Smoothly transition intensity value
        _currentIntensity = Mathf.MoveTowards(_currentIntensity, _targetIntensity, fadeSpeed * Time.deltaTime);

        // 1. Update Particle Emission
        if (rainParticles != null)
        {
            _emission.rateOverTime = _currentIntensity * maxEmissionRate;

            // Ensure the particle system object itself is playing
            if (_currentIntensity > 0.01f && !rainParticles.isPlaying)
            {
                rainParticles.Play();
            }
            else if (_currentIntensity <= 0.01f && rainParticles.isPlaying)
            {
                rainParticles.Stop();
            }
        }

        // 2. Fade Rain Sound IN
        if (rainAudio != null)
        {
            rainAudio.volume = _currentIntensity * maxRainVolume;

            if (_currentIntensity > 0.01f && !rainAudio.isPlaying)
                rainAudio.Play();
            else if (_currentIntensity <= 0.01f && rainAudio.isPlaying)
                rainAudio.Stop();
        }

        // 3. Fade Wind Sound OUT
        if (windAudio != null)
        {
            windAudio.volume = (1f - _currentIntensity) * maxWindVolume;

            if (windAudio.volume <= 0.01f && windAudio.isPlaying)
                windAudio.Pause();
            else if (windAudio.volume > 0.01f && !windAudio.isPlaying)
                windAudio.UnPause();
        }
    }

    // Call these methods from external scripts or triggers
    public void StartRain(float intensity = 1f)
    {
        _targetIntensity = Mathf.Clamp01(intensity);
    }

    public void StopRain()
    {
        _targetIntensity = 0f;
    }
}