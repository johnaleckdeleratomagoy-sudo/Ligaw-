using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    [Header("Resource Gathering Counts")]
    public int foodCount;
    public int waterCount;
    public int stoneCount;
    public int herbCount;
    public int logCount;

    [Header("Camera & View References")]
    public Transform handTransform;
    public HotbarManager hotbarUI;
    public float reachDistance = 10f;

    [Header("Grid Setup")]
    [Tooltip("Set this to your Frame Slot layer to make sure placement left-clicks detect the board accurately")]
    public LayerMask frameLayer;

    private int currentSlotIndex = 0;
    private bool isInspectingPuzzle = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (hotbarUI == null)
        {
            hotbarUI = FindFirstObjectByType<HotbarManager>();
        }
    }

    private void Start()
    {
        LockCursor(true);

        if (hotbarUI == null)
        {
            hotbarUI = FindFirstObjectByType<HotbarManager>();
        }
    }

    private void Update()
    {
        // =====================================================
        // HOTBAR SELECTION
        // =====================================================

        for (int i = 0; i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SwitchSlot(i);
            }
        }

        // =====================================================
        // LEFT CLICK
        // =====================================================

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleClickPlacement();
        }
    }

    // =========================================================
    // CRAFTING
    // =========================================================

    public bool HasIngredients(List<Ingredients> ingredients)
    {
        if (ingredients == null)
            return false;

        foreach (var ingredient in ingredients)
        {
            if (ingredient.item == null)
                continue;

            if (GetItemCount(ingredient.item) < ingredient.amount)
            {
                return false;
            }
        }

        return true;
    }

    public void ConsumeIngredients(List<Ingredients> ingredients)
    {
        if (ingredients == null ||
            hotbarUI == null ||
            hotbarUI.hotbarSlots == null)
        {
            return;
        }

        foreach (var ingredient in ingredients)
        {
            if (ingredient.item == null)
                continue;

            int amountLeftToDeduct = ingredient.amount;

            // Take from the inventory grid first so the hotbar
            // (and whatever is in hand) is only used as a last resort.
            foreach (InventoryItem itemUI in GetInventoryGridItems())
            {
                if (amountLeftToDeduct <= 0)
                    break;

                amountLeftToDeduct -=
                    DeductFromItem(
                        itemUI,
                        ingredient.item,
                        amountLeftToDeduct
                    );
            }

            foreach (var slot in hotbarUI.hotbarSlots)
            {
                if (amountLeftToDeduct <= 0)
                    break;

                if (slot == null)
                    continue;

                amountLeftToDeduct -=
                    DeductFromItem(
                        slot.GetComponentInChildren<InventoryItem>(),
                        ingredient.item,
                        amountLeftToDeduct
                    );
            }

            string itemName =
                ingredient.item.itemName != null
                ? ingredient.item.itemName.ToLower()
                : ingredient.item.name.ToLower();

            if (itemName.Contains("stone") ||
                itemName.Contains("rock"))
            {
                stoneCount =
                    Mathf.Max(
                        0,
                        stoneCount -
                        ingredient.amount
                    );
            }
            else if (itemName.Contains("food") ||
                     itemName.Contains("apple"))
            {
                foodCount =
                    Mathf.Max(
                        0,
                        foodCount -
                        ingredient.amount
                    );
            }
            else if (itemName.Contains("water"))
            {
                waterCount =
                    Mathf.Max(
                        0,
                        waterCount -
                        ingredient.amount
                    );
            }
            else if (itemName.Contains("herb") ||
                     itemName.Contains("plant"))
            {
                herbCount =
                    Mathf.Max(
                        0,
                        herbCount -
                        ingredient.amount
                    );
            }
            else if (itemName.Contains("log") ||
                     itemName.Contains("wood"))
            {
                logCount =
                    Mathf.Max(
                        0,
                        logCount -
                        ingredient.amount
                    );
            }
        }

        hotbarUI.NotifyHotbarChanged();

        // Refresh the held item so a consumed ingredient doesn't stay in hand.
        hotbarUI.UpdateHandVisual();
    }

    // =========================================================
    // GET ITEM COUNT
    // =========================================================

    public int GetItemCount(ItemDataSO targetItem)
    {
        if (targetItem == null)
            return 0;

        int totalCount = 0;

        // Hotbar
        if (hotbarUI != null &&
            hotbarUI.hotbarSlots != null)
        {
            foreach (var slot in hotbarUI.hotbarSlots)
            {
                if (slot == null)
                    continue;

                InventoryItem itemUI =
                    slot.GetComponentInChildren<InventoryItem>();

                if (itemUI != null &&
                    itemUI.GetItemID() == targetItem.itemID)
                {
                    totalCount +=
                        itemUI.GetStackCount();
                }
            }
        }

        // Inventory grid
        foreach (InventoryItem itemUI in GetInventoryGridItems())
        {
            if (itemUI.GetItemID() == targetItem.itemID)
            {
                totalCount +=
                    itemUI.GetStackCount();
            }
        }

        return totalCount;
    }

    // =========================================================
    // INVENTORY GRID HELPERS
    // =========================================================

    private List<InventoryItem> GetInventoryGridItems()
    {
        List<InventoryItem> items = new List<InventoryItem>();

        InventorySlot[] slots =
            FindObjectsByType<InventorySlot>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (InventorySlot slot in slots)
        {
            if (slot == null)
                continue;

            InventoryItem itemUI =
                slot.GetComponentInChildren<InventoryItem>(true);

            if (itemUI != null)
                items.Add(itemUI);
        }

        return items;
    }

    private int DeductFromItem(
        InventoryItem itemUI,
        ItemDataSO target,
        int amountWanted)
    {
        if (itemUI == null ||
            itemUI.GetItemID() != target.itemID)
        {
            return 0;
        }

        int deductAmount =
            Mathf.Min(
                amountWanted,
                itemUI.GetStackCount()
            );

        if (deductAmount > 0)
        {
            itemUI.RemoveFromStack(deductAmount);
        }

        return deductAmount;
    }

    /// <summary>
    /// Used when the hotbar is full, so crafted/picked-up items
    /// aren't lost. Fills matching stacks first, then empty slots.
    /// </summary>
    private bool TryAddToInventoryGrid(
        ItemDataSO item,
        int amount)
    {
        List<InventoryItem> items = GetInventoryGridItems();
        int remaining = amount;

        if (item.isStackable)
        {
            foreach (InventoryItem itemUI in items)
            {
                if (remaining <= 0)
                    break;

                if (itemUI.GetItemID() != item.itemID)
                    continue;

                remaining = itemUI.AddToStack(remaining);
            }
        }

        foreach (InventoryItem itemUI in items)
        {
            if (remaining <= 0)
                break;

            if (itemUI.GetItemID() != -1)
                continue;

            int amountForSlot =
                item.isStackable
                ? Mathf.Min(remaining, item.maxStackSize)
                : 1;

            itemUI.SetupItem(item, amountForSlot);
            remaining -= amountForSlot;
        }

        return remaining <= 0;
    }

    // =========================================================
    // RESOURCE PICKUP
    // =========================================================

    public void AddResource(
        ItemDataSO itemData,
        int amount)
    {
        if (itemData == null)
            return;

        string itemName =
            itemData.itemName != null
            ? itemData.itemName.ToLower()
            : itemData.name.ToLower();

        if (itemName.Contains("food") ||
            itemName.Contains("apple"))
        {
            foodCount += amount;
        }
        else if (itemName.Contains("water"))
        {
            waterCount += amount;
        }
        else if (itemName.Contains("stone") ||
                 itemName.Contains("rock"))
        {
            stoneCount += amount;
        }
        else if (itemName.Contains("herb") ||
                 itemName.Contains("plant"))
        {
            herbCount += amount;
        }
        else if (itemName.Contains("log") ||
                 itemName.Contains("wood"))
        {
            logCount += amount;
        }

        Debug.Log(
            "[Inventory] Updated! " +
            "Food: " + foodCount +
            " | Water: " + waterCount +
            " | Stone: " + stoneCount +
            " | Herb: " + herbCount +
            " | Logs: " + logCount
        );
    }

    // =========================================================
    // PICK UP WORLD GATHERABLE
    // =========================================================

    public bool DirectPickUp(
        GameObject worldObject,
        ItemDataSO itemData,
        int amount = 1)
    {
        if (itemData == null)
        {
            Debug.LogWarning(
                "[PlayerInventory] ItemDataSO is null."
            );

            return false;
        }

        if (hotbarUI == null)
        {
            Debug.LogWarning(
                "[PlayerInventory] HotbarManager is not assigned."
            );

            return false;
        }

        // Add to hotbar first, then fall back to the inventory grid.
        bool addedSuccessfully =
            hotbarUI.TryAddToHotbar(
                itemData,
                amount
            ) ||
            TryAddToInventoryGrid(
                itemData,
                amount
            );

        if (!addedSuccessfully)
        {
            Debug.LogWarning(
                "[PlayerInventory] Could not add " +
                itemData.itemName +
                " to hotbar."
            );

            return false;
        }

        // =====================================================
        // ONLY WORLD PICKUPS UPDATE RESOURCE COUNTS
        // =====================================================

        AddResource(
            itemData,
            amount
        );

        if (worldObject != null)
        {
            Collider[] colliders =
                worldObject.GetComponentsInChildren<Collider>();

            foreach (Collider col in colliders)
            {
                if (col != null)
                    col.enabled = false;
            }

            Gatherable gatherable =
                worldObject.GetComponent<Gatherable>();

            if (gatherable != null)
            {
                gatherable.MarkAsCollected();
            }

            Destroy(worldObject);
        }

        Debug.Log(
            "[PlayerInventory] Picked up WORLD item: " +
            itemData.itemName +
            " x" +
            amount
        );

        return true;
    }

    // =========================================================
    // PICK UP DROPPED ITEM
    // =========================================================

    public bool PickUpDroppedItem(
        GameObject droppedObject,
        ItemDataSO itemData,
        int amount = 1)
    {
        if (itemData == null)
        {
            Debug.LogWarning(
                "[PlayerInventory] Dropped item has no ItemDataSO."
            );

            return false;
        }

        if (hotbarUI == null)
        {
            Debug.LogWarning(
                "[PlayerInventory] HotbarManager is not assigned."
            );

            return false;
        }

        // =====================================================
        // PUT IT BACK INTO THE HOTBAR
        // =====================================================

        bool addedSuccessfully =
            hotbarUI.TryAddToHotbar(
                itemData,
                amount
            );

        if (!addedSuccessfully)
        {
            Debug.LogWarning(
                "[PlayerInventory] Hotbar is full. " +
                itemData.itemName +
                " remains on the ground."
            );

            return false;
        }

        // =====================================================
        // IMPORTANT:
        //
        // DO NOT CALL AddResource() HERE.
        //
        // This is NOT a new world resource.
        // It was already counted when originally gathered.
        // =====================================================

        if (droppedObject != null)
        {
            Collider[] colliders =
                droppedObject.GetComponentsInChildren<Collider>();

            foreach (Collider col in colliders)
            {
                if (col != null)
                    col.enabled = false;
            }

            Rigidbody rb =
                droppedObject.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            Destroy(droppedObject);
        }

        Debug.Log(
            "[PlayerInventory] Picked up DROPPED item: " +
            itemData.itemName +
            " x" +
            amount +
            " | Day 1 progress NOT increased."
        );

        return true;
    }

    // =========================================================
    // PUZZLE VIEW
    // =========================================================

    public void SetPuzzleViewMode(
        bool dynamicState)
    {
        isInspectingPuzzle =
            dynamicState;

        LockCursor(!dynamicState);
    }

    private void LockCursor(
        bool shouldLock)
    {
        if (shouldLock)
        {
            Cursor.lockState =
                CursorLockMode.Locked;

            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState =
                CursorLockMode.None;

            Cursor.visible = true;
        }
    }

    // =========================================================
    // CLICK PLACEMENT
    // =========================================================

    private void HandleClickPlacement()
    {
        Camera activeCam =
            Camera.main;

        if (activeCam == null)
        {
            Debug.LogError(
                "[Inventory] No active camera found!"
            );

            return;
        }

        if (hotbarUI == null ||
            hotbarUI.hotbarSlots == null)
        {
            return;
        }

        if (currentSlotIndex < 0 ||
            currentSlotIndex >=
            hotbarUI.hotbarSlots.Count)
        {
            return;
        }

        HotbarSlot activeSlot =
            hotbarUI.hotbarSlots[
                currentSlotIndex
            ];

        if (activeSlot == null)
        {
            return;
        }

        int activeItemID =
            activeSlot.GetStoredPieceID();

        if (activeItemID == -1)
            return;
    }

    // =========================================================
    // HOTBAR
    // =========================================================

    private void SwitchSlot(
        int index)
    {
        if (hotbarUI == null ||
            hotbarUI.hotbarSlots == null ||
            index < 0 ||
            index >= hotbarUI.hotbarSlots.Count)
        {
            return;
        }

        currentSlotIndex =
            index;

        hotbarUI.SelectSlot(index);

        Debug.Log(
            "[Inventory] Switched to Hotbar Slot: " +
            (index + 1)
        );
    }
}