using System.Collections.Generic;
using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance { get; private set; }

    [Header("All Item Data")]
    [Tooltip("Add every ItemDataSO used by the game here.")]
    public List<ItemDataSO> allItems = new List<ItemDataSO>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        Debug.Log(
            "[ItemDatabase] Item Database initialized. " +
            "Items: " + allItems.Count
        );
    }

    public ItemDataSO GetItemByID(int itemID)
    {
        foreach (ItemDataSO item in allItems)
        {
            if (item == null)
                continue;

            if (item.itemID == itemID)
            {
                return item;
            }
        }

        Debug.LogWarning(
            "[ItemDatabase] Item ID " +
            itemID +
            " was not found."
        );

        return null;
    }
}