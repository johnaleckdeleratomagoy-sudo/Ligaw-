using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Adds a "How to Play" button and panel to the main menu at runtime,
// so no scene editing is required.
public class HowToPlayMenu : MonoBehaviour
{
    private const string MenuSceneName = "MainMenu 1";

    private GameObject panel;

    private const string HowToPlayText =
        "<b>GOAL</b>\n" +
        "Survive in the forest. On Day 1 gather <b>3 apples, 5 logs and 3 stones</b>.\n\n" +
        "<b>CONTROLS</b>\n" +
        "W A S D  -  Move\n" +
        "Mouse  -  Look / aim\n" +
        "Space  -  Jump\n" +
        "Shift  -  Sprint\n" +
        "Ctrl  -  Crouch\n" +
        "V  -  Switch camera view\n" +
        "E  -  Pick up / interact / hold to light campfire\n" +
        "Left Mouse  -  Chop, attack, cast and reel fishing line\n" +
        "G  -  Drop item\n" +
        "1 - 9  -  Select hotbar slot\n" +
        "I  -  Inventory\n" +
        "C  -  Crafting\n" +
        "R  -  Rotate placed item\n" +
        "Tab  -  Show / hide control hints\n" +
        "Esc  -  Pause, save, settings\n\n" +
        "<b>TIPS</b>\n" +
        "Keep Health, Stamina, Hunger and Thirst up - eat and drink often.\n" +
        "Craft tools at the workbench and light a campfire for safety.\n" +
        "Watch out for bears and boars.";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (SceneManager.GetActiveScene().name != MenuSceneName)
            return;

        if (FindFirstObjectByType<HowToPlayMenu>() != null)
            return;

