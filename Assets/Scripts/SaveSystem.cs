using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }

    [Header("Save Settings")]
    [SerializeField]
    private string saveFileName = "savegame.json";

    [Header("Save Panel")]
    [SerializeField]
    private GameObject saveSystemPanel;

    [SerializeField]
    private GameObject saveMessagePanel;

    [Header("Load Settings")]
    [SerializeField]
    private float restoreDelay = 0.5f;

    private bool isLoadingSavedGame = false;

    private PlayerSaveData.PlayerData pendingPlayerData;

    private SaveData pendingSaveData;

    private string SavePath
    {
        get
        {
            return Path.Combine(
                Application.persistentDataPath,
                saveFileName
            );
        }
    }


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded +=
            OnSceneLoaded;
    }


    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -=
                OnSceneLoaded;
        }
    }


    // =========================================================
    // SAVE
    // =========================================================

    public void SaveGame()
    {
        GameObject player =
            FindPlayer();

        if (player == null)
        {
            Debug.LogError(
                "[SaveSystem] PLAYER NOT FOUND! " +
                "Game was not saved."
            );

            return;
        }


        PlayerSaveData playerSaveData =
            player.GetComponent<PlayerSaveData>();

        if (playerSaveData == null)
        {
            Debug.LogError(
                "[SaveSystem] PlayerSaveData component " +
                "is missing from Player!"
            );

            return;
        }


        PlayerSaveData.PlayerData playerData =
            playerSaveData.CreateSaveData();


        SaveData saveData =
            new SaveData();


        // =====================================================
        // SCENE
        // =====================================================

        saveData.sceneName =
            SceneManager.GetActiveScene().name;


        // =====================================================
        // PLAYER DATA
        // =====================================================

        saveData.playerData =
            playerData;

        CaptureWorldData(saveData);


        // =====================================================
        // CONVERT TO JSON
        // =====================================================

        string json =
            JsonUtility.ToJson(
                saveData,
                true
            );


        try
        {
            File.WriteAllText(
                SavePath,
                json
            );


            Debug.Log(
                "===================================="
            );

            Debug.Log(
                "[SaveSystem] GAME SAVED SUCCESSFULLY!"
            );

            Debug.Log(
                "[SaveSystem] File: " +
                SavePath
            );

            Debug.Log(
                "[SaveSystem] Scene: " +
                saveData.sceneName
            );

            Debug.Log(
                "[SaveSystem] Player Position: " +
                player.transform.position
            );


            // =================================================
            // RESOURCE LOG
            // =================================================

            Debug.Log(
                "[SaveSystem] Food: " +
                playerData.foodCount
            );

            Debug.Log(
                "[SaveSystem] Water: " +
                playerData.waterCount
            );

            Debug.Log(
                "[SaveSystem] Stone: " +
                playerData.stoneCount
            );

            Debug.Log(
                "[SaveSystem] Herb: " +
                playerData.herbCount
            );

            Debug.Log(
                "[SaveSystem] Logs: " +
                playerData.logCount
            );


            // =================================================
            // SURVIVAL LOG
            // =================================================

            Debug.Log(
                "[SaveSystem] Health: " +
                playerData.health
            );

            Debug.Log(
                "[SaveSystem] Stamina: " +
                playerData.stamina
            );

            Debug.Log(
                "[SaveSystem] Hunger: " +
                playerData.hunger
            );

            Debug.Log(
                "[SaveSystem] Thirst: " +
                playerData.thirst
            );


            Debug.Log(
                "===================================="
            );


            ShowSaveMessage();
        }
        catch (Exception e)
        {
            Debug.LogError(
                "[SaveSystem] ERROR SAVING GAME: " +
                e.Message
            );
        }
    }


    // =========================================================
    // RESUME
    // =========================================================

    public void ResumeSavedGame()
    {
        if (!HasSaveGame())
        {
            Debug.LogWarning(
                "[SaveSystem] No save game found."
            );

            return;
        }


        SaveData data =
            LoadSaveData();


        if (data == null)
        {
            Debug.LogError(
                "[SaveSystem] Could not load save data."
            );

            return;
        }


        if (string.IsNullOrEmpty(
            data.sceneName))
        {
            Debug.LogError(
                "[SaveSystem] Saved scene is empty."
            );

            return;
        }


        pendingPlayerData =
            data.playerData;

        pendingSaveData =
            data;

        // Collected world items must be restored BEFORE the scene
        // loads, because spawners/gatherables check them on start.
        if (data.saveVersion >= 2 &&
            WorldPickupSave.Instance != null)
        {
            WorldPickupSave.Instance.ReplaceCollectedIDs(
                data.collectedPickupIDs
            );
        }


        isLoadingSavedGame = true;

        Time.timeScale = 1f;


        Debug.Log(
            "===================================="
        );

        Debug.Log(
            "[SaveSystem] LOADING SAVED GAME"
        );

        Debug.Log(
            "[SaveSystem] Scene: " +
            data.sceneName
        );


        if (data.playerData != null)
        {
            Debug.Log(
                "[SaveSystem] Saved Position: " +
                new Vector3(
                    data.playerData.positionX,
                    data.playerData.positionY,
                    data.playerData.positionZ
                )
            );


            // =================================================
            // SURVIVAL VALUES
            // =================================================

            Debug.Log(
                "[SaveSystem] Saved Health: " +
                data.playerData.health
            );

            Debug.Log(
                "[SaveSystem] Saved Stamina: " +
                data.playerData.stamina
            );

            Debug.Log(
                "[SaveSystem] Saved Hunger: " +
                data.playerData.hunger
            );

            Debug.Log(
                "[SaveSystem] Saved Thirst: " +
                data.playerData.thirst
            );
        }


        Debug.Log(
            "===================================="
        );


        SceneManager.LoadScene(
            data.sceneName
        );
    }


    // =========================================================
    // SCENE LOADED
    // =========================================================

    private void OnSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        if (!isLoadingSavedGame)
        {
            return;
        }


        StartCoroutine(
            RestorePlayerAfterSceneLoad()
        );
    }


    // =========================================================
    // RESTORE
    // =========================================================

    private IEnumerator RestorePlayerAfterSceneLoad()
    {
        Debug.Log(
            "[SaveSystem] Waiting for Player..."
        );


        yield return null;
        yield return null;
        yield return null;

        yield return new WaitForEndOfFrame();


        if (restoreDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                restoreDelay
            );
        }


        GameObject player =
            FindPlayer();


        if (player == null)
        {
            Debug.LogError(
                "[SaveSystem] Player was not found " +
                "after loading scene!"
            );

            isLoadingSavedGame = false;

            yield break;
        }


        PlayerSaveData playerSaveData =
            player.GetComponent<PlayerSaveData>();


        if (playerSaveData == null)
        {
            Debug.LogError(
                "[SaveSystem] PlayerSaveData is missing!"
            );

            isLoadingSavedGame = false;

            yield break;
        }


        if (pendingPlayerData == null)
        {
            Debug.LogError(
                "[SaveSystem] Pending player data is NULL!"
            );

            isLoadingSavedGame = false;

            yield break;
        }


        Debug.Log(
            "[SaveSystem] Restoring Player to: " +
            new Vector3(
                pendingPlayerData.positionX,
                pendingPlayerData.positionY,
                pendingPlayerData.positionZ
            )
        );


        // =====================================================
        // RESTORE ALL PLAYER DATA
        // =====================================================

        playerSaveData.LoadSaveData(
            pendingPlayerData
        );


        // Give other scripts one frame
        // to finish initialization.

        yield return null;


        // =====================================================
        // APPLY POSITION ONE MORE TIME
        // =====================================================

        CharacterController controller =
            player.GetComponent<CharacterController>();


        if (controller != null)
        {
            controller.enabled = false;
        }


        Vector3 finalPosition =
            new Vector3(
                pendingPlayerData.positionX,
                pendingPlayerData.positionY,
                pendingPlayerData.positionZ
            );


        player.transform.position =
            finalPosition;


        // Restore rotation again as well.

        Quaternion finalRotation =
            new Quaternion(
                pendingPlayerData.rotationX,
                pendingPlayerData.rotationY,
                pendingPlayerData.rotationZ,
                pendingPlayerData.rotationW
            );


        player.transform.rotation =
            finalRotation;


        if (controller != null)
        {
            controller.enabled = true;
        }


        Debug.Log(
            "[SaveSystem] FINAL PLAYER POSITION: " +
            player.transform.position
        );


        // =====================================================
        // SURVIVAL RESTORE CHECK
        // =====================================================

        SurvivalUIManager survival =
            player.GetComponent<SurvivalUIManager>();


        if (survival == null)
        {
            survival =
                FindFirstObjectByType<SurvivalUIManager>();
        }


        if (survival != null)
        {
            Debug.Log(
                "[SaveSystem] FINAL SURVIVAL VALUES: " +
                "Health=" + survival.health +
                " | Stamina=" + survival.stamina +
                " | Hunger=" + survival.hunger +
                " | Thirst=" + survival.thirst
            );

            survival.RefreshUI();
        }
        else
        {
            Debug.LogWarning(
                "[SaveSystem] SurvivalUIManager not found " +
                "after loading."
            );
        }


        // =====================================================
        // FINISH
        // =====================================================

        RestoreWorldData(pendingSaveData);

        pendingSaveData = null;

        pendingPlayerData = null;

        isLoadingSavedGame = false;


        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible = false;

        Time.timeScale = 1f;


        Debug.Log(
            "[SaveSystem] SAVE GAME RESTORE COMPLETE!"
        );
    }


    // =========================================================
    // FIND PLAYER
    // =========================================================

    private GameObject FindPlayer()
    {
        GameObject player =
            GameObject.FindWithTag("Player");


        if (player != null)
        {
            return player;
        }


        PlayerInventory inventory =
            FindFirstObjectByType<PlayerInventory>();


        if (inventory != null)
        {
            return inventory.gameObject;
        }


        PlayerSaveData saveData =
            FindFirstObjectByType<PlayerSaveData>();


        if (saveData != null)
        {
            return saveData.gameObject;
        }


        return null;
    }


    // =========================================================
    // SAVE DATA
    // =========================================================

    [Serializable]
    public class TaskSaveData
    {
        public int apples;
        public int logs;
        public int stones;
        public List<string> countedWorldPickupIDs =
            new List<string>();
    }

    [Serializable]
    public class PlacedObjectSaveData
    {
        public int itemID;
        public float posX, posY, posZ;
        public float rotX, rotY, rotZ, rotW;
        public bool isLit;
    }

    [Serializable]
    public class SaveData
    {
        // 0 = old saves (player only), 2 = includes world data.
        public int saveVersion;

        public string sceneName;

        public PlayerSaveData.PlayerData playerData;

        public TaskSaveData tasks;

        public int currentDay;
        public float currentTime;

        public List<string> collectedPickupIDs =
            new List<string>();

        public List<PlacedObjectSaveData> placedObjects =
            new List<PlacedObjectSaveData>();
    }


    // =========================================================
    // WORLD DATA (tasks, time, pickups, placed objects)
    // =========================================================

    private void CaptureWorldData(SaveData saveData)
    {
        saveData.saveVersion = 2;

        // Task panel
        if (Day1TaskManager.Instance != null)
        {
            Day1TaskManager tasks = Day1TaskManager.Instance;

            saveData.tasks = new TaskSaveData
            {
                apples = tasks.currentApples,
                logs = tasks.currentLogs,
                stones = tasks.currentStones,
                countedWorldPickupIDs =
                    tasks.GetCountedWorldPickupIDs()
            };
        }

        // Day / time
        HDRPDayNightCycle cycle =
            FindFirstObjectByType<HDRPDayNightCycle>();

        if (cycle != null)
        {
            saveData.currentDay = cycle.currentDay;
            saveData.currentTime = cycle.currentTime;
        }

        // World items already picked up
        if (WorldPickupSave.Instance != null)
        {
            saveData.collectedPickupIDs =
                WorldPickupSave.Instance.GetCollectedIDs();
        }

        // Placed objects (campfires, etc.)
        foreach (PlacedObjectMarker marker in PlacedObjectMarker.All)
        {
            if (marker == null)
                continue;

            Transform t = marker.transform;
            Campfire fire =
                marker.GetComponentInChildren<Campfire>();

            saveData.placedObjects.Add(
                new PlacedObjectSaveData
                {
                    itemID = marker.itemID,
                    posX = t.position.x,
                    posY = t.position.y,
                    posZ = t.position.z,
                    rotX = t.rotation.x,
                    rotY = t.rotation.y,
                    rotZ = t.rotation.z,
                    rotW = t.rotation.w,
                    isLit = fire != null && fire.IsLit
                }
            );
        }
    }

    private void RestoreWorldData(SaveData data)
    {
        if (data == null || data.saveVersion < 2)
            return;

        // Task panel
        if (data.tasks != null &&
            Day1TaskManager.Instance != null)
        {
            Day1TaskManager.Instance.LoadProgress(
                data.tasks.apples,
                data.tasks.logs,
                data.tasks.stones,
                data.tasks.countedWorldPickupIDs
            );
        }

        // Day / time
        HDRPDayNightCycle cycle =
            FindFirstObjectByType<HDRPDayNightCycle>();

        if (cycle != null && data.currentDay > 0)
        {
            cycle.currentDay = data.currentDay;
            cycle.currentTime = data.currentTime;
        }

        // Placed objects
        ItemDatabase database =
            ItemDatabase.Instance != null
                ? ItemDatabase.Instance
                : FindFirstObjectByType<ItemDatabase>();

        if (database == null || data.placedObjects == null)
            return;

        foreach (PlacedObjectSaveData placed in data.placedObjects)
        {
            ItemDataSO item =
                database.GetItemByID(placed.itemID);

            if (item == null || item.physicalPrefab == null)
                continue;

            GameObject obj = Instantiate(
                item.physicalPrefab,
                new Vector3(placed.posX, placed.posY, placed.posZ),
                new Quaternion(
                    placed.rotX, placed.rotY,
                    placed.rotZ, placed.rotW
                )
            );

            obj.name = item.itemName;

            foreach (Rigidbody rb in
                obj.GetComponentsInChildren<Rigidbody>())
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            PlacedObjectMarker marker =
                obj.AddComponent<PlacedObjectMarker>();

            marker.itemID = item.itemID;

            Campfire fire =
                obj.GetComponentInChildren<Campfire>();

            if (fire != null)
                fire.RestoreLitState(placed.isLit);
        }
    }


    // =========================================================
    // CHECK SAVE
    // =========================================================

    public bool HasSaveGame()
    {
        return File.Exists(
            SavePath
        );
    }


    // =========================================================
    // LOAD JSON
    // =========================================================

    public SaveData LoadSaveData()
    {
        if (!File.Exists(SavePath))
        {
            Debug.LogWarning(
                "[SaveSystem] Save file does not exist."
            );

            return null;
        }


        try
        {
            string json =
                File.ReadAllText(
                    SavePath
                );


            SaveData data =
                JsonUtility.FromJson<SaveData>(
                    json
                );


            return data;
        }
        catch (Exception e)
        {
            Debug.LogError(
                "[SaveSystem] ERROR READING SAVE: " +
                e.Message
            );

            return null;
        }
    }


    // =========================================================
    // DELETE SAVE
    // =========================================================

    public void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);

            Debug.Log(
                "[SaveSystem] Save file deleted."
            );
        }
        else
        {
            Debug.Log(
                "[SaveSystem] No save file to delete."
            );
        }
    }


    // =========================================================
    // SAVE PANEL
    // =========================================================

    public void OpenSavePanel()
    {
        GameObject panel =
            FindSavePanel();


        if (panel != null)
        {
            panel.SetActive(true);
        }


        Time.timeScale = 0f;


        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible = true;
    }


    public void CloseSavePanel()
    {
        GameObject panel =
            FindSavePanel();


        if (panel != null)
        {
            panel.SetActive(false);
        }


        Time.timeScale = 1f;


        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible = false;
    }


    private GameObject FindSavePanel()
    {
        if (saveSystemPanel != null)
        {
            return saveSystemPanel;
        }


        GameObject[] objects =
            Resources.FindObjectsOfTypeAll<GameObject>();


        foreach (GameObject obj in objects)
        {
            if (obj.name == "SaveSystemPanel")
            {
                if (obj.scene.IsValid())
                {
                    saveSystemPanel = obj;

                    return obj;
                }
            }
        }


        return null;
    }


    private void ShowSaveMessage()
    {
        if (saveMessagePanel != null)
        {
            saveMessagePanel.SetActive(true);


            StartCoroutine(
                HideSaveMessage()
            );
        }
    }


    private IEnumerator HideSaveMessage()
    {
        yield return new WaitForSecondsRealtime(
            2f
        );


        if (saveMessagePanel != null)
        {
            saveMessagePanel.SetActive(false);
        }
    }
}