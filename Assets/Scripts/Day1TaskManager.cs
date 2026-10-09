using UnityEngine;
using System.Collections.Generic;

public class Day1TaskManager : MonoBehaviour
{
    public static Day1TaskManager Instance;

    [Header("Day 1 Requirements")]
    public int applesNeeded = 3;
    public int logsNeeded = 5;
    public int stonesNeeded = 3;

    [Header("Current Progress")]
    public int currentApples = 0;
    public int currentLogs = 0;
    public int currentStones = 0;

    // =========================================================
    // TRACK ITEMS ALREADY COUNTED
    // =========================================================

    private HashSet<string> countedWorldPickupIDs =
        new HashSet<string>();

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        RefreshTaskUI();
        CheckDay1Completion();
    }

    // =========================================================
    // ITEM GATHERED
    // =========================================================

    public void OnItemGathered(string itemName, int amount)
    {
        if (string.IsNullOrEmpty(itemName))
            return;

        if (amount <= 0)
            return;

        string cleanName = itemName.ToLower();

        // -----------------------------------------------------
        // IMPORTANT:
        // This version is kept compatible with your existing
        // Gatherable.cs.
        //
        // The actual duplicate protection is handled by
        // OnWorldItemGathered().
        // -----------------------------------------------------

        if (cleanName.Contains("apple"))
        {
            currentApples = Mathf.Min(
                currentApples + amount,
                applesNeeded
            );
        }
        else if (cleanName.Contains("log"))
        {
            currentLogs = Mathf.Min(
                currentLogs + amount,
                logsNeeded
            );
        }
        else if (cleanName.Contains("stone"))
        {
            currentStones = Mathf.Min(
                currentStones + amount,
                stonesNeeded
            );
        }

        RefreshTaskUI();
        CheckDay1Completion();
    }

    // =========================================================
    // WORLD ITEM GATHERED
    // =========================================================
    //
    // Use this method for Gatherable world objects.
    //
    // Example:
    //
    // Forest_Log_001
    // Forest_Log_002
    // Forest_Rock_001
    //
    // Once one of these IDs has been counted, picking up the
    // dropped version later will NOT increase the task counter.
    // =========================================================

    public void OnWorldItemGathered(
        string worldPickupID,
        string itemName,
        int amount)
    {
        if (string.IsNullOrEmpty(worldPickupID))
        {
            Debug.LogWarning(
                "[Day1TaskManager] World item has no ID. " +
                "Task progress was not counted."
            );

            return;
        }

        // -----------------------------------------------------
        // Already counted?
        // -----------------------------------------------------

        if (countedWorldPickupIDs.Contains(worldPickupID))
        {
            Debug.Log(
                "[Day1TaskManager] Ignoring duplicate world item: " +
                worldPickupID
            );

            return;
        }

        // -----------------------------------------------------
        // Mark as counted BEFORE increasing progress.
        // -----------------------------------------------------

        countedWorldPickupIDs.Add(worldPickupID);

        // -----------------------------------------------------
        // Count the item.
        // -----------------------------------------------------

        OnItemGathered(
            itemName,
            amount
        );
    }

    // =========================================================
    // CHECK IF WORLD ITEM WAS ALREADY COUNTED
    // =========================================================

    public bool WasWorldItemCounted(string worldPickupID)
    {
        if (string.IsNullOrEmpty(worldPickupID))
            return false;

        return countedWorldPickupIDs.Contains(
            worldPickupID
        );
    }

    // =========================================================
    // SAVE / LOAD SUPPORT
    // =========================================================

    public List<string> GetCountedWorldPickupIDs()
    {
        return new List<string>(countedWorldPickupIDs);
    }

    public void LoadProgress(
        int apples,
        int logs,
        int stones,
        List<string> countedIDs)
    {
        currentApples = Mathf.Clamp(apples, 0, applesNeeded);
        currentLogs = Mathf.Clamp(logs, 0, logsNeeded);
        currentStones = Mathf.Clamp(stones, 0, stonesNeeded);

        countedWorldPickupIDs.Clear();

        if (countedIDs != null)
        {
            foreach (string id in countedIDs)
            {
                if (!string.IsNullOrEmpty(id))
                    countedWorldPickupIDs.Add(id);
            }
        }

        RefreshTaskUI();
        CheckDay1Completion();
    }

    // =========================================================
    // RESET TASK PROGRESS
    // =========================================================

    public void ResetTaskProgress()
    {
        currentApples = 0;
        currentLogs = 0;
        currentStones = 0;

        countedWorldPickupIDs.Clear();

        RefreshTaskUI();
        CheckDay1Completion();

        Debug.Log(
            "[Day1TaskManager] Day 1 task progress reset."
        );
    }

    // =========================================================
    // UI
    // =========================================================

    private void RefreshTaskUI()
    {
        if (Day1TaskUI.Instance != null)
        {
            Day1TaskUI.Instance.UpdateUI(
                currentApples,
                applesNeeded,

                currentLogs,
                logsNeeded,

                currentStones,
                stonesNeeded
            );
        }
    }

    // =========================================================
    // COMPLETION
    // =========================================================

    public void CheckDay1Completion()
    {
        if (
            currentApples >= applesNeeded &&
            currentLogs >= logsNeeded &&
            currentStones >= stonesNeeded
        )
        {
            Debug.Log(
                "DAY 1 TASKS COMPLETE!"
            );

            if (Day1TaskUI.Instance != null)
            {
                Day1TaskUI.Instance.ShowDayCompleteUI();
            }
        }
    }
}