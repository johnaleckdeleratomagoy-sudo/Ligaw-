using UnityEngine;

public enum ItemType
{
    Resource,     // Logs, Apples, Stones
    Tool,         // Axes, Pickaxes
    Consumable,   // Food, Water
    Equipment,    // Armor
    Placeable     // Campfires, Structures
}

[CreateAssetMenu(fileName = "New Item Data", menuName = "Inventory/Item Data")]
public class ItemDataSO : ScriptableObject
{
    [Header("Basic Info")]
    public int itemID;
    public string itemName;
    public ItemType itemType = ItemType.Resource;

    [TextArea(3, 5)]
    public string description;

    public Sprite itemIcon;

    [Header("3D Models")]
    [Tooltip("The 3D prefab dropped onto the ground in the world.")]
    public GameObject physicalPrefab;

    [Tooltip("The 3D prefab displayed in the player's hands when selected/equipped.")]
    public GameObject pickupInHand;

    [Header("Hand Alignment Offsets")]
    public Vector3 handPositionOffset = Vector3.zero;

    [Tooltip("Rotation offset in degrees (Euler angles).")]
    public Vector3 handRotationOffset = Vector3.zero;

    public Vector3 handScale = Vector3.one;

    [Header("Stacking Settings")]
    public bool isStackable = true;

    [Min(1)]
    public int maxStackSize = 64;

    [Header("Tool & Durability")]
    public bool hasDurability = false;

    [Min(1)]
    public int maxDurability = 100;

    [Header("Consumable Stats")]
    [Min(0)]
    public float healthRestore = 0f;

    [Min(0)]
    public float hungerRestore = 0f;

    [Min(0)]
    public float thirstRestore = 0f;

    private void OnValidate()
    {
        // If the item cannot stack,
        // automatically force the maximum stack size to 1.
        if (!isStackable)
        {
            maxStackSize = 1;
        }

        // Prevent invalid stack sizes.
        if (maxStackSize < 1)
        {
            maxStackSize = 1;
        }

        // Prevent invalid durability values.
        if (maxDurability < 1)
        {
            maxDurability = 1;
        }

        // Prevent negative consumable values.
        if (healthRestore < 0f)
        {
            healthRestore = 0f;
        }

        if (hungerRestore < 0f)
        {
            hungerRestore = 0f;
        }

        if (thirstRestore < 0f)
        {
            thirstRestore = 0f;
        }
    }

    /// <summary>
    /// Returns the unique ID of this item.
    /// Used by the save/load system.
    /// </summary>
    public int GetItemID()
    {
        return itemID;
    }

    /// <summary>
    /// Returns the display name of this item.
    /// </summary>
    public string GetItemName()
    {
        return itemName;
    }

    /// <summary>
    /// Returns whether this item can be stacked.
    /// </summary>
    public bool CanStack()
    {
        return isStackable;
    }

    /// <summary>
    /// Returns the maximum number of this item
    /// that can be stored in one stack.
    /// </summary>
    public int GetMaxStackSize()
    {
        return maxStackSize;
    }
}