using UnityEngine;

public class MouseLock : MonoBehaviour
{
    [Header("Look Settings")]
    public float mouseSensitivity = 100f;
    public Transform playerCamera;

    private float xRotation = 0f;
    private PlayerMovement playerMovement;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Get a reference to the PlayerMovement script on this same object
        playerMovement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        // SAFETY GATE: If the script component is disabled, stop instantly
        if (!enabled) return;

        // DOUBLE SAFETY: Check the specific movement flag we toggle with the 'B' key
        if (playerMovement != null && !playerMovement.canRotateMouse)
        {
            return; // Freeze camera rotation completely!
        }

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }

        transform.Rotate(Vector3.up * mouseX);
    }
}