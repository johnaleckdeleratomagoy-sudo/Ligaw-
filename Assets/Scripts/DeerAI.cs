using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class DeerAI : MonoBehaviour
{
    [Header("Target & Flee Settings")]
    public Transform player;
    public float fleeDistance = 25f;
    public float runSpeed = 6f;
    public float walkSpeed = 2f;
    public float fleeDuration = 6f;

    [Header("Health Settings")]
    public int maxHealth = 60;
    public int currentHealth;
    public Slider healthSlider;
    public Transform healthCanvasTransform;

    [Header("Loot Drop Settings")]
    public GameObject meatPrefab;
    public int meatDropCount = 2;

    [Header("Death Settings")]
    public float destroyDelay = 3.0f;

    [Header("Wandering Settings")]
    public float wanderRadius = 20f;
    public float minWanderWait = 2f;
    public float maxWanderWait = 5f;

    private Animator anim;
    private NavMeshAgent agent;
    private Camera mainCam;

    private float wanderWaitTimer = 0f;
    private bool isWaiting = false;
    private bool isDead = false;
    private bool isFleeing = false;
    private float fleeTimer = 0f;

    void Start()
    {
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        mainCam = Camera.main;

        currentHealth = maxHealth;

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (agent == null)
        {
            Debug.LogError("NavMeshAgent missing on " + gameObject.name);
            enabled = false;
            return;
        }

        FindPlayer();
        PickNewWanderPosition();
    }

    void FindPlayer()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    void Update()
    {
        if (isDead) return;

        // Keep UI health bar facing the main camera
        if (healthCanvasTransform != null && mainCam != null)
        {
            healthCanvasTransform.rotation = mainCam.transform.rotation;
        }

        if (player == null)
        {
            FindPlayer();
        }

        if (currentHealth <= 0) return;

        if (isFleeing)
        {
            HandleFleeing();
        }
        else
        {
            WanderAround();
        }

        UpdateAnimatorParameters();
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;

        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartFleeing();
        }
    }

    void StartFleeing()
    {
        isFleeing = true;
        fleeTimer = fleeDuration;
        isWaiting = false;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.speed = runSpeed;
            SetFleeDestination();
        }
    }

    void HandleFleeing()
    {
        fleeTimer -= Time.deltaTime;

        if (fleeTimer <= 0f)
        {
            isFleeing = false;
            PickNewWanderPosition();
            return;
        }

        // Keep updating destination away from player if reached stopping distance
        if (agent.isOnNavMesh && (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance))
        {
            SetFleeDestination();
        }
    }

    void SetFleeDestination()
    {
        if (player == null || !agent.isOnNavMesh) return;

        // Vector away from the player
        Vector3 runDirection = (transform.position - player.position).normalized;
        Vector3 targetPosition = transform.position + runDirection * fleeDistance;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPosition, out hit, fleeDistance, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    void WanderAround()
    {
        if (!agent.isOnNavMesh) return;

        agent.speed = walkSpeed;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!isWaiting)
            {
                isWaiting = true;
                wanderWaitTimer = Random.Range(minWanderWait, maxWanderWait);
            }

            wanderWaitTimer -= Time.deltaTime;
            if (wanderWaitTimer <= 0f)
            {
                PickNewWanderPosition();
            }
        }
        else
        {
            isWaiting = false;
        }
    }

    void PickNewWanderPosition()
    {
        if (!agent.isOnNavMesh) return;

        isWaiting = false;

        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += transform.position;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius * 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    void UpdateAnimatorParameters()
    {
        if (anim == null) return;

        float currentSpeed = agent.velocity.magnitude;

        if (currentSpeed < 0.1f)
        {
            // Idle state: Vert = 0, State = 0
            anim.SetFloat("Vert", 0f);
            anim.SetFloat("State", 0f);
        }
        else if (isFleeing || currentSpeed > walkSpeed + 0.5f)
        {
            // Run state: Vert = 1, State = 1
            anim.SetFloat("Vert", 1f);
            anim.SetFloat("State", 1f);
        }
        else
        {
            // Walk state: Vert = 1, State = 0
            anim.SetFloat("Vert", 1f);
            anim.SetFloat("State", 0f);
        }
    }

    void Die()
    {
        isDead = true;

        if (agent != null)
        {
            if (agent.isOnNavMesh) agent.isStopped = true;
            agent.enabled = false;
        }

        if (anim != null)
        {
            anim.enabled = false; // Disable animator on death
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (healthCanvasTransform != null)
        {
            healthCanvasTransform.gameObject.SetActive(false);
        }

        SpawnMeat();
        Destroy(gameObject, destroyDelay);
    }

    void SpawnMeat()
    {
        MeatDrop.Spawn(meatPrefab, meatDropCount, transform.position);
    }
}