using UnityEngine;

public class SpearThrow : MonoBehaviour
{
    [Header("Spear Item Data")]
    public ItemDataSO spearItemData;

    [Header("Throw Settings")]
    [SerializeField] private float minimumForce = 8f;
    [SerializeField] private float maximumForce = 35f;

    [Header("Charge Settings")]
    [SerializeField] private float chargeTime = 1.5f;

    [Header("Model Offset Settings")]
    [Tooltip("Rotation needed to make the spear tip point along +Z.")]
    [SerializeField]
    private Vector3 meshOffsetEuler =
        new Vector3(90f, 0f, 0f);

    [Header("World Settings")]
    [Tooltip("Set to 0 so the thrown spear remains in the world.")]
    [SerializeField] private float destroyAfterSeconds = 0f;

    [Tooltip("Makes the spear rotate toward its movement direction.")]
    [SerializeField] private bool rotateWithVelocity = true;

    [Tooltip("Stops the spear when it hits something.")]
    [SerializeField] private bool stopOnHit = true;

    [Header("Thrown Spear Damage")]
    [SerializeField] private int throwDamage = 40;

    private Rigidbody rb;
    private bool hasBeenThrown = false;
    private bool hasHitSomething = false;

    public bool HasBeenThrown => hasBeenThrown;
    public bool HasHitSomething => hasHitSomething;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;
    }

    private float GetThrowForce(float chargeAmount)
    {
        chargeAmount = Mathf.Clamp01(chargeAmount);

        return Mathf.Lerp(
            minimumForce,
            maximumForce,
            chargeAmount
        );
    }

    public void Throw(
        Vector3 direction,
        float chargeAmount)
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();

            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
        }

        Vector3 throwDirection =
            direction.sqrMagnitude > 0.001f
                ? direction.normalized
                : transform.forward;

        hasBeenThrown = true;
        hasHitSomething = false;

        float force =
            GetThrowForce(chargeAmount);

        // Start the spear pointing in the actual free-aim direction.
        transform.rotation =
            Quaternion.LookRotation(
                throwDirection,
                Vector3.up
            ) *
            Quaternion.Euler(
                meshOffsetEuler
            );

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // VelocityChange ignores mass, so the spear leaves at exactly
        // `force` m/s - the same speed SpearTrajectory draws. (Impulse
        // divides by mass, so any spear mass other than 1 made the
        // spear fly differently from the aim line.)
        rb.AddForce(
            throwDirection * force,
            ForceMode.VelocityChange
        );

        if (destroyAfterSeconds > 0f)
        {
            Destroy(
                gameObject,
                destroyAfterSeconds
            );
        }

        Debug.Log(
            "[SpearThrow] Thrown. " +
            "Charge: " +
            chargeAmount.ToString("F2") +
            " Force: " +
            force.ToString("F2") +
            " Direction: " +
            throwDirection
        );
    }

    private void Update()
    {
        if (!hasBeenThrown)
            return;

        if (!rotateWithVelocity)
            return;

        if (hasHitSomething)
            return;

        if (rb == null)
            return;

        Vector3 velocity =
            rb.linearVelocity;

        if (velocity.sqrMagnitude < 0.1f)
            return;

        Vector3 movementDirection =
            velocity.normalized;

        transform.rotation =
            Quaternion.LookRotation(
                movementDirection,
                Vector3.up
            ) *
            Quaternion.Euler(
                meshOffsetEuler
            );
    }

    private void OnCollisionEnter(
        Collision collision)
    {
        if (!hasBeenThrown)
            return;

        if (hasHitSomething)
            return;

        hasHitSomething = true;

        // Damage animals using the same animal systems
        // already used by HotbarManager's spear stab.
        TryDamageAnimal(
            collision.gameObject
        );

        if (stopOnHit && rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.isKinematic = true;
            rb.useGravity = false;
        }

        Debug.Log(
            "[SpearThrow] Spear hit: " +
            collision.gameObject.name
        );
    }

    private bool TryDamageAnimal(
        GameObject hitObject)
    {
        if (hitObject == null)
            return false;

        BearAI bear =
            hitObject.GetComponentInParent<BearAI>();

        if (bear != null)
        {
            bear.TakeDamage(
                throwDamage,
                hitObject.transform.position
            );

            Debug.Log(
                "[SpearThrow] Spear damaged Bear for " +
                throwDamage
            );

            return true;
        }

        WildBoarAI boar =
            hitObject.GetComponentInParent<WildBoarAI>();

        if (boar != null)
        {
            boar.TakeDamage(
                throwDamage
            );

            Debug.Log(
                "[SpearThrow] Spear damaged Wild Boar for " +
                throwDamage
            );

            return true;
        }

        DeerAI deer =
            hitObject.GetComponentInParent<DeerAI>();

        if (deer != null)
        {
            deer.TakeDamage(
                throwDamage
            );

            Debug.Log(
                "[SpearThrow] Spear damaged Deer for " +
                throwDamage
            );

            return true;
        }

        return false;
    }

    public void ResetSpear()
    {
        hasBeenThrown = false;
        hasHitSomething = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.isKinematic = true;
            rb.useGravity = false;
        }

        transform.localRotation =
            Quaternion.Euler(
                meshOffsetEuler
            );
    }
}
