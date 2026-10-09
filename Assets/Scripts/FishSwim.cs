using UnityEngine;

public class FishSwim : MonoBehaviour
{
    [Header("Swimming")]
    public float swimSpeed = 1.5f;
    public float turnSpeed = 2f;

    [Header("Movement Area")]
    public float swimRadius = 8f;
    public float minHeight = -2f;
    public float maxHeight = 2f;

    [Header("Direction Change")]
    public float directionChangeTime = 3f;

    private Vector3 centerPosition;
    private Vector3 swimDirection;
    private float directionTimer;

    private void Start()
    {
        centerPosition = transform.position;

        ChooseNewDirection();
    }

    private void Update()
    {
        directionTimer -= Time.deltaTime;

        if (directionTimer <= 0f)
        {
            ChooseNewDirection();
        }

        Vector3 targetDirection = swimDirection;

        if (targetDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(targetDirection, Vector3.up);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );
        }

        transform.position +=
            transform.forward * swimSpeed * Time.deltaTime;

        KeepFishInsideArea();
    }

    private void ChooseNewDirection()
    {
        Vector3 randomDirection = Random.insideUnitSphere;

        randomDirection.y = Random.Range(
            minHeight,
            maxHeight
        );

        if (randomDirection.sqrMagnitude < 0.01f)
        {
            randomDirection = Vector3.forward;
        }

        swimDirection = randomDirection.normalized;

        directionTimer = directionChangeTime +
                         Random.Range(-1f, 1f);
    }

    private void KeepFishInsideArea()
    {
        Vector3 offset = transform.position - centerPosition;

        if (offset.magnitude > swimRadius)
        {
            Vector3 directionToCenter = -offset.normalized;

            swimDirection = Vector3.Lerp(
                swimDirection,
                directionToCenter,
                2f * Time.deltaTime
            ).normalized;
        }

        Vector3 position = transform.position;

        float minY = centerPosition.y + minHeight;
        float maxY = centerPosition.y + maxHeight;

        position.y = Mathf.Clamp(
            position.y,
            minY,
            maxY
        );

        transform.position = position;
    }
}