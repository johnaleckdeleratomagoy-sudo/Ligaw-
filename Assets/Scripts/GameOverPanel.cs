using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds the Game Over panel at runtime from the art in Resources/GameOver
/// (frame with "GAME OVER" title, plus Retry and Back to Main Menu signs).
/// Call Show() when the player dies.
/// </summary>
public class GameOverPanel : MonoBehaviour
{
    [Header("Sprites (loaded from Resources/GameOver if left empty)")]
    public Sprite frameSprite;
    public Sprite retrySprite;
    public Sprite mainMenuSprite;

    [Header("Scene Panel (optional - found by name if empty)")]
    public GameObject scenePanel;
    public string scenePanelName = "GameOverPanel";
    public string retryButtonName = "RetryBt";
    public string mainMenuButtonName = "BackToMainBT";

    [Header("Text")]
    public string messageText = "YOU HAVE PERISHED\nIN THE WILDERNESS";
    public string subText = "The forest claims another soul...";
    public TMP_FontAsset font;
    public Color textColor = new Color(0.88f, 0.83f, 0.72f, 1f);
    public float messageSize = 72f;
    public float subTextSize = 34f;
    public float messageY = 70f;  // offset from panel center
    public float subTextY = -90f;

    [Header("Scenes")]
    public string mainMenuSceneName = "MainMenu 1";

    [Header("Layout (reference 1920x1080)")]
    public float panelWidth = 1100f;
    public Vector2 buttonSize = new Vector2(330f, 110f);
    [Range(0f, 0.5f)] public float buttonBottomOffset = 0.12f; // fraction of panel height
    public float buttonSpacing = 420f;

    [Header("Timing")]
    [Tooltip("Used when no Animator is given, and as extra pause after the death animation ends")]
    public float showDelay = 0.5f;
    public string deathStateName = "Die";
    [Tooltip("Safety net: show the panel anyway after this many seconds")]
    public float maxWaitForAnimation = 8f;
    public float fadeDuration = 0.6f;

    static readonly Color Dim = new Color(0f, 0f, 0f, 0.6f);

    GameObject root;
    CanvasGroup group;
    bool shown;

    public void Show(Animator deathAnimator = null)
    {
        if (shown) return;
        shown = true;
        StartCoroutine(ShowRoutine(deathAnimator));
    }

    IEnumerator ShowRoutine(Animator anim)
    {
        if (anim != null) yield return WaitForDeathAnimation(anim);
        yield return new WaitForSeconds(showDelay);

        EnsureEventSystem();
        if (!UseScenePanel()) Build();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
        {
            group.alpha = t / fadeDuration;
            yield return null;
        }
        group.alpha = 1f;
        Time.timeScale = 0f;
    }

    // Waits until the death state is playing, then until it has played through once.
    IEnumerator WaitForDeathAnimation(Animator anim)
    {
        float timeout = Time.time + maxWaitForAnimation;

        // 1. wait for the transition into the death state
        while (Time.time < timeout && !InDeathState(anim, out _))
            yield return null;

        // 2. wait for it to finish (normalizedTime reaches 1 = one full play)
        while (Time.time < timeout && InDeathState(anim, out var info) && info.normalizedTime < 1f)
            yield return null;
    }

    bool InDeathState(Animator anim, out AnimatorStateInfo info)
    {
        info = default;
        if (anim == null) return false;
        for (int layer = 0; layer < anim.layerCount; layer++)
        {
            info = anim.GetCurrentAnimatorStateInfo(layer);
            if (info.IsName(deathStateName) && !anim.IsInTransition(layer)) return true;
        }
        return false;
    }

    public void Retry()
    {
        // Player and Canvas persist across scenes, so revive in place instead of reloading
        ClosePanel();
        Time.timeScale = 1f;
        shown = false;

        var health = FindFirstObjectByType<PlayerHealth>();
        if (health != null) health.Revive();
    }

    void ClosePanel()
    {
        if (root != null) Destroy(root);          // runtime fallback panel
        if (scenePanel != null) scenePanel.SetActive(false);
    }

    /// <summary>Hides the scene's GameOverPanel (call at gameplay start so it never shows by accident).</summary>
    public static void HideScenePanel(string panelName = "GameOverPanel")
    {
        foreach (var rt in Resources.FindObjectsOfTypeAll<RectTransform>())
        {
            if (rt.name == panelName && rt.gameObject.scene.IsValid())
            {
                rt.gameObject.SetActive(false);
                return;
            }
        }
    }

    // Gameplay objects marked DontDestroyOnLoad would otherwise follow us into the menu.
    static void DestroyGameplayObjects()
    {
        DestroyAll<PersistentUI>();
        DestroyAll<PauseMenu>();
        DestroyAll<WorldPickupSave>();
        DestroyAll<PlayerMovement>();
        DestroyAll<CameraSingleton>();
    }

