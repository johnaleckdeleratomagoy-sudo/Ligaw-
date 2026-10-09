using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldPickupSave : MonoBehaviour
{
    public static WorldPickupSave Instance { get; private set; }

    [Serializable]
    private class WorldPickupSaveData
    {
        public List<string> collectedIDs = new List<string>();
    }

    private HashSet<string> collectedIDs = new HashSet<string>();

    private string SavePath
    {
        get
        {
            return System.IO.Path.Combine(
                Application.persistentDataPath,
                "world_pickups.json"
            );
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadWorldPickupData();

        Debug.Log(
            "[WorldPickupSave] Initialized. Collected pickups: " +
            collectedIDs.Count
        );
    }

    // =========================================================
    // CHECK IF PICKUP WAS ALREADY COLLECTED
    // =========================================================

    public bool IsCollected(string pickupID)
    {
        if (string.IsNullOrEmpty(pickupID))
            return false;

        return collectedIDs.Contains(pickupID);
    }

    // =========================================================
    // MARK PICKUP AS COLLECTED
    // =========================================================

    public void MarkCollected(string pickupID)
    {
        if (string.IsNullOrEmpty(pickupID))
        {
            Debug.LogWarning(
                "[WorldPickupSave] Cannot save pickup with empty ID."
            );

            return;
        }

        if (collectedIDs.Contains(pickupID))
            return;

        collectedIDs.Add(pickupID);

        SaveWorldPickupData();

        Debug.Log(
            "[WorldPickupSave] Collected pickup saved: " +
            pickupID
        );
    }

    // =========================================================
    // SAVE
    // =========================================================

    public void SaveWorldPickupData()
    {
        try
        {
            WorldPickupSaveData data = new WorldPickupSaveData();

            data.collectedIDs = new List<string>(collectedIDs);

            string json = JsonUtility.ToJson(data, true);

            System.IO.File.WriteAllText(
                SavePath,
                json
            );

            Debug.Log(
                "[WorldPickupSave] World pickup data saved."
            );
        }
        catch (Exception e)
        {
            Debug.LogError(
                "[WorldPickupSave] Save failed: " +
                e.Message
            );
        }
    }

    // =========================================================
    // LOAD
    // =========================================================

    public void LoadWorldPickupData()
    {
        if (!System.IO.File.Exists(SavePath))
        {
            collectedIDs.Clear();

            Debug.Log(
                "[WorldPickupSave] No world pickup save found."
            );

            return;
        }

        try
        {
            string json =
                System.IO.File.ReadAllText(SavePath);

            if (string.IsNullOrEmpty(json))
            {
                collectedIDs.Clear();
                return;
            }

            WorldPickupSaveData data =
                JsonUtility.FromJson<WorldPickupSaveData>(json);

            collectedIDs.Clear();

            if (data != null &&
                data.collectedIDs != null)
            {
                foreach (string id in data.collectedIDs)
                {
                    if (!string.IsNullOrEmpty(id))
                        collectedIDs.Add(id);
                }
            }

            Debug.Log(
                "[WorldPickupSave] Loaded " +
                collectedIDs.Count +
                " collected world pickups."
            );
        }
        catch (Exception e)
        {
            Debug.LogError(
                "[WorldPickupSave] Load failed: " +
                e.Message
            );

            collectedIDs.Clear();
        }
    }

    // =========================================================
    // SAVE GAME SUPPORT
    // =========================================================

    public List<string> GetCollectedIDs()
    {
        return new List<string>(collectedIDs);
    }

    public void ReplaceCollectedIDs(List<string> ids)
    {
        collectedIDs.Clear();

        if (ids != null)
        {
            foreach (string id in ids)
            {
                if (!string.IsNullOrEmpty(id))
                    collectedIDs.Add(id);
            }
        }

        SaveWorldPickupData();
    }

    // =========================================================
    // CLEAR ALL
    // =========================================================

    public void ClearAllWorldPickups()
    {
        collectedIDs.Clear();

        try
        {
            if (System.IO.File.Exists(SavePath))
                System.IO.File.Delete(SavePath);
        }
        catch (Exception e)
        {
            Debug.LogError(
                "[WorldPickupSave] Could not delete world pickup save: " +
                e.Message
            );
        }

        Debug.Log(
            "[WorldPickupSave] ALL world pickup data cleared."
        );
    }

    // =========================================================
    // DEBUG
    // =========================================================

    public int GetCollectedCount()
    {
        return collectedIDs.Count;
    }
}