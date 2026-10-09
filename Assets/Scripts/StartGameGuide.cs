using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Shows a short sequence of guide messages when the game scene starts.
/// Spawns itself automatically in the gameplay scene (no scene setup needed).
/// Press Enter to skip to the next message, or Tab to skip the whole guide.
/// </summary>
public class StartGameGuide : MonoBehaviour
{
    private const string GameSceneName = "Forest";

    [TextArea(2, 4)]
    public string[] messages =
    {
        "Welcome to Ligaw.\nYou must survive in the forest.",
        "Move with W A S D and look around with the mouse.\nHold Shift to run.",
        "Day 1 goal: gather 3 Apples, 5 Logs and 3 Stones.\nPress E or Left Click to pick things up.",
        "Press I to open your inventory and C to craft.\nUse the number keys 1-9 to use your hotbar.",
        "Watch your health, hunger and thirst.\nStay near a campfire at night. Good luck!"
    };

    public float startDelay = 1.5f;
    public float fadeTime = 0.6f;
    public float holdTime = 5f;

    private CanvasGroup group;
    private TextMeshProUGUI label;
    private TextMeshProUGUI hint;
    private bool next;
    private bool skipAll;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (SceneManager.GetActiveScene().name != GameSceneName)
            return;

        if (FindFirstObjectByType<StartGameGuide>() != null)
            return;

        new GameObject("StartGameGuide").AddComponent<StartGameGuide>();
    }

    private void Start()
    {
        BuildUI();
        StartCoroutine(RunGuide());
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.enterKey.wasPressedThisFrame)
            next = true;

        if (Keyboard.current.tabKey.wasPressedThisFrame)
            skipAll = true;
    }

    private IEnumerator RunGuide()
    {
        yield return new WaitForSecondsRealtime(startDelay);

        foreach (string msg in messages)
        {
            if (skipAll)
                break;

            label.text = msg;
            next = false;

            yield return Fade(0f, 1f);

            float t = 0f;
            while (t < holdTime && !next && !skipAll)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            yield return Fade(1f, 0f);
        }

        Destroy(gameObject);
    }

    private IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, t / fadeTime);
            yield return null;
        }
        group.alpha = to;
    }

    private void BuildUI()
    {
        GameObject canvasObj = new GameObject("GuideCanvas");
        canvasObj.transform.SetParent(transform, false);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        group = canvasObj.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        // Dark strip near the bottom for readability
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(canvasObj.transform, false);
        var img = panel.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0f, 0f, 0f, 0.55f);
        var prt = img.rectTransform;
        prt.anchorMin = new Vector2(0.5f, 0f);
        prt.anchorMax = new Vector2(0.5f, 0f);
        prt.pivot = new Vector2(0.5f, 0f);
        prt.anchoredPosition = new Vector2(0f, 120f);
        prt.sizeDelta = new Vector2(1200f, 190f);

        label = CreateText("Message", panel.transform, 40, new Vector2(0f, 20f), new Vector2(1120f, 130f));
        hint = CreateText("Hint", panel.transform, 22, new Vector2(0f, -70f), new Vector2(1120f, 40f));
        hint.text = "Enter: next   |   Tab: skip guide";
        hint.color = new Color(1f, 1f, 1f, 0.6f);
    }

    private TextMeshProUGUI CreateText(string objName, Transform parent, float size, Vector2 pos, Vector2 dims)
    {
        GameObject go = new GameObject(objName);
        go.transform.SetParent(parent, false);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        var rt = tmp.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = dims;
        return tmp;
    }
}
