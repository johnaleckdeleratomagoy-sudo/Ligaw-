using UnityEngine;

// Run after CinemachineBrain (order 200) so the line is built from this
// frame's final camera position/rotation, not last frame's.
[DefaultExecutionOrder(300)]
public class SpearTrajectory : MonoBehaviour
{
    [Header("Trajectory")]
    [SerializeField] private bool showTrajectory = true;
    [SerializeField] private int trajectoryPoints = 30;
    [SerializeField] private float trajectoryTimeStep = 0.08f;
    [SerializeField] private float trajectoryMaxTime = 2.4f;

    [Header("Throw")]
    [SerializeField] private float minimumThrowForce = 8f;
    [SerializeField] private float maximumThrowForce = 35f;

    [Header("Aim")]
    [Tooltip("0.5 = center of screen.")]
    [SerializeField] private float aimScreenX = 0.5f;

    [Tooltip("0 = bottom, 1 = top. Change this if your crosshair is not centered vertically.")]
    [SerializeField] private float aimScreenY = 0.603f;

    [Tooltip("How far the crosshair looks for something to aim at.")]
    [SerializeField] private float maxAimDistance = 100f;


    [Header("Spear Spawn")]
    [Tooltip("Optional. If empty, the trajectory starts in front of the active camera.")]
    [SerializeField] private Transform trajectoryStartPoint;

    [SerializeField] private float startForwardOffset = 1.5f;
    [SerializeField] private float startVerticalOffset = -0.1f;

    [Header("Line")]
    [SerializeField] private float startWidth = 0.025f;
    [SerializeField] private float endWidth = 0.008f;

    private LineRenderer lineRenderer;
    private Camera activeCamera;

    private bool isShowingTrajectory;
    private float chargeAmount;

    private void Awake()
    {
        CreateLineRenderer();
        HideTrajectory();
    }

    private void LateUpdate()
    {
        if (!isShowingTrajectory || !showTrajectory)
            return;

        UpdateTrajectory();
    }

    private void CreateLineRenderer()
    {
        lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = trajectoryPoints;

        lineRenderer.startWidth = startWidth;
        lineRenderer.endWidth = endWidth;

        lineRenderer.numCornerVertices = 4;
        lineRenderer.numCapVertices = 4;

        lineRenderer.enabled = false;

        Shader shader = Shader.Find("HDRP/Unlit");

        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        if (shader != null)
        {
            Material material = new Material(shader);

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", new Color(1f, 0.65f, 0f, 1f));
            else if (material.HasProperty("_Color"))
                material.SetColor("_Color", new Color(1f, 0.65f, 0f, 1f));

            lineRenderer.material = material;
        }
    }

    public void BeginTrajectory(float currentChargeAmount)
    {
        if (!showTrajectory)
            return;

        chargeAmount = Mathf.Clamp01(currentChargeAmount);
        isShowingTrajectory = true;

        if (lineRenderer != null)
            lineRenderer.enabled = true;

        UpdateTrajectory();
    }

    public void SetCharge(float currentChargeAmount)
    {
        chargeAmount = Mathf.Clamp01(currentChargeAmount);
    }

    public void HideTrajectory()
    {
        isShowingTrajectory = false;

        if (lineRenderer != null)
            lineRenderer.enabled = false;
    }

    private void UpdateTrajectory()
    {
        activeCamera = GetActiveCamera();

        if (activeCamera == null || lineRenderer == null)
            return;

        Vector3 startPosition = GetStartPosition(activeCamera);

        Vector3 direction =
            GetAimDirection(activeCamera, startPosition);

        float throwForce =
            Mathf.Lerp(
                minimumThrowForce,
                maximumThrowForce,
                chargeAmount
            );

        Vector3 initialVelocity =
            direction * throwForce;

        Vector3 gravity = Physics.gravity;

        lineRenderer.positionCount = trajectoryPoints;

        for (int i = 0; i < trajectoryPoints; i++)
        {
            float time =
                Mathf.Min(
                    i * trajectoryTimeStep,
                    trajectoryMaxTime
                );

            Vector3 position =
                startPosition +
                initialVelocity * time +
                0.5f * gravity * time * time;

            lineRenderer.SetPosition(i, position);
        }
    }

    // =========================================================
    // SHARED WITH THE ACTUAL THROW (HotbarManager)
    // =========================================================

    private Transform playerRoot;
    private Transform handBone;

    private void CachePlayer()
    {
        if (playerRoot == null)
        {
            PlayerMovement player =
                FindFirstObjectByType<PlayerMovement>();

            if (player != null)
                playerRoot = player.transform;
        }

        if (handBone == null && playerRoot != null)
        {
            Animator animator =
                playerRoot.GetComponentInChildren<Animator>();

            if (animator != null && animator.isHuman)
            {
                handBone = animator.GetBoneTransform(
                    HumanBodyBones.RightHand
                );
            }
        }
    }

    /// <summary>Where the spear leaves from: the right hand.</summary>
    public Vector3 GetStartPosition(Camera cam)
    {
        if (trajectoryStartPoint != null)
            return trajectoryStartPoint.position;

        CachePlayer();

        if (handBone != null)
            return handBone.position;

        if (playerRoot != null)
            return playerRoot.position + Vector3.up * 1.5f;

        return cam.transform.position +
               cam.transform.forward * startForwardOffset +
               cam.transform.up * startVerticalOffset;
    }

    /// <summary>
    /// Direction from the start (hand) to whatever the crosshair points at.
    /// </summary>
    public Vector3 GetAimDirection(Camera cam, Vector3 startPosition)
    {
        CachePlayer();

        Ray aimRay = cam.ScreenPointToRay(
            new Vector2(
                Screen.width * aimScreenX,
                Screen.height * aimScreenY
            )
        );

        Vector3 targetPoint =
            aimRay.GetPoint(maxAimDistance);

        RaycastHit[] hits = Physics.RaycastAll(
            aimRay,
            maxAimDistance,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        float nearest = float.MaxValue;

        foreach (RaycastHit hit in hits)
        {
            // Ignore the player's own colliders so we aim past them.
            if (playerRoot != null &&
                hit.collider.transform.IsChildOf(playerRoot))
            {
                continue;
            }

            if (hit.distance < nearest)
            {
                nearest = hit.distance;
                targetPoint = hit.point;
            }
        }

        Vector3 toTarget = targetPoint - startPosition;

        // If the target is behind or right at the hand, just use the
        // camera ray so the spear never flies backwards.
        if (toTarget.sqrMagnitude < 1f ||
            Vector3.Dot(toTarget, aimRay.direction) <= 0f)
        {
            return aimRay.direction.normalized;
        }

        return toTarget.normalized;
    }

    private Camera GetActiveCamera()
    {
        Camera mainCamera = Camera.main;

        if (mainCamera != null && mainCamera.isActiveAndEnabled)
            return mainCamera;

        Camera[] cameras = FindObjectsByType<Camera>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (Camera cam in cameras)
        {
            if (cam != null && cam.isActiveAndEnabled)
                return cam;
        }

        return null;
    }
}
