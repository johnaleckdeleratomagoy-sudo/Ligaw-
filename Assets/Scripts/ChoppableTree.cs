using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class ChoppableTree : MonoBehaviour
{
    [Header("Tree Stats")]
    public int maxHealth = 3;
    private int currentHealth;

    [Header("Physics Falling Effect")]
    [Tooltip("If true, the tree tips over like a real tree before spawning logs.")]
    public bool fallBeforeSpawningLogs = true;
    public float fallSpeed = 2f;

    [Header("Spawns")]
    public GameObject logPrefab;
    public int logsToSpawn = 3;

    [Header("Feedback Effects")]
    public float shakeIntensity = 0.15f;
    public float shakeDuration = 0.2f;

    public AudioSource audioSource;
    public AudioClip hitSound;
    public AudioClip fallSound;

    public ParticleSystem hitParticles;

    [Header("Visual Settings")]
    [Tooltip("Assign the mesh model child transform here so shaking does NOT shift the root collider.")]
    [SerializeField] private Transform visualModelTransform;

    public Transform VisualModel => visualModelTransform != null ? visualModelTransform : transform;

    private Vector3 baseVisualPosition;
    private bool isDestroyed = false;
    private Coroutine shakeCoroutine;

    private void Awake()
    {
        currentHealth = maxHealth;

        // Find AudioSource automatically if one is not assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // Prevent the tree fall sound from playing when the scene starts
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }

        // Auto-assign first child transform if visualModelTransform isn't set
        if (visualModelTransform == null && transform.childCount > 0)
        {
            visualModelTransform = transform.GetChild(0);
        }

        // Cache local position of the visual transform
        if (visualModelTransform != null)
        {
            baseVisualPosition = visualModelTransform.localPosition;
        }
    }

    private void Start()
    {
        // Extra safety in case the AudioSource was assigned after Awake
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }
    }

    public void TakeDamage(int damageAmount = 1)
    {
        if (isDestroyed) return;

        currentHealth -= damageAmount;

        Debug.Log(
            $"[ChoppableTree] {gameObject.name} took {damageAmount} damage. Health remaining: {currentHealth}",
            this
        );

        // Play chopping effects
        PlayChopEffects();

        // Tree has no health left
        if (currentHealth <= 0)
        {
            FellTree();
        }
    }

    public void PlayChopEffects()
    {
        if (isDestroyed) return;

        // Play wood hit particles
        if (hitParticles != null)
        {
            hitParticles.Play();
        }

        // Play chopping sound
        PlaySound(hitSound);

        // Shake tree
        if (gameObject.activeInHierarchy && visualModelTransform != null)
        {
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);

                // Reset position before starting a new shake
                visualModelTransform.localPosition = baseVisualPosition;
            }

            shakeCoroutine = StartCoroutine(ShakeTree());
        }
    }

    private IEnumerator ShakeTree()
    {
        if (visualModelTransform == null)
            yield break;

        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            if (isDestroyed)
                yield break;

            Vector3 randomOffset = new Vector3(
                Random.Range(-shakeIntensity, shakeIntensity),
                0f,
                Random.Range(-shakeIntensity, shakeIntensity)
            );

            visualModelTransform.localPosition =
                baseVisualPosition + randomOffset;

            elapsed += Time.deltaTime;

            yield return null;
        }

        if (!isDestroyed)
        {
            visualModelTransform.localPosition = baseVisualPosition;
        }
    }

    public void SetHealth(int health)
    {
        currentHealth = health;
    }

    // Plays a clip even if there is no AudioSource, and keeps playing
    // after this object is destroyed.
    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;

        if (audioSource != null)
            audioSource.PlayOneShot(clip);
        else
            AudioSource.PlayClipAtPoint(clip, transform.position);
    }

    public void FellTree()
    {
        if (isDestroyed)
            return;

        isDestroyed = true;

        // Stop shaking
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }

        // Reset visual position
        if (visualModelTransform != null)
        {
            visualModelTransform.localPosition = baseVisualPosition;
        }

        // IMPORTANT:
        // Play the tree fall sound ONLY when the tree actually falls.
        PlaySound(fallSound);

        // Convert colliders to triggers so falling tree doesn't block player
        Collider[] allColliders = GetComponentsInChildren<Collider>();

        foreach (Collider col in allColliders)
        {
            col.isTrigger = true;
        }

        // Disable NavMeshObstacle if present
        if (TryGetComponent(out NavMeshObstacle navObstacle))
        {
            navObstacle.enabled = false;
        }

        // Start falling animation
        if (fallBeforeSpawningLogs)
        {
            StartCoroutine(FallAndSpawnSequence());
        }
        else
        {
            SpawnLogsAndDestroy();
        }
    }

    private IEnumerator FallAndSpawnSequence()
    {
        // Safe random fall direction calculation
        Vector2 randomCircle = Random.insideUnitCircle.normalized;

        if (randomCircle == Vector2.zero)
        {
            randomCircle = Vector2.right;
        }

        Vector3 fallDirection = new Vector3(
            randomCircle.x,
            0f,
            randomCircle.y
        );

        Vector3 fallAxis = Vector3.Cross(
            fallDirection,
            Vector3.up
        );

        Vector3 pivotPoint = transform.position;

        float elapsed = 0f;
        float totalDuration = 1.2f;

        while (elapsed < totalDuration)
        {
            float stepAngle =
                (90f / totalDuration) *
                Time.deltaTime *
                fallSpeed;

            transform.RotateAround(
                pivotPoint,
                fallAxis,
                stepAngle
            );

            elapsed += Time.deltaTime;

            yield return null;
        }

        // Small delay before logs spawn
        yield return new WaitForSeconds(0.3f);

        SpawnLogsAndDestroy();
    }

    private void SpawnLogsAndDestroy()
    {
        Vector3 spawnOrigin =
            transform.position + Vector3.up * 0.5f;

        if (logPrefab != null)
        {
            for (int i = 0; i < logsToSpawn; i++)
            {
                Vector3 spawnPos = spawnOrigin + new Vector3(
                    Random.Range(-0.5f, 0.5f),
                    0.2f + (i * 0.3f),
                    Random.Range(-0.5f, 0.5f)
                );

                GameObject logObj = Instantiate(
                    logPrefab,
                    spawnPos,
                    Quaternion.Euler(
                        0,
                        Random.Range(0, 360),
                        0
                    )
                );

                if (logObj.TryGetComponent(out Rigidbody rb))
                {
                    Vector3 popDirection = new Vector3(
                        Random.Range(-1f, 1f),
                        1.8f,
                        Random.Range(-1f, 1f)
                    ).normalized;

                    rb.AddForce(
                        popDirection * Random.Range(2f, 4f),
                        ForceMode.Impulse
                    );
                }
            }
        }
        else
        {
            Debug.LogWarning(
                $"[ChoppableTree] Log Prefab missing on {gameObject.name}! No logs spawned."
            );
        }

        Destroy(gameObject);
    }
}