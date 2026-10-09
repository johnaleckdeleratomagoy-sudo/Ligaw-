using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SettingsMenuController : MonoBehaviour
{
    [Header("Settings Pages")]
    public GameObject audioPanel;
    public GameObject controlPanel;
    public GameObject sensitivityPanel;
    public GameObject graphicsPanel;

    [Header("Other Menu Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingMenu;

    private void Start()
    {
        // Buttons/panels are matched by name (e.g. "SensitivityButton" + "Sensitivitypanel",
        // "GraphicsButton" + "Graphics panel"), so no inspector wiring is needed.
        // No main menu panel assigned means this copy lives in the pause menu (Forest scene).
        AudioSettingsUI audioUI = GetComponentInChildren<AudioSettingsUI>(true);
        if (audioUI != null && mainMenuPanel == null)
            audioUI.openedFromPauseMenu = true;

        WireTab("sensitiv", ref sensitivityPanel, ShowSensitivity);
        WireTab("graphic", ref graphicsPanel, ShowGraphics);

        BuildGraphicsUI();
        ShowAudio();
    }

    // Finds a button whose name contains `key` and the page panel whose name contains `key` + "panel",
    // then makes the button show that page.
    private void WireTab(string key, ref GameObject panel, UnityAction show)
    {
        Button button = null;

        foreach (Button b in GetComponentsInChildren<Button>(true))
        {
            if (b.name.ToLowerInvariant().Contains(key))
            {
                button = b;
                break;
            }
        }

        if (panel == null)
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLowerInvariant();
                if (n.Contains(key) && n.Contains("panel") && t.GetComponent<Button>() == null)
                {
                    panel = t.gameObject;
                    break;
                }
            }
        }

        if (button == null || panel == null)
            return;

        // The button may have been duplicated from another one; ignore its inherited OnClick calls.
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);

        button.onClick.AddListener(show);
    }

    private void BuildGraphicsUI()
    {
        if (graphicsPanel == null)
            return;

        // Positioned inside the wooden frame (fractions of the canvas), styled like the Audio labels.
        Canvas canvas = graphicsPanel.GetComponentInParent<Canvas>();
        RectTransform reference = canvas != null ? (RectTransform)canvas.rootCanvas.transform : null;

        TMP_Text style = null;
        if (audioPanel != null)
        {
            foreach (TMP_Text t in audioPanel.GetComponentsInChildren<TMP_Text>(true))
            {
                if (t.text != null && t.text.ToUpperInvariant().Contains("VOLUME"))
                {
                    style = t;
                    break;
                }
            }
        }

        GraphicsSettingsBuilder.Build(graphicsPanel, reference, style);
    }

    public void ShowAudio()
    {
        SetPages(audio: true, control: false, sensitivity: false, graphics: false);
    }

    public void ShowControls()
    {
        SetPages(audio: false, control: true, sensitivity: false, graphics: false);
    }

    public void ShowSensitivity()
    {
        SetPages(audio: false, control: false, sensitivity: true, graphics: false);
    }

    public void ShowGraphics()
    {
        SetPages(audio: false, control: false, sensitivity: false, graphics: true);
    }

    private void SetPages(bool audio, bool control, bool sensitivity, bool graphics)
    {
        if (audioPanel != null)
            audioPanel.SetActive(audio);

        if (controlPanel != null)
            controlPanel.SetActive(control);

        if (sensitivityPanel != null)
            sensitivityPanel.SetActive(sensitivity);

        if (graphicsPanel != null)
            graphicsPanel.SetActive(graphics);
    }

    public void BackToMainMenu()
    {
        // In the pause menu, going back returns to the pause panel instead.
        AudioSettingsUI audioUI = GetComponentInChildren<AudioSettingsUI>(true);
        if (audioUI != null && audioUI.openedFromPauseMenu)
        {
            audioUI.BackButton();
            return;
        }

        if (settingMenu != null)
            settingMenu.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }
}
