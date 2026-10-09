using UnityEngine;
using TMPro; // Use 'using UnityEngine.UI;' instead if you are using standard legacy Text

public class UIDescriptionPanel : MonoBehaviour
{
    private static UIDescriptionPanel instance;

    // The panel is hidden with SetActive(false), so Awake may never run if it
    // starts inactive in the scene. Look it up (including inactive) on demand.
    public static UIDescriptionPanel Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<UIDescriptionPanel>(
                    FindObjectsInactive.Include);
            }

            return instance;
        }
        private set { instance = value; }
    }

    [Header("UI Text References")]
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemDescriptionText;

    private void Awake()
    {
        if (instance == null) instance = this;
        else if (instance != this) { Destroy(gameObject); return; }

        Hide(); // Start the game with the panel hidden
    }

    public void Show(string name, string description)
    {
        gameObject.SetActive(true);
        itemNameText.text = name;
        itemDescriptionText.text = description;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}