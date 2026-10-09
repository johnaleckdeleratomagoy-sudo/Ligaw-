using System;
using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
public class PlayerSaveData : MonoBehaviour
{
    // =========================================================
    // HOTBAR SLOT SAVE DATA
    // =========================================================

    [Serializable]
    public class HotbarSlotSaveData
    {
        public int itemID = -1;
        public int stackCount = 0;
    }


    // =========================================================
    // PLAYER SAVE DATA
    // =========================================================

    [Serializable]
    public class PlayerData
    {
        // -----------------------------------------------------
        // PLAYER POSITION
        // -----------------------------------------------------

        public float positionX;
        public float positionY;
        public float positionZ;


        // -----------------------------------------------------
        // PLAYER ROTATION
        // -----------------------------------------------------

        public float rotationX;
        public float rotationY;
        public float rotationZ;
        public float rotationW;


        // -----------------------------------------------------
        // RESOURCE COUNTS
        // -----------------------------------------------------

        public int foodCount;
        public int waterCount;
        public int stoneCount;
        public int herbCount;
        public int logCount;


        // -----------------------------------------------------
        // SURVIVAL STATS
        // -----------------------------------------------------

        public float health;
        public float stamina;
        public float hunger;
        public float thirst;


        // -----------------------------------------------------
        // HOTBAR
        // -----------------------------------------------------

        public int selectedHotbarSlot;

        public HotbarSlotSaveData[] hotbarSlots;
    }


    // =========================================================
    // REFERENCES
    // =========================================================

    private PlayerInventory playerInventory;
    private CharacterController characterController;
    private SurvivalUIManager survivalUI;
    private PlayerMovement playerMovement;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        playerInventory =
            GetComponent<PlayerInventory>();

        characterController =
            GetComponent<CharacterController>();

        survivalUI =
            GetComponent<SurvivalUIManager>();

