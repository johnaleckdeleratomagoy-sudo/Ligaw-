using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class BearAI : MonoBehaviour
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
    public string deathTriggerName = "Die";
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

    private string[] attackTriggers = new string[]
    {
        "Attack1", "Attack2", "Attack3", "Attack4",
        "Attack5", "Attack6", "Attack7", "Attack8"
    };

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

        if (healthCanvasTransform != null && mainCam != null)
        {
            healthCanvasTransform.rotation = mainCam.transform.rotation;
        }

        if (player == null || playerHealth == null)
        {
            FindPlayer();
            if (player == null) return;
        }

        if (playerHealth != null && playerHealth.IsDead)
        {
            if (isAggro)
            {
                isAggro = false;
                SetAnimBools(false, false, true, false, false, false, false);
                PickNewWanderPosition();
            }
            WanderAround();
            return;
        }

        if (currentHealth <= 0) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Auto-aggro check with Buff trigger animation
        if (!isAggro && distanceToPlayer <= chaseRange)
        {
            TriggerAggro();
        }

        if (isAggro)
        {
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

    void TriggerAggro()
    {
        isAggro = true;
        if (anim != null)
        {
            anim.SetTrigger("Buff"); // Plays roar/buff animation upon seeing player
        }
    }

    public void TakeDamage(int amount, Vector3 attackerPosition = default)
    {
        if (isDead) return;

        currentHealth -= amount;

        if (!isAggro)
        {
            TriggerAggro();
        }

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
            // Play directional hit reaction if attacker position is passed
            if (attackerPosition != default)
            {
                PlayDirectionalHit(attackerPosition);
            }
            else if (player != null)
            {
                PlayDirectionalHit(player.position);
            }
        }
    }

    void PlayDirectionalHit(Vector3 attackerPos)
    {
        if (anim == null) return;

        Vector3 hitDirection = (attackerPos - transform.position).normalized;
        float dotForward = Vector3.Dot(transform.forward, hitDirection);
        float dotRight = Vector3.Dot(transform.right, hitDirection);

        if (dotForward > 0.5f)
        {
            anim.SetTrigger("Get Hit Front");
        }
        else if (dotForward < -0.5f)
        {
            anim.SetTrigger("Get Hit Back");
        }
        else if (dotRight > 0f)
        {
            anim.SetTrigger("Get Hit Right");
        }
        else
        {
            anim.SetTrigger("Get Hit Left");
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

        SetAnimBools(false, false, false, false, false, false, false);

        if (anim != null)
        {
            anim.SetTrigger(deathTriggerName);
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

                // Randomly select between Idle, Sit, Eat, or Sleep while resting
                int state = Random.Range(0, 4);
                SetAnimBools(false, false, state == 0, false, state == 1, state == 2, state == 3);
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
            SetAnimBools(true, false, false, false, false, false, false);
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

        SetAnimBools(false, true, false, false, false, false, false);
    }

    void TryAttack()
    {
        if (playerHealth != null && playerHealth.IsDead) return;

        isWaiting = false;
        if (agent.hasPath) agent.ResetPath();

        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        SetAnimBools(false, false, false, true, false, false, false);

        if (Time.time >= nextAttackTime)
        {
            if (anim != null)
            {
                int randomIndex = Random.Range(0, attackTriggers.Length);
                string randomAttack = attackTriggers[randomIndex];
                anim.SetTrigger(randomAttack);
            }

            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }

            nextAttackTime = Time.time + attackRate;
        }
    }

    void SetAnimBools(bool walk, bool run, bool idle, bool combatIdle, bool eat, bool sit, bool sleep)
    {
        if (anim == null) return;

        anim.SetBool("WalkForward", walk);
        anim.SetBool("Run Forward", run);
        anim.SetBool("Idle", idle);
        anim.SetBool("Combat Idle", combatIdle);
        anim.SetBool("Eat", eat);
        anim.SetBool("Sit", sit);
        anim.SetBool("Sleep", sleep);
    }
}