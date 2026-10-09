using UnityEngine;

public class SavePanelCloseButton : MonoBehaviour
{
    [Header("Save Panel")]
    [SerializeField] private GameObject saveSystemPanel;

    public void ClosePanel()
    {
        Debug.Log(
            "[SavePanelCloseButton] Close button clicked."
        );

        // Close the panel directly
        if (saveSystemPanel != null)
        {
            saveSystemPanel.SetActive(false);

            Debug.Log(
                "[SavePanelCloseButton] SaveSystemPanel closed."
            );
        }
        else
        {
            Debug.LogWarning(
                "[SavePanelCloseButton] SaveSystemPanel reference is missing."
            );
        }

        // Resume the game
        Time.timeScale = 1f;

        // Restore FPS mouse control
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log(
            "[SavePanelCloseButton] Game resumed."
        );
    }
}