        new GameObject("HowToPlayMenu").AddComponent<HowToPlayMenu>();
    }

    private void Start()
    {
        EnsureEventSystem();

        GameObject canvasObj = new GameObject("HowToPlayCanvas");
        canvasObj.transform.SetParent(transform, false);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        CreatePanel(canvasObj.transform);

        // Prefer the button already in the main menu list; only create
        // our own corner button if the scene doesn't have one.
        Button existing = FindExistingButton();
        if (existing != null)
        {
            // The scene button may have been duplicated from another one (e.g. Quit),
            // so switch off any inspector-assigned OnClick calls it inherited.
            for (int i = 0; i < existing.onClick.GetPersistentEventCount(); i++)
                existing.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);

            existing.onClick.AddListener(() => panel.SetActive(true));
        }
        else
            CreateOpenButton(canvasObj.transform);
    }

    // Matches a scene button named e.g. "HowToPlayBT", "How To Play", "HOWTOPLAY_Button".
    private Button FindExistingButton()
    {
        foreach (Button b in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string n = b.name.Replace(" ", "").Replace("_", "").ToLowerInvariant();
            if (n.Contains("howtoplay") && b.transform.root != transform.root)
                return b;
        }
        return null;
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }

    private void CreateOpenButton(Transform parent)
    {
        Button button = CreateButton(parent, "HowToPlayButton", "HOW TO PLAY",
            new Vector2(260, 60), 28);

        RectTransform rt = (RectTransform)button.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1, 0);
        rt.anchoredPosition = new Vector2(-40, 40);

        button.onClick.AddListener(() => panel.SetActive(true));
    }

    // Looks up art already used by the settings screens so the panel matches them.
    private static Sprite FindSprite(string objectName)
    {
        foreach (Image img in FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (img.name.Trim() == objectName && img.sprite != null)
                return img.sprite;
        }
        return null;
    }

    private static TMP_FontAsset FindMenuFont()
    {
        foreach (TMP_Text t in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.font != null && t.font.name.Contains("Snow Kei"))
                return t.font;
        }
        return null;
    }

    // Widen the wooden frame (1 = original width). Text area grows with it.
    private const float FrameWidthScale = 1.5f;
    private const float FrameCenterX = 0.503f;
    private const float FrameHalfWidth = 0.166f;

    private static readonly Color CreamText = new Color(0.93f, 0.88f, 0.75f, 1f);

    private void CreatePanel(Transform parent)
    {
        Sprite frameSprite = FindSprite("ControlsPanel");
        Sprite backSprite = FindSprite("BackBT");
        TMP_FontAsset font = FindMenuFont();

        // Full-screen wooden-frame background (same art as the Controls/Audio screens).
        panel = new GameObject("HowToPlayPanel", typeof(RectTransform));
        panel.transform.SetParent(parent, false);

        RectTransform prt = (RectTransform)panel.transform;
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.offsetMin = prt.offsetMax = Vector2.zero;

        // Only the frame art is widened; text and button stay at normal scale so they stay crisp.
        GameObject frame = new GameObject("Frame", typeof(RectTransform), typeof(Image));
        frame.transform.SetParent(panel.transform, false);
        RectTransform frt = (RectTransform)frame.transform;
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = frt.offsetMax = Vector2.zero;
        frt.pivot = new Vector2(FrameCenterX, 0.5f);
        frt.localScale = new Vector3(FrameWidthScale, 1f, 1f);

        // Horizontal extent of the widened frame (screen fractions).
        float half = FrameHalfWidth * FrameWidthScale;
        float left = FrameCenterX - half;
        float right = FrameCenterX + half;
        float pad = 0.02f;

        Image bg = frame.GetComponent<Image>();
        if (frameSprite != null)
        {
            bg.sprite = frameSprite;
            bg.color = Color.white;
        }
        else
        {
            bg.color = new Color(0f, 0f, 0f, 0.92f);
        }

        // Title goes in the frame's header bar.
        TextMeshProUGUI title = CreateText(panel.transform, "Title", "HOW TO PLAY", font, 44,
            TextAlignmentOptions.Center);
        RectTransform trt = (RectTransform)title.transform;
        trt.anchorMin = new Vector2(left + pad, 0.765f);
        trt.anchorMax = new Vector2(right - pad, 0.815f);
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        title.enableAutoSizing = true;
        title.fontSizeMin = 20;
        title.fontSizeMax = 56;

        // Body text inside the frame; auto-sizes to fit.
        TextMeshProUGUI body = CreateText(panel.transform, "Body", HowToPlayText, font, 26,
            TextAlignmentOptions.TopLeft);
        RectTransform brt = (RectTransform)body.transform;
        brt.anchorMin = new Vector2(left + pad, 0.12f);
        brt.anchorMax = new Vector2(right - pad, 0.745f);
        brt.offsetMin = brt.offsetMax = Vector2.zero;
        body.enableAutoSizing = true;
        body.fontSizeMin = 12;
        body.fontSizeMax = 40;
        body.overflowMode = TextOverflowModes.Truncate;

        // BACK button bottom-left, like the settings screens.
        Button close = CreateButton(panel.transform, "CloseButton", "BACK",
            new Vector2(240, 70), 30, backSprite, font);
        RectTransform crt = (RectTransform)close.transform;
        crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0, 0);
        crt.anchoredPosition = new Vector2(60, 70);
        close.onClick.AddListener(() => panel.SetActive(false));

        panel.SetActive(false);
    }

    private static TextMeshProUGUI CreateText(Transform parent, string objName, string content,
        TMP_FontAsset font, float fontSize, TextAlignmentOptions align)
    {
        GameObject obj = new GameObject(objName, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI t = obj.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = content;
        t.fontSize = fontSize;
        t.alignment = align;
        t.color = CreamText;
        t.enableWordWrapping = true;
        t.raycastTarget = false;
        return t;
    }

    private Button CreateButton(Transform parent, string objName, string label,
        Vector2 size, float fontSize, Sprite sprite = null, TMP_FontAsset font = null)
    {
        GameObject obj = new GameObject(objName, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        ((RectTransform)obj.transform).sizeDelta = size;

        Image img = obj.GetComponent<Image>();
        Button button = obj.GetComponent<Button>();
        ColorBlock colors = button.colors;

        if (sprite != null)
        {
            // Wooden button art already contains the label, so no text on top.
            img.sprite = sprite;
            img.color = Color.white;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            button.colors = colors;
            return button;
        }

        img.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);
        colors.highlightedColor = new Color(0.35f, 0.55f, 0.35f, 1f);
        colors.pressedColor = new Color(0.25f, 0.4f, 0.25f, 1f);
        button.colors = colors;

        TextMeshProUGUI text = CreateText(obj.transform, "Text", label, font, fontSize,
            TextAlignmentOptions.Center);
        text.color = Color.white;
        RectTransform rt = (RectTransform)text.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        return button;
    }
}
