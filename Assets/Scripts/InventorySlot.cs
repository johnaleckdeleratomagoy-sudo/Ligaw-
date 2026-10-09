using UnityEngine;

public class InventorySlot : MonoBehaviour
{
    [SerializeField] private InventoryItem innerItem;

    void Awake()
    {
        // Automatically grab inner InventoryItem if not assigned in Inspector
        if (innerItem == null)
        {
            innerItem = GetComponentInChildren<InventoryItem>();
        }

        if (innerItem == null)
        {
            Debug.LogError($"[InventorySlot] No InventoryItem found on {gameObject.name}!");
        }
    }

    /// <summary>
    /// Checks if the slot is currently empty.
    /// </summary>
    public bool IsEmpty()
    {
        return innerItem == null || innerItem.GetItemID() == -1;
    }

    /// <summary>
    /// Assigns a new item to this inventory slot.
    /// </summary>
    public void AssignPiece(ItemDataSO itemData, int amount = 1)
    {
        if (innerItem != null && itemData != null)
        {
            innerItem.SetupItem(itemData, amount);
        }
    }

    /// <summary>
    /// Adds items to the existing stack. Returns any leftover overflow amount.
    /// </summary>
    public int AddToStack(int amountToAdd)
    {
        if (innerItem == null || IsEmpty()) return amountToAdd;

        return innerItem.AddToStack(amountToAdd);
    }

    /// <summary>
    /// Removes a specified amount from the current stack.
    /// </summary>
    public bool RemoveFromStack(int amountToRemove = 1)
    {
        if (innerItem == null || IsEmpty()) return false;

        return innerItem.RemoveFromStack(amountToRemove);
    }

    /// <summary>
    /// Returns the ID of the item stored in this slot (-1 if empty).
    /// </summary>
    public int GetStoredPieceID()
    {
        return innerItem != null ? innerItem.GetItemID() : -1;
    }

    /// <summary>
    /// Clears and resets this inventory slot UI.
    /// </summary>
    public void ClearSlot()
    {
        if (innerItem != null)
        {
            innerItem.ClearItem();
        }
    }

    // --- CRAFTING SYSTEM INTEGRATION METHODS ---

    /// <summary>
    /// Gets the ItemDataSO assigned to the inner UI item.
    /// </summary>
    public ItemDataSO GetItemData()
    {
        if (innerItem == null || IsEmpty()) return null;

        return innerItem.currentItemData;
    }

    /// <summary>
    /// Returns the current stack count inside this slot.
    /// </summary>
    public int GetItemCount()
    {
        if (innerItem == null || IsEmpty()) return 0;

        return innerItem.stackCount;
    }

    /// <summary>
    /// Reduces stack size by amount, clearing the slot if it hits 0.
    /// </summary>
    public void ReduceCount(int amount)
    {
        if (innerItem == null || IsEmpty()) return;

        bool hasRemaining = innerItem.RemoveFromStack(amount);

        // If stack is completely consumed, clear the slot UI
        if (!hasRemaining || GetItemCount() <= 0)
        {
            ClearSlot();
        }
    }

    /// <summary>
    /// Helper to directly reference the inner item script.
    /// </summary>
    public InventoryItem GetInnerItem()
    {
        return innerItem;
    }
}