using UnityEngine;
using UnityEngine.InputSystem;

public class HUDPanelToggle : MonoBehaviour
{
    // Read by GameplayControlFeedback so it doesn't re-show the panel
    // while the player has hidden it with Tab.
    public static bool ControlPanelHiddenByUser { get; private set; }

    [Header("Panels")]
    public GameObject activeTasksPanel;
    public GameObject controlPanel;

    [Header("Options")]
    [Tooltip("Key that shows/hides the control panel.")]
    public Key toggleKey = Key.Tab;

    [Tooltip("Start with the control panel hidden.")]
    public bool startHidden = false;

    private void Start()
    {
        ControlPanelHiddenByUser = startHidden;

        ApplyPanels();
    }

    private void OnDestroy()
    {
        ControlPanelHiddenByUser = false;
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            ControlPanelHiddenByUser = !ControlPanelHiddenByUser;
            ApplyPanels();
        }
    }

    private void ApplyPanels()
    {
        if (activeTasksPanel != null)
            activeTasksPanel.SetActive(!ControlPanelHiddenByUser);

        if (controlPanel != null)
            controlPanel.SetActive(!ControlPanelHiddenByUser);
    }
}
