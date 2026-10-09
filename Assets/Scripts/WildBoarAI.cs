using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class WildBoarAI : MonoBehaviour
{
    [Header("Target & Detection")]
    public Transform player;
    public float chaseRange = 20f;
    public float attackRange = 2.5f;
    public float chaseSpeed = 3.5f;
    public float walkSpeed = 1.5f;
    public int attackDamage = 20;
    public float attackRate = 1.5f;

    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;
    public Slider healthSlider;
    public Transform healthCanvasTransform;

    [Header("Loot Drop Settings")]
    public GameObject meatPrefab;
    public int meatDropCount = 2;

    [Header("Death Settings")]
    public float destroyDelay = 4.0f;

    [Header("Wandering Settings")]
    public float wanderRadius = 15f;
    public float minWanderWait = 1f;
    public float maxWanderWait = 3f;

    [Header("Aggro Settings")]
    public bool isAggro = false;

    private float nextAttackTime = 0f;
    private Animator anim;
    private NavMeshAgent agent;
    private Camera mainCam;

    private float wanderWaitTimer = 0f;
    private bool isWaiting = false;
    private bool isDead = false;

    private PlayerHealth playerHealth;

    // Animation trigger pools matching your Animator Parameters
    private string[] attackTriggers = new string[] { "Attack1", "Attack2" };
    private string[] hitTriggers = new string[] { "Hit1", "Hit2" };
    private string[] deathTriggers = new string[] { "Death1", "Death2" };

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

        if (player != null && playerHealth == null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
        }
    }

    void Update()
    {
        if (isDead) return;

        // Keep health canvas facing main camera
        if (healthCanvasTransform != null && mainCam != null)
        {
            healthCanvasTransform.rotation = mainCam.transform.rotation;
        }

        if (player == null || playerHealth == null)
        {
            FindPlayer();
            if (player == null) return;
        }

        // Return to wander if player is dead
        if (playerHealth != null && playerHealth.IsDead)
        {
            if (isAggro)
            {
                isAggro = false;
                SetAnimBools(false, false, true);
                PickNewWanderPosition();
            }
            WanderAround();
            return;
        }

        if (currentHealth <= 0) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // REMOVED: Auto-aggro when player enters detection range (chaseRange)

        if (isAggro)
        {
            // Stop chasing if player gets too far away (e.g. escaped)
            if (distanceToPlayer > chaseRange)
            {
                isAggro = false;
                PickNewWanderPosition();
            }
            else if (distanceToPlayer <= attackRange)
            {
                TryAttack();
            }
            else
            {
                ChasePlayer();
            }
        }
        else
        {
            WanderAround();
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;

        // Attacking the boar triggers aggro!
        isAggro = true;

        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else if (anim != null)
        {
            // Randomly pick between Hit1 and Hit2 triggers
            string randomHit = hitTriggers[Random.Range(0, hitTriggers.Length)];
            anim.SetTrigger(randomHit);
        }
    }

    // Call this method if you want to stun the boar during combat
    public void StunBoar()
    {
        if (isDead || anim == null) return;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }

        anim.SetTrigger("FullStun");
    }

    void Die()
    {
        isDead = true;

        if (agent != null)
        {
            if (agent.isOnNavMesh) agent.isStopped = true;
            agent.enabled = false;
        }

        SetAnimBools(false, false, false);

        if (anim != null)
        {
            // Randomly select between Death1 and Death2 triggers
            string randomDeath = deathTriggers[Random.Range(0, deathTriggers.Length)];
            anim.SetTrigger(randomDeath);
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
                SetAnimBools(false, false, true);
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
            SetAnimBools(true, false, false);
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

    void ChasePlayer()
    {
        isWaiting = false;

        agent.speed = chaseSpeed;
        agent.SetDestination(player.position);

        SetAnimBools(false, true, false);
    }

    void TryAttack()
    {
        if (playerHealth != null && playerHealth.IsDead) return;

        isWaiting = false;
        if (agent.hasPath) agent.ResetPath();

        // Turn boar towards player during attack
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        SetAnimBools(false, false, true);

        if (Time.time >= nextAttackTime)
        {
            if (anim != null)
            {
                // Randomly trigger Attack1 or Attack2
                string randomAttack = attackTriggers[Random.Range(0, attackTriggers.Length)];
                anim.SetTrigger(randomAttack);
            }

            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }

            nextAttackTime = Time.time + attackRate;
        }
    }

    void SetAnimBools(bool walk, bool run, bool idle)
    {
        if (anim == null) return;

        anim.SetBool("Walk", walk);
        anim.SetBool("Run", run);
        anim.SetBool("Idle", idle);
    }
}