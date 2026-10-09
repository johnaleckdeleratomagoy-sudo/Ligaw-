using UnityEngine;

public class WorkbenchInteraction : MonoBehaviour
{
    [Header("UI Reference")]
    public GameObject craftingPanel;

    [Header("Interaction Settings")]
    public KeyCode interactKey = KeyCode.E;

    [Header("Player Freeze Settings")]
    // Drag your Player GameObject or Movement Script here
    public MonoBehaviour playerMovementScript;
    public MonoBehaviour cameraLookScript;

    private bool isPlayerInside = false;

    private void Update()
    {
        if (isPlayerInside && GameInput.Pressed("Interact", interactKey))
        {
            ToggleWorkbench();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;

            // Auto-assign player scripts if not set in inspector
            if (playerMovementScript == null)
                playerMovementScript = other.GetComponent<MonoBehaviour>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;

            if (craftingPanel != null && craftingPanel.activeSelf)
            {
                CloseWorkbench();
            }
        }
    }

    public void ToggleWorkbench()
    {
        if (craftingPanel == null) return;

        bool isOpen = !craftingPanel.activeSelf;
        craftingPanel.SetActive(isOpen);

        // Toggle Mouse Cursor
        Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isOpen;

        // Freeze / Unfreeze Player Movement & Camera
        FreezePlayer(isOpen);

        // Update Inventory state
        Inventory inv = FindFirstObjectByType<Inventory>();
        if (inv != null)
        {
            inv.SetAtWorkbenchState(isOpen);
        }
    }

    private void CloseWorkbench()
    {
        craftingPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        FreezePlayer(false);

        Inventory inv = FindFirstObjectByType<Inventory>();
        if (inv != null)
        {
            inv.SetAtWorkbenchState(false);
        }
    }

    private void FreezePlayer(bool freeze)
    {
        // Disabling movement & look scripts stops player control completely
        if (playerMovementScript != null)
            playerMovementScript.enabled = !freeze;

        if (cameraLookScript != null)
            cameraLookScript.enabled = !freeze;
    }
}