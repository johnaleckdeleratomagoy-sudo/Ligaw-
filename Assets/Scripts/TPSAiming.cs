using UnityEngine;

public class TPSAiming : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;

    [Header("Aim Settings")]
    [SerializeField] private float maxAimDistance = 100f;
    [SerializeField] private LayerMask aimLayers; // Avoid hitting the player capsule

    private Vector3 targetPoint;
    private PlayerMovement playerMovement;

    void Start()
    {
        // Find the PlayerMovement script on this object to read its rotation flag
        playerMovement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        // SAFETY GATE 1: Check standard cursor mode
        if (Cursor.lockState == CursorLockMode.None)
        {
            return;
        }

        // SAFETY GATE 2: Sync with the exact same flag used by PlayerMovement and MouseLock
        if (playerMovement != null && !playerMovement.canRotateMouse)
        {
            return;
        }

        // 1. Find the screen center (0.5, 0.5)
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        // 2. Raycast forward from camera to find what the crosshair is looking at
        if (Physics.Raycast(ray, out hit, maxAimDistance, aimLayers))
        {
            targetPoint = hit.point; // Hit a wall/enemy
        }
        else
        {
            targetPoint = ray.GetPoint(maxAimDistance); // Hit nothing, project into open air
        }

        // 3. For Debugging: See the actual bullet trajectory from the camera to the target
        Debug.DrawLine(mainCamera.transform.position, targetPoint, Color.red);
    }

    public void FireWeapon()
    {
        // Safety check here too so you can't shoot guns while dragging items!
        if (Cursor.lockState == CursorLockMode.None) return;
        if (playerMovement != null && !playerMovement.canRotateMouse) return;

        Vector3 shootDirection = (targetPoint - mainCamera.transform.position).normalized;
    }
}