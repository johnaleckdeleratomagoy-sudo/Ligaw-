using UnityEngine;

public class InventoryToggle : MonoBehaviour
{
    public GameObject inventoryPanel;

    [Header("UI References")]
    public GameObject crosshair;

    private PlayerMovement playerMovement;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
        }
    }

    void Update()
    {
        // PAUSE DETECTOR
        if (Time.timeScale == 0f)
        {
            if (inventoryPanel != null && inventoryPanel.activeSelf)
            {
                CloseInventory();
            }

            return;
        }

        // Inventory toggle key: I
        if (GameInput.Pressed("Inventory", KeyCode.I))
        {
            if (inventoryPanel != null)
            {
                bool isActive = !inventoryPanel.activeSelf;

                if (isActive)
                {
                    OpenInventory();
                }
                else
                {
                    CloseInventory();
                }
            }
        }
    }

    // DRAG & DROP COEXISTENCE
    void LateUpdate()
    {
        if (inventoryPanel != null &&
            inventoryPanel.activeSelf &&
            Time.timeScale != 0f)
        {
            if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }

    private void OpenInventory()
    {
        inventoryPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (playerMovement != null)
            playerMovement.canRotateMouse = false;

        if (crosshair != null)
            crosshair.SetActive(false);
    }

    public void CloseInventory()
    {
        inventoryPanel.SetActive(false);

        if (Time.timeScale != 0f)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (crosshair != null)
                crosshair.SetActive(true);
        }

        if (playerMovement != null)
            playerMovement.canRotateMouse = true;
    }
}