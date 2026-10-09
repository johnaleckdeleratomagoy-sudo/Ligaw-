using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Builds the Graphics settings UI (resolution, display mode, quality, VSync) inside the
// "Graphics panel" at runtime, and re-applies the saved choices when the game starts.
public static class GraphicsSettingsBuilder
{
    private const string ResKey = "Gfx_Resolution";
    private const string ModeKey = "Gfx_FullScreen";
    private const string QualityKey = "Gfx_Quality";
    private const string VSyncKey = "Gfx_VSync";

    private static readonly Color WoodColor = new Color(0.22f, 0.16f, 0.11f, 1f);
    private static readonly Color BorderColor = new Color(0.45f, 0.42f, 0.34f, 1f);
    private static readonly Color ValueColor = new Color(0.93f, 0.88f, 0.75f, 1f);

    private static List<Resolution> resolutions;

    // ---------------------------------------------------------------
    // Apply saved settings on startup so they carry into the game scene
    // ---------------------------------------------------------------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplySaved()
    {
        if (PlayerPrefs.HasKey(QualityKey))
            QualitySettings.SetQualityLevel(
                Mathf.Clamp(PlayerPrefs.GetInt(QualityKey), 0, QualitySettings.names.Length - 1), true);

        if (PlayerPrefs.HasKey(VSyncKey))
            QualitySettings.vSyncCount = PlayerPrefs.GetInt(VSyncKey) == 1 ? 1 : 0;

        if (PlayerPrefs.HasKey(ResKey) || PlayerPrefs.HasKey(ModeKey))
        {
            List<Resolution> list = GetResolutions();
            int ri = Mathf.Clamp(PlayerPrefs.GetInt(ResKey, list.Count - 1), 0, list.Count - 1);
            bool full = PlayerPrefs.GetInt(ModeKey, 1) == 1;
            SetResolution(list[ri], full);
        }
    }

    // ---------------------------------------------------------------
    // Build UI
    // ---------------------------------------------------------------

    // referenceRect: the rectangle the rows should fill (e.g. the Audio panel's rect).
    // styleSource: any label whose font/colour the row labels should copy.
    public static void Build(GameObject graphicsPanel, RectTransform canvasRect, TMP_Text styleSource)
    {
        if (graphicsPanel == null || graphicsPanel.transform.Find("GraphicsContent") != null)
            return;

        GameObject content = new GameObject("GraphicsContent", typeof(RectTransform));
        RectTransform crt = (RectTransform)content.transform;
        crt.SetParent(graphicsPanel.transform, false);
        FitToFraction(crt, canvasRect != null ? canvasRect : (RectTransform)graphicsPanel.transform,
            FrameInner.xMin, FrameInner.xMax, FrameInner.yMin, FrameInner.yMax);

        List<Resolution> list = GetResolutions();
        string[] resNames = list.ConvertAll(r => r.width + " x " + r.height).ToArray();
        int resIndex = Mathf.Clamp(PlayerPrefs.GetInt(ResKey, FindCurrentResolution(list)), 0, list.Count - 1);

        string[] modes = { "WINDOWED", "FULLSCREEN" };
        int modeIndex = PlayerPrefs.GetInt(ModeKey, Screen.fullScreen ? 1 : 0);

        string[] quality = QualitySettings.names;
        for (int i = 0; i < quality.Length; i++) quality[i] = quality[i].ToUpperInvariant();
        int qualityIndex = Mathf.Clamp(PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel()),
            0, quality.Length - 1);

        string[] onOff = { "OFF", "ON" };
        int vsyncIndex = PlayerPrefs.GetInt(VSyncKey, QualitySettings.vSyncCount > 0 ? 1 : 0);

        // Current choices, applied together whenever one changes.
        int curRes = resIndex, curMode = modeIndex;

        AddRow(content.transform, 0, "RESOLUTION", resNames, resIndex, styleSource, i =>
        {
            curRes = i;
            PlayerPrefs.SetInt(ResKey, i);
            SetResolution(list[curRes], curMode == 1);
        });

        AddRow(content.transform, 1, "DISPLAY MODE", modes, modeIndex, styleSource, i =>
        {
            curMode = i;
            PlayerPrefs.SetInt(ModeKey, i);
            SetResolution(list[curRes], curMode == 1);
        });

        AddRow(content.transform, 2, "QUALITY", quality, qualityIndex, styleSource, i =>
        {
            PlayerPrefs.SetInt(QualityKey, i);
            QualitySettings.SetQualityLevel(i, true);
        });