    static void DestroyAll<T>() where T : Component
    {
        foreach (var c in FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Destroy(c.gameObject);
    }

    public void BackToMainMenu()
    {
        Time.timeScale = 1f;
        ClosePanel();
        DestroyGameplayObjects();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // ---------------------------------------------------------------- Scene panel

    bool UseScenePanel()
    {
        if (scenePanel == null)
        {
            // includes inactive objects; skips prefab assets
            foreach (var rt in Resources.FindObjectsOfTypeAll<RectTransform>())
            {
                if (rt.name == scenePanelName && rt.gameObject.scene.IsValid())
                {
                    scenePanel = rt.gameObject;
                    break;
                }
            }
        }
        if (scenePanel == null) return false;

        scenePanel.SetActive(true);
        scenePanel.transform.SetAsLastSibling();

        group = scenePanel.GetComponent<CanvasGroup>();
        if (group == null) group = scenePanel.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        WireButton(retryButtonName, Retry);
        WireButton(mainMenuButtonName, BackToMainMenu);

        var panelRt = (RectTransform)scenePanel.transform;
        AddText(panelRt, "GameOverMessage", messageText, messageSize, FontStyles.Bold, messageY, 220f);
        AddText(panelRt, "GameOverSubText", subText, subTextSize, FontStyles.Italic, subTextY, 80f);
        return true;
    }

    void WireButton(string objectName, UnityEngine.Events.UnityAction action)
    {
        var t = FindDeep(scenePanel.transform, objectName);
        if (t == null) { Debug.LogWarning("GameOverPanel: button '" + objectName + "' not found"); return; }

        var button = t.GetComponent<Button>();
        if (button == null) button = t.gameObject.AddComponent<Button>();
        button.onClick.RemoveAllListeners();
        // also switch off any OnClick events set in the Inspector
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            button.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
        button.onClick.AddListener(action);
    }

    static Transform FindDeep(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }

    void AddText(RectTransform parent, string name, string content, float size, FontStyles style, float y, float height)
    {
        if (string.IsNullOrEmpty(content)) return;

        var existing = FindDeep(parent, name);
        var go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.1f, 0.5f);
        rt.anchorMax = new Vector2(0.9f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(0f, height);
        rt.anchoredPosition = new Vector2(0f, y);

        var t = go.GetComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = content;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = textColor;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;

        // keep the text behind the buttons
        go.transform.SetAsFirstSibling();
    }

    // ---------------------------------------------------------------- Runtime fallback

    void Build()
    {
        if (frameSprite == null) frameSprite = LoadSprite("GameOver/GameOverFrame");
        if (retrySprite == null) retrySprite = LoadSprite("GameOver/RetrySign");
        if (mainMenuSprite == null) mainMenuSprite = LoadSprite("GameOver/MainMenuSign");

        root = new GameObject("GameOverCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        group = root.GetComponent<CanvasGroup>();
        group.alpha = 0f;

        // Full-screen dimmer (also blocks clicks behind the panel)
        var dim = MakeImage("Dim", root.transform, Dim, null);
        Stretch(dim.rectTransform);

        // Frame keeps its own aspect ratio
        float aspect = frameSprite != null ? frameSprite.rect.height / frameSprite.rect.width : 0.554f;
        var frame = MakeImage("Frame", root.transform, Color.white, frameSprite);
        var fr = frame.rectTransform;
        fr.anchorMin = fr.anchorMax = fr.pivot = new Vector2(0.5f, 0.5f);
        fr.sizeDelta = new Vector2(panelWidth, panelWidth * aspect);
        fr.anchoredPosition = Vector2.zero;

        AddText(fr, "GameOverMessage", messageText, messageSize, FontStyles.Bold, messageY, 220f);
        AddText(fr, "GameOverSubText", subText, subTextSize, FontStyles.Italic, subTextY, 80f);

        float y = -fr.sizeDelta.y * (0.5f - buttonBottomOffset);
        MakeButton(frame.transform, "RetryButton", retrySprite, new Vector2(-buttonSpacing * 0.5f, y), Retry);
        MakeButton(frame.transform, "MainMenuButton", mainMenuSprite, new Vector2(buttonSpacing * 0.5f, y), BackToMainMenu);
    }

    void MakeButton(Transform parent, string name, Sprite sprite, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var img = MakeImage(name, parent, Color.white, sprite);
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = buttonSize;
        rt.anchoredPosition = pos;

        var button = img.gameObject.AddComponent<Button>();
        button.targetGraphic = img;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.25f, 1.2f, 1.1f, 1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.AddListener(onClick);
    }

    static Image MakeImage(string name, Transform parent, Color color, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.preserveAspect = sprite != null;
        return img;
    }

    // Works whether the PNG imported as a Sprite or as a plain Texture2D.
    static Sprite LoadSprite(string path)
    {
        var sprite = Resources.Load<Sprite>(path);
        if (sprite != null) return sprite;

        var tex = Resources.Load<Texture2D>(path);
        if (tex == null)
        {
            Debug.LogWarning("GameOverPanel: missing Resources/" + path);
            return null;
        }
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }
}
