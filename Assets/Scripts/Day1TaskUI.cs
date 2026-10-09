using UnityEngine;
using TMPro;

public class Day1TaskUI : MonoBehaviour
{
    public static Day1TaskUI Instance;

    [Header("Task UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI applesText;
    public TextMeshProUGUI logsText;
    public TextMeshProUGUI stonesText;
    public TextMeshProUGUI completionText; // Optional overlay text when complete

    [Header("Colors")]
    public Color pendingColor = Color.white;
    public Color completedColor = Color.green;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (completionText != null)
            completionText.gameObject.SetActive(false);
    }

    public void UpdateUI(int currentApples, int applesNeeded,
                         int currentLogs, int logsNeeded,
                         int currentStones, int stonesNeeded)
    {
        // 1. Update Apples Task Text
        bool applesDone = currentApples >= applesNeeded;
        string appleCheck = applesDone ? "[✓]" : "[ ]";
        applesText.text = $"{appleCheck} Gather Apples: {currentApples}/{applesNeeded}";
        applesText.color = applesDone ? completedColor : pendingColor;

        // 2. Update Logs Task Text
        bool logsDone = currentLogs >= logsNeeded;
        string logCheck = logsDone ? "[✓]" : "[ ]";
        logsText.text = $"{logCheck} Gather Logs: {currentLogs}/{logsNeeded}";
        logsText.color = logsDone ? completedColor : pendingColor;

        // 3. Update Stones Task Text
        bool stonesDone = currentStones >= stonesNeeded;
        string stoneCheck = stonesDone ? "[✓]" : "[ ]";
        stonesText.text = $"{stoneCheck} Gather Stones: {currentStones}/{stonesNeeded}";
        stonesText.color = stonesDone ? completedColor : pendingColor;
    }

    public void ShowDayCompleteUI()
    {
        if (completionText != null)
        {
            completionText.gameObject.SetActive(true);
            completionText.text = "🎉 DAY 1 COMPLETED!";
        }
    }
}