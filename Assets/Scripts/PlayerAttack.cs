using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    public int attackDamage = 15;
    public float attackRange = 5.0f;
    public float attackRate = 1.0f;

    [Header("References")]
    public Camera mainCamera;
    public Transform attackPoint;

    [Header("Hit Mask")]
    public LayerMask enemyLayers = ~0;

    private float nextAttackTime = 0f;
    private Animator anim;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (attackPoint == null)
        {
            attackPoint = transform;
        }

        // Programmatically exclude the "Player" layer from enemyLayers
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer != -1)
        {
            enemyLayers &= ~(1 << playerLayer);
        }
    }

    void Update()
    {
        if (Time.time >= nextAttackTime && Input.GetButtonDown("Fire1"))
        {
            Attack();
            nextAttackTime = Time.time + 1f / attackRate;
        }
    }

    void Attack()
    {
        if (mainCamera == null) return;

        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit[] hits = Physics.RaycastAll(ray, attackRange + 3.0f, enemyLayers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        bool hitValidTarget = false;

        foreach (RaycastHit hit in hits)
        {
            // Skip player and player children
            if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
                continue;

            // Verify distance from attackPoint/Player
            float distFromPlayer = Vector3.Distance(attackPoint.position, hit.point);
            if (distFromPlayer > attackRange)
                continue;

            // Check for Bear / Enemy
            if (TryDamageTarget(hit.collider.gameObject))
            {
                hitValidTarget = true;
                break;
            }

            // Check for Tree / Choppable object
            if (hit.collider.GetComponentInParent<ChoppableTree>() != null || hit.collider.GetComponent<Terrain>() != null)
            {
                hitValidTarget = true;
                break;
            }
        }

        // Trigger animation ONLY if a valid target was hit
        if (hitValidTarget && anim != null && HasParameter(anim, "Attack"))
        {
            anim.SetTrigger("Attack");
        }
    }

    bool TryDamageTarget(GameObject target)
    {
        BearAI bear = target.GetComponentInParent<BearAI>();
        if (bear != null)
        {
            bear.TakeDamage(attackDamage);
            Debug.Log("<color=green>SUCCESS! Hit Bear and dealt " + attackDamage + " damage!</color>");
            return true;
        }

        WildBoarAI boar = target.GetComponentInParent<WildBoarAI>();
        if (boar != null)
        {
            boar.TakeDamage(attackDamage);
            Debug.Log("<color=green>SUCCESS! Hit Boar and dealt " + attackDamage + " damage!</color>");
            return true;
        }

        DeerAI deer = target.GetComponentInParent<DeerAI>();
        if (deer != null)
        {
            deer.TakeDamage(attackDamage);
            Debug.Log("<color=green>SUCCESS! Hit Deer and dealt " + attackDamage + " damage!</color>");
            return true;
        }

        return false;
    }

    bool HasParameter(Animator animator, string paramName)
    {
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 origin = (attackPoint != null) ? attackPoint.position : transform.position;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin, attackRange * 0.8f);
    }
}