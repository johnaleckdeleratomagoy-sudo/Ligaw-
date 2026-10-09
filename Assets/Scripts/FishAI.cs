using UnityEngine;

public class FishAI : MonoBehaviour
{
    [Header("Swimming")]
    [SerializeField] private float swimSpeed = 1.2f;
    [SerializeField] private float turnSpeed = 3f;

    [Header("Swimming Area")]
    [SerializeField] private float swimRadius = 10f;
    [SerializeField] private float minDepth = -3f;
    [SerializeField] private float maxDepth = -0.5f;

    [Header("Bobber Detection")]
    [SerializeField] private float bobberDetectionDistance = 5f;
    [SerializeField] private float biteDistance = 0.7f;

    [Header("Direction")]
    [SerializeField] private float directionChangeTime = 3f;

    private Vector3 homePosition;
    private Vector3 targetDirection;

    private float directionTimer;

    private FishingBobber targetBobber;

    private bool goingToBobber;
    private bool isCaught;

    private void Start()
    {
        homePosition =
            transform.position;

        ChooseRandomDirection();
    }

    private void Update()
    {
        if (isCaught)
            return;

        FindBobber();

        if (targetBobber != null)
        {
            SwimToBobber();
        }
        else
        {
            NormalSwimming();
        }
    }

    // =========================================================
    // FIND BOBBER
    // =========================================================

    private void FindBobber()
    {
        FishingManager manager =
            FishingManager.Instance;

        if (manager == null)
            return;

        FishingBobber[] bobbers =
            FindObjectsByType<FishingBobber>(
                FindObjectsSortMode.None
            );

        targetBobber = null;

        float closestDistance =
            bobberDetectionDistance;

        foreach (
            FishingBobber bobber
            in bobbers)
        {
            if (bobber == null)
                continue;

            float distance =
                Vector3.Distance(
                    transform.position,
                    bobber.transform.position
                );

            if (distance < closestDistance)
            {
                closestDistance =
                    distance;

                targetBobber =
                    bobber;
            }
        }
    }

    // =========================================================
    // NORMAL SWIMMING
    // =========================================================

    private void NormalSwimming()
    {
        directionTimer -=
            Time.deltaTime;

        if (directionTimer <= 0f)
        {
            ChooseRandomDirection();
        }

        MoveInDirection(
            targetDirection
        );

        KeepInsideArea();
    }

    // =========================================================
    // SWIM TO BOBBER
    // =========================================================

    private void SwimToBobber()
    {
        Vector3 direction =
            targetBobber.transform.position -
            transform.position;

        float distance =
            direction.magnitude;

        if (distance <= biteDistance)
        {
            FishingManager manager =
                FishingManager.Instance;

            if (manager != null)
            {
                manager.FishBites(
                    this
                );
            }

            return;
        }

        direction.Normalize();

        MoveInDirection(
            direction
        );
    }

    // =========================================================
    // MOVEMENT
    // =========================================================

    private void MoveInDirection(
        Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(
                direction,
                Vector3.up
            );

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                turnSpeed *
                Time.deltaTime
            );

        transform.position +=
            transform.forward *
            swimSpeed *
            Time.deltaTime;
    }

    // =========================================================
    // RANDOM DIRECTION
    // =========================================================

    private void ChooseRandomDirection()
    {
        targetDirection =
            Random.insideUnitSphere.normalized;

        directionTimer =
            directionChangeTime +
            Random.Range(
                -1f,
                1f
            );
    }

    // =========================================================
    // KEEP FISH INSIDE LAKE
    // =========================================================

    private void KeepInsideArea()
    {
        Vector3 offset =
            transform.position -
            homePosition;

        if (offset.magnitude >
            swimRadius)
        {
            targetDirection =
                -offset.normalized;
        }

        Vector3 position =
            transform.position;

        position.y =
            Mathf.Clamp(
                position.y,
                homePosition.y + minDepth,
                homePosition.y + maxDepth
            );

        transform.position =
            position;
    }

    // =========================================================
    // CAUGHT
    // =========================================================

    public void OnCaught()
    {
        isCaught = true;

        gameObject.SetActive(
            false
        );

        Debug.Log(
            "[FishAI] Fish caught."
        );
    }
}