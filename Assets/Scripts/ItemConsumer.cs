using UnityEngine;

public class ItemConsumer : MonoBehaviour
{
    private PlayerStats playerStats;

    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogError("[ItemConsumer] Missing PlayerStats component on this GameObject!");
        }
    }

    /// <summary>
    /// Attempts to consume an item from ItemDataSO.
    /// Returns true if successfully consumed, false if it wasn't a consumable.
    /// </summary>
    public bool TryConsumeItem(ItemDataSO itemData)
    {
        if (itemData == null)
        {
            Debug.LogWarning("Cannot consume: ItemDataSO is null.");
            return false;
        }

        // Verify that this item is actually a consumable
        if (itemData.itemType != ItemType.Consumable)
        {
            Debug.Log($"{itemData.itemName} is not a consumable item.");
            return false;
        }

        if (playerStats == null) return false;

        // Apply health, hunger, and thirst stats
        playerStats.RestoreStats(
            itemData.healthRestore,
            itemData.hungerRestore,
            itemData.thirstRestore
        );

        Debug.Log($"Consumed: {itemData.itemName}");
        return true;
    }
}