
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Campfire : MonoBehaviour
{
    [Header("Fire VFX & Light")]
    public ParticleSystem torchLightVFX;
    public Light fireLight;
    public AudioSource fireAudio;

    [Header("Light Flicker Settings")]
    public float baseLightIntensity = 3.0f;
    public float flickerSpeed = 8.0f;
    public float flickerAmount = 0.8f;

    [Header("Interaction Settings")]
    public KeyCode interactKey = KeyCode.E;
    public float interactDistance = 3f;

    [Header("Lighting Hold Settings")]
    public float lightHoldDuration = 2.0f;

    [Header("Lighting UI")]
    public GameObject lightingUI;
    public Slider lightingProgressBar;
    public TMP_Text lightingText;

    private bool isLit = false;
    private Transform playerTransform;

    private float holdTimer = 0f;
    private bool isHoldingToLight = false;

    private void Start()
    {
        // Auto-find Particle System if missing
        if (torchLightVFX == null)
        {
            torchLightVFX = GetComponentInChildren<ParticleSystem>();
        }

        // Auto-find Light component if missing
        if (fireLight == null)
        {
            fireLight = GetComponentInChildren<Light>();
        }

        // Auto-find AudioSource component if missing
        if (fireAudio == null)
        {
            fireAudio = GetComponentInChildren<AudioSource>();
        }

        // Find Player reference initially
        FindPlayer();

        // Ensure campfire starts extinguished
        SetFireState(false);

        // Hide lighting UI at start
        HideLightingUI();
    }

    private void Update()
    {
        // Keep searching for player if not found yet
        if (playerTransform == null)
        {
            FindPlayer();

            if (playerTransform == null)
            {
                HideLightingUI();
                return;
            }
        }

        // Measure distance to player
        float distance = Vector3.Distance(
            transform.position,
            playerTransform.position
        );

        // Campfire is already lit
        if (isLit)
        {
            HideLightingUI();
        }
        // Player is close enough
        else if (distance <= interactDistance)
        {
            HandleLighting();
        }
        // Player is too far away
        else
        {
            CancelLighting();
            HideLightingUI();
        }

        // Realistic organic fire flicker
        if (isLit && fireLight != null)
        {
            float noise = Mathf.PerlinNoise(
                Time.time * flickerSpeed,
                0f
            );

            fireLight.intensity =
                baseLightIntensity + (noise * flickerAmount);
        }
    }

    private void HandleLighting()
    {
        ShowLightingUI();

        // Player is holding E
        if (GameInput.Held("Interact", interactKey))
        {
            if (!isHoldingToLight)
            {
                isHoldingToLight = true;
                holdTimer = 0f;
            }

            // Increase hold timer
            holdTimer += Time.deltaTime;

            // Calculate progress
            float progress = Mathf.Clamp01(
                holdTimer / lightHoldDuration
            );

            // Update progress bar
            if (lightingProgressBar != null)
            {
                lightingProgressBar.value = progress;
            }

            // Light the fire when complete
            if (holdTimer >= lightHoldDuration)
            {
                LightFire();
            }
        }
        else
        {
            // E released
            CancelLighting();
        }
    }

    private void LightFire()
    {
        isHoldingToLight = false;
        holdTimer = 0f;

        if (lightingProgressBar != null)
        {
            lightingProgressBar.value = 1f;
        }

        SetFireState(true);

        HideLightingUI();
    }

    private void CancelLighting()
    {
        isHoldingToLight = false;
        holdTimer = 0f;

        if (lightingProgressBar != null)
        {
            lightingProgressBar.value = 0f;
        }
    }

    public bool IsLit
    {
        get { return isLit; }
    }

    // Used by the save system to restore a campfire's state.
    public void RestoreLitState(bool lit)
    {
        SetFireState(lit);
    }

    public void ToggleFire()
    {
        if (!isLit)
        {
            LightFire();
        }
        else
        {
            SetFireState(false);
        }
    }

    private void SetFireState(bool active)
    {
        isLit = active;

        // Particle System
        if (torchLightVFX != null)
        {
            if (active)
            {
                torchLightVFX.Play(true);
            }
            else
            {
                torchLightVFX.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear
                );
            }
        }

        // Point Light
        if (fireLight != null)
        {
            fireLight.enabled = active;
        }

        // Audio
        if (fireAudio != null)
        {
            if (active)
            {
                if (!fireAudio.isPlaying)
                {
                    fireAudio.Play();
                }
            }
            else
            {
                fireAudio.Stop();
            }
        }
    }

    private void ShowLightingUI()
    {
        if (lightingUI != null)
        {
            lightingUI.SetActive(true);
        }

        if (lightingText != null)
        {
            lightingText.text = "Hold E to Light";
        }
    }

    private void HideLightingUI()
    {
        if (lightingUI != null)
        {
            lightingUI.SetActive(false);
        }
    }

    private void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");

        if (p != null)
        {
            playerTransform = p.transform;
        }
        else if (Camera.main != null)
        {
            playerTransform = Camera.main.transform;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            interactDistance
        );
    }

    public void ShowLightingPrompt()
    {
        if (!isLit)
        {
            ShowLightingUI();
        }
    }
}
