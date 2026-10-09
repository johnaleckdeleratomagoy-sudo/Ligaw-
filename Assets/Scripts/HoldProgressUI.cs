using UnityEngine;
using UnityEngine.UI;

public class HoldProgressUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Image fillImage;        // Assign ChopCircleUI here
    [SerializeField] private CanvasGroup canvasGroup; // For smooth fading in/out

    [Header("Settings")]
    [SerializeField] private Vector2 cursorOffset = new Vector2(25f, -25f);
    [SerializeField] private float fadeSpeed = 12f;

    private Canvas parentCanvas;
    private RectTransform rectTransform;
    private bool isHolding = false;
    private float targetFill = 0f;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0f;
    }

    void Update()
    {
        // Smoothly fade canvas group
        float targetAlpha = isHolding ? 1f : 0f;
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);

        if (canvasGroup.alpha > 0.01f)
        {
            // Position circle next to screen cursor
            Vector2 mousePos = Input.mousePosition;
            rectTransform.position = mousePos + cursorOffset;

            // Animate fill progress smoothly
            if (fillImage != null)
            {
                fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFill, Time.deltaTime * 15f);
            }
        }
    }

    public void ShowUI(float progressNormalized)
    {
        isHolding = true;
        targetFill = Mathf.Clamp01(progressNormalized);
    }

    public void HideUI()
    {
        isHolding = false;
        targetFill = 0f;
        if (fillImage != null) fillImage.fillAmount = 0f;
    }
}