        AddRow(content.transform, 3, "VSYNC", onOff, vsyncIndex, styleSource, i =>
        {
            PlayerPrefs.SetInt(VSyncKey, i);
            QualitySettings.vSyncCount = i;
        });
    }

    // ---------------------------------------------------------------
    // Rows
    // ---------------------------------------------------------------

    private static void AddRow(Transform parent, int row, string label, string[] options, int start,
        TMP_Text style, Action<int> onChange)
    {
        float rowHeight = 0.20f;
        float top = 0.98f - row * 0.25f;
        float bottom = top - rowHeight;

        GameObject rowObj = new GameObject(label + "Row", typeof(RectTransform));
        rowObj.transform.SetParent(parent, false);
        RectTransform rrt = (RectTransform)rowObj.transform;
        rrt.anchorMin = new Vector2(0.04f, bottom);
        rrt.anchorMax = new Vector2(0.96f, top);
        rrt.offsetMin = rrt.offsetMax = Vector2.zero;

        TextMeshProUGUI lbl = CreateText(rowObj.transform, "Label", label, style, TextAlignmentOptions.Left, false);
        SetAnchors((RectTransform)lbl.transform, 0f, 0.38f);

        int index = Mathf.Clamp(start, 0, options.Length - 1);

        TextMeshProUGUI value = CreateText(rowObj.transform, "Value", options[index], style,
            TextAlignmentOptions.Center, true);
        SetAnchors((RectTransform)value.transform, 0.52f, 0.86f);

        Button left = CreateArrow(rowObj.transform, "<", style, 0.42f, 0.50f);
        Button right = CreateArrow(rowObj.transform, ">", style, 0.88f, 0.96f);

        Action<int> step = delta =>
        {
            index = (index + delta + options.Length) % options.Length;
            value.text = options[index];
            onChange(index);
            PlayerPrefs.Save();
        };

        left.onClick.AddListener(() => step(-1));
        right.onClick.AddListener(() => step(1));
    }

    private static Button CreateArrow(Transform parent, string symbol, TMP_Text style, float xMin, float xMax)
    {
        GameObject obj = new GameObject(symbol == "<" ? "PrevButton" : "NextButton",
            typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        SetAnchors((RectTransform)obj.transform, xMin, xMax);

        obj.GetComponent<Image>().color = WoodColor;

        Outline outline = obj.AddComponent<Outline>();
        outline.effectColor = BorderColor;
        outline.effectDistance = new Vector2(2f, -2f);

        Button button = obj.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1.3f, 1.25f, 1.1f, 1f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        button.colors = colors;

        TextMeshProUGUI t = CreateText(obj.transform, "Text", symbol, style, TextAlignmentOptions.Center, true);
        SetAnchors((RectTransform)t.transform, 0f, 1f);
        return button;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string text, TMP_Text style,
        TextAlignmentOptions align, bool light)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI t = obj.AddComponent<TextMeshProUGUI>();
        if (style != null) t.font = style.font;
        t.text = text;
        t.alignment = align;
        t.color = light || style == null ? ValueColor : style.color;
        t.enableAutoSizing = true;
        t.fontSizeMin = 12;
        t.fontSizeMax = 40;
        t.enableWordWrapping = false;
        t.raycastTarget = false;
        return t;
    }

    private static void SetAnchors(RectTransform rt, float xMin, float xMax)
    {
        rt.anchorMin = new Vector2(xMin, 0f);
        rt.anchorMax = new Vector2(xMax, 1f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // Dark inner area of the wooden frame, as fractions of the whole canvas
    // (below the header bar, above the divider line). Tweak these to move/resize the rows.
    private static readonly Rect FrameInner = Rect.MinMaxRect(0.27f, 0.23f, 0.74f, 0.59f);

    // Make rt cover a fractional part of the canvas rect (0-1 from its bottom-left).
    private static void FitToFraction(RectTransform rt, RectTransform canvas,
        float xMin, float xMax, float yMin, float yMax)
    {
        Vector3[] c = new Vector3[4];
        canvas.GetWorldCorners(c);

        Vector3 min = new Vector3(Mathf.Lerp(c[0].x, c[2].x, xMin), Mathf.Lerp(c[0].y, c[2].y, yMin), c[0].z);
        Vector3 max = new Vector3(Mathf.Lerp(c[0].x, c[2].x, xMax), Mathf.Lerp(c[0].y, c[2].y, yMax), c[0].z);

        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.position = (min + max) * 0.5f;

        Vector3 s = rt.lossyScale;
        rt.sizeDelta = new Vector2((max.x - min.x) / s.x, (max.y - min.y) / s.y);
    }

    // ---------------------------------------------------------------
    // Display helpers
    // ---------------------------------------------------------------

    private static List<Resolution> GetResolutions()
    {
        if (resolutions != null)
            return resolutions;

        resolutions = new List<Resolution>();
        foreach (Resolution r in Screen.resolutions)
        {
            int existing = resolutions.FindIndex(x => x.width == r.width && x.height == r.height);
            if (existing >= 0) resolutions[existing] = r; // keep the highest refresh rate
            else resolutions.Add(r);
        }

        if (resolutions.Count == 0)
            resolutions.Add(new Resolution { width = Screen.width, height = Screen.height });

        return resolutions;
    }

    private static int FindCurrentResolution(List<Resolution> list)
    {
        int found = list.FindIndex(r => r.width == Screen.width && r.height == Screen.height);
        return found >= 0 ? found : list.Count - 1;
    }

    private static void SetResolution(Resolution r, bool fullscreen)
    {
        Screen.SetResolution(r.width, r.height,
            fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
    }
}
