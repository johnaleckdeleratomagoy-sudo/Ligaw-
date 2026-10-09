using UnityEngine;

public class DroppedItem : MonoBehaviour
{
    private ItemDataSO itemData;
    private int amount = 1;

    private float pickupDelay;
    private float spawnTime;

    // =========================================================
    // INITIALIZE
    // =========================================================

    public void Initialize(ItemDataSO data, float delay, int itemAmount = 1)
    {
        itemData = data;
        amount = Mathf.Max(1, itemAmount);

        pickupDelay = delay;
        spawnTime = Time.time;
    }

    // =========================================================
    // PICKUP CHECK
    // =========================================================

    public bool CanBePickedUp()
    {
        return Time.time >= spawnTime + pickupDelay;
    }

    // =========================================================
    // GET ITEM DATA
    // =========================================================

    public ItemDataSO GetItemData()
    {
        return itemData;
    }

    public int GetAmount()
    {
        return amount;
    }
}