        playerMovement =
            GetComponent<PlayerMovement>();
    }


    // =========================================================
    // CREATE SAVE DATA
    // =========================================================

    public PlayerData CreateSaveData()
    {
        if (playerInventory == null)
        {
            playerInventory =
                GetComponent<PlayerInventory>();
        }

        if (survivalUI == null)
        {
            survivalUI =
                GetComponent<SurvivalUIManager>();

            if (survivalUI == null)
            {
                survivalUI =
                    FindFirstObjectByType<SurvivalUIManager>();
            }
        }

        if (playerMovement == null)
        {
            playerMovement =
                GetComponent<PlayerMovement>();
        }

        PlayerData data =
            new PlayerData();


        // =====================================================
        // PLAYER POSITION
        // =====================================================

        Vector3 position =
            transform.position;

        data.positionX =
            position.x;

        data.positionY =
            position.y;

        data.positionZ =
            position.z;


        // =====================================================
        // PLAYER ROTATION
        // =====================================================

        Quaternion rotation =
            transform.rotation;

        data.rotationX =
            rotation.x;

        data.rotationY =
            rotation.y;

        data.rotationZ =
            rotation.z;

        data.rotationW =
            rotation.w;


        // =====================================================
        // RESOURCE COUNTS
        // =====================================================

        data.foodCount =
            playerInventory.foodCount;

        data.waterCount =
            playerInventory.waterCount;

        data.stoneCount =
            playerInventory.stoneCount;

        data.herbCount =
            playerInventory.herbCount;

        data.logCount =
            playerInventory.logCount;


        // =====================================================
        // SURVIVAL STATS
        // =====================================================

        if (survivalUI != null)
        {
            data.health =
                survivalUI.health;

            data.hunger =
                survivalUI.hunger;

            data.thirst =
                survivalUI.thirst;
        }
        else
        {
            data.health = 100f;
            data.hunger = 100f;
            data.thirst = 100f;

            Debug.LogWarning(
                "[PlayerSaveData] SurvivalUIManager not found. " +
                "Saving default Health/Hunger/Thirst values."
            );
        }

        // Stamina is controlled by PlayerMovement.
        if (playerMovement != null)
        {
            data.stamina =
                playerMovement.GetCurrentStamina();
        }
        else if (survivalUI != null)
        {
            data.stamina =
                survivalUI.stamina;
        }
        else
        {
            data.stamina = 100f;
        }


        // =====================================================
        // HOTBAR
        // =====================================================

        HotbarManager hotbar =
            playerInventory.hotbarUI;

        if (hotbar != null &&
            hotbar.hotbarSlots != null)
        {
            data.selectedHotbarSlot =
                hotbar.currentSelectedSlot;

            data.hotbarSlots =
                new HotbarSlotSaveData[
                    hotbar.hotbarSlots.Count
                ];


            for (
                int i = 0;
                i < hotbar.hotbarSlots.Count;
                i++
            )
            {
                data.hotbarSlots[i] =
                    new HotbarSlotSaveData();

                HotbarSlot slot =
                    hotbar.hotbarSlots[i];

                if (slot == null)
                {
                    data.hotbarSlots[i].itemID = -1;
                    data.hotbarSlots[i].stackCount = 0;

                    continue;
                }

                data.hotbarSlots[i].itemID =
                    slot.GetStoredPieceID();

                data.hotbarSlots[i].stackCount =
                    slot.GetItemCount();

                Debug.Log(
                    "[PlayerSaveData] Saving Hotbar Slot " +
                    (i + 1) +
                    " | Item ID: " +
                    data.hotbarSlots[i].itemID +
                    " | Amount: " +
                    data.hotbarSlots[i].stackCount
                );
            }
        }
        else
        {
            data.selectedHotbarSlot = 0;

            data.hotbarSlots =
                new HotbarSlotSaveData[0];

            Debug.LogWarning(
                "[PlayerSaveData] HotbarManager or " +
                "hotbar slots not found."
            );
        }


        // =====================================================
        // SAVE SUMMARY
        // =====================================================

        Debug.Log(
            "========================================\n" +
            "[PlayerSaveData] SAVE DATA CREATED\n" +
            "Position: " + transform.position + "\n" +
            "Health: " + data.health + "\n" +
            "Stamina: " + data.stamina + "\n" +
            "Hunger: " + data.hunger + "\n" +
            "Thirst: " + data.thirst + "\n" +
            "Food: " + data.foodCount + "\n" +
            "Water: " + data.waterCount + "\n" +
            "Stone: " + data.stoneCount + "\n" +
            "Herb: " + data.herbCount + "\n" +
            "Logs: " + data.logCount + "\n" +
            "Hotbar Slots: " +
            data.hotbarSlots.Length +
            "\n========================================"
        );


        return data;
    }


    // =========================================================
    // LOAD PLAYER DATA
    // =========================================================

    public void LoadSaveData(PlayerData data)
    {
        if (data == null)
        {
            Debug.LogError(
                "[PlayerSaveData] Save data is NULL!"
            );

            return;
        }


        if (playerInventory == null)
        {
            playerInventory =
                GetComponent<PlayerInventory>();
        }

        if (survivalUI == null)
        {
            survivalUI =
                GetComponent<SurvivalUIManager>();

            if (survivalUI == null)
            {
                survivalUI =
                    FindFirstObjectByType<SurvivalUIManager>();
            }
        }

        if (playerMovement == null)
        {
            playerMovement =
                GetComponent<PlayerMovement>();
        }


        Debug.Log(
            "[PlayerSaveData] RESTORING PLAYER..."
        );


        // =====================================================
        // DISABLE CHARACTER CONTROLLER
        // =====================================================

        if (characterController != null)
        {
            characterController.enabled = false;
        }


        // =====================================================
        // RESTORE POSITION
        // =====================================================

        Vector3 savedPosition =
            new Vector3(
                data.positionX,
                data.positionY,
                data.positionZ
            );

        transform.position =
            savedPosition;


        // =====================================================
        // RESTORE ROTATION
        // =====================================================

        Quaternion savedRotation =
            new Quaternion(
                data.rotationX,
                data.rotationY,
                data.rotationZ,
                data.rotationW
            );

        transform.rotation =
            savedRotation;


        // =====================================================
        // RESTORE RESOURCE COUNTS
        // =====================================================

        playerInventory.foodCount =
            data.foodCount;

        playerInventory.waterCount =
            data.waterCount;

        playerInventory.stoneCount =
            data.stoneCount;

        playerInventory.herbCount =
            data.herbCount;

        playerInventory.logCount =
            data.logCount;


        // =====================================================
        // RESTORE SURVIVAL STATS
        // =====================================================

        if (survivalUI != null)
        {
            survivalUI.LoadSurvivalStats(
                data.health,
                data.hunger,
                data.thirst,
                data.stamina
            );
        }


        // =====================================================
        // RESTORE PLAYER MOVEMENT STAMINA
        // =====================================================

        if (playerMovement != null)
        {
            playerMovement.SetCurrentStamina(
                data.stamina
            );
        }


        // =====================================================
        // RESTORE HOTBAR
        // =====================================================

        RestoreHotbar(data);


        // =====================================================
        // ENABLE CHARACTER CONTROLLER
        // =====================================================

        if (characterController != null)
        {
            characterController.enabled = true;
        }


        // =====================================================
        // RESTORE SUMMARY
        // =====================================================

        Debug.Log(
            "========================================\n" +
            "[PlayerSaveData] PLAYER RESTORED!\n" +
            "Position: " + transform.position + "\n" +
            "Health: " + data.health + "\n" +
            "Stamina: " + data.stamina + "\n" +
            "Hunger: " + data.hunger + "\n" +
            "Thirst: " + data.thirst + "\n" +
            "Food: " + playerInventory.foodCount + "\n" +
            "Water: " + playerInventory.waterCount + "\n" +
            "Stone: " + playerInventory.stoneCount + "\n" +
            "Herb: " + playerInventory.herbCount + "\n" +
            "Logs: " + playerInventory.logCount +
            "\n========================================"
        );
    }


    // =========================================================
    // RESTORE HOTBAR
    // =========================================================

    private void RestoreHotbar(PlayerData data)
    {
        HotbarManager hotbar =
            playerInventory.hotbarUI;


        if (hotbar == null)
        {
            Debug.LogError(
                "[PlayerSaveData] HotbarManager is NULL."
            );

            return;
        }


        if (hotbar.hotbarSlots == null)
        {
            Debug.LogError(
                "[PlayerSaveData] Hotbar slots are NULL."
            );

            return;
        }


        if (data.hotbarSlots == null)
        {
            Debug.LogWarning(
                "[PlayerSaveData] Saved hotbar data is NULL."
            );

            return;
        }


        // =====================================================
        // CLEAR CURRENT HOTBAR
        // =====================================================

        foreach (HotbarSlot slot in hotbar.hotbarSlots)
        {
            if (slot != null)
            {
                slot.ClearSlot();
            }
        }


        // =====================================================
        // GET ITEM DATABASE
        // =====================================================

        ItemDatabase database =
            ItemDatabase.Instance;

        if (database == null)
        {
            database =
                FindFirstObjectByType<ItemDatabase>();
        }

        if (database == null)
        {
            Debug.LogError(
                "[PlayerSaveData] ItemDatabase was NOT found!"
            );

            return;
        }


        // =====================================================
        // RESTORE ITEMS
        // =====================================================

        int slotCount =
            Mathf.Min(
                hotbar.hotbarSlots.Count,
                data.hotbarSlots.Length
            );


        for (
            int i = 0;
            i < slotCount;
            i++
        )
        {
            HotbarSlotSaveData savedSlot =
                data.hotbarSlots[i];

            if (savedSlot == null)
            {
                continue;
            }


            if (
                savedSlot.itemID == -1 ||
                savedSlot.stackCount <= 0
            )
            {
                continue;
            }


            HotbarSlot targetSlot =
                hotbar.hotbarSlots[i];

            if (targetSlot == null)
            {
                continue;
            }


            ItemDataSO itemData =
                database.GetItemByID(
                    savedSlot.itemID
                );

            if (itemData == null)
            {
                Debug.LogError(
                    "[PlayerSaveData] Could not find ItemDataSO!\n" +
                    "Item ID: " +
                    savedSlot.itemID +
                    "\nHotbar Slot: " +
                    (i + 1)
                );

                continue;
            }


            targetSlot.AssignPiece(
                itemData,
                savedSlot.stackCount
            );


            Debug.Log(
                "[PlayerSaveData] RESTORED Hotbar Slot " +
                (i + 1) +
                " | " +
                itemData.itemName +
                " x" +
                savedSlot.stackCount
            );
        }


        // =====================================================
        // RESTORE SELECTED SLOT
        // =====================================================

        if (hotbar.hotbarSlots.Count > 0)
        {
            int selectedSlot =
                Mathf.Clamp(
                    data.selectedHotbarSlot,
                    0,
                    hotbar.hotbarSlots.Count - 1
                );

            hotbar.SelectSlot(
                selectedSlot
            );
        }


        // =====================================================
        // UPDATE HOTBAR
        // =====================================================

        hotbar.NotifyHotbarChanged();


        Debug.Log(
            "[PlayerSaveData] HOTBAR RESTORE COMPLETE."
        );
    }
}