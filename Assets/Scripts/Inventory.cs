using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    [Header("Crafting Setup")]
    public List<Recipe> allRecipes = new List<Recipe>();
    public Transform craftingGrid;
    public GameObject craftingButtonPrefab;

    [Header("Inventory References")]
    public HotbarManager hotbarManager;

    [Header("Workbench Settings")]
    public bool isAtWorkbench = false; // Set to true when standing near the workbench

    private void Awake()
    {
        PopulateCraftingGrid();
    }

    public void SetAtWorkbenchState(bool state)
    {
        isAtWorkbench = state;
        PopulateCraftingGrid(); // Refresh UI when entering/leaving table
    }

    public void PopulateCraftingGrid()
    {
        if (craftingGrid == null || craftingButtonPrefab == null) return;

        // Clear previous buttons
        for (int i = craftingGrid.childCount - 1; i >= 0; i--)
        {
            Destroy(craftingGrid.GetChild(i).gameObject);
        }

        // Spawn button for each recipe
        foreach (Recipe recipe in allRecipes)
        {
            if (recipe == null || recipe.result == null) continue;

            GameObject buttonObj = Instantiate(craftingButtonPrefab, craftingGrid);

            // Set output icon using safe sprite lookup
            Image iconImage = buttonObj.transform.GetChild(0).GetComponent<Image>();
            if (iconImage != null)
            {
                Sprite resultIcon = GetSpriteFromItemData(recipe.result);
                if (resultIcon != null)
                {
                    iconImage.sprite = resultIcon;
                    iconImage.enabled = true;
                }
                else
                {
                    iconImage.enabled = false;
                }
            }

            // Setup button interactable and click event
            Button btn = buttonObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = CanCraft(recipe);
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => Craft(recipe));
            }
        }
    }

    public bool CanCraft(Recipe recipe)
    {
        if (recipe == null || hotbarManager == null) return false;

        // SAFE WORKBENCH CHECK
        if (RequiresWorkbenchCheck(recipe) && !isAtWorkbench)
        {
            return false;
        }

        foreach (Ingredients ing in recipe.ingredients)
        {
            if (ing.item == null) continue;

            int totalFound = GetItemCountFromHotbar(ing.item);

            if (totalFound < ing.amount)
            {
                return false;
            }
        }
        return true;
    }

    public void Craft(Recipe recipe)
    {
        if (!CanCraft(recipe)) return;

        ConsumeIngredients(recipe);

        // Uses existing Hotbar add method
        if (hotbarManager != null)
        {
            hotbarManager.TryAddToHotbar(recipe.result, recipe.resultAmount);
            hotbarManager.NotifyHotbarChanged();
            hotbarManager.UpdateHandVisual();
        }

        PopulateCraftingGrid(); // Refresh UI button states
    }

    private void ConsumeIngredients(Recipe recipe)
    {
        foreach (Ingredients ing in recipe.ingredients)
        {
            if (ing.item != null)
            {
                RemoveItemFromHotbar(ing.item, ing.amount);
            }
        }
    }

    // --- Helper Methods ---

    private Sprite GetSpriteFromItemData(ItemDataSO data)
    {
        if (data == null) return null;

        var dataType = data.GetType();

        FieldInfo field = dataType.GetField("icon") ?? dataType.GetField("itemIcon") ?? dataType.GetField("sprite");
        if (field != null)
        {
            return field.GetValue(data) as Sprite;
        }

        PropertyInfo prop = dataType.GetProperty("icon") ?? dataType.GetProperty("itemIcon") ?? dataType.GetProperty("sprite");
        if (prop != null)
        {
            return prop.GetValue(data) as Sprite;
        }

        return null;
    }

    private bool RequiresWorkbenchCheck(Recipe recipe)
    {
        var field = recipe.GetType().GetField("requiresWorkbench");
        if (field != null && field.FieldType == typeof(bool))
        {
            return (bool)field.GetValue(recipe);
        }
        return false;
    }

    private int GetItemCountFromHotbar(ItemDataSO targetItem)
    {
        int count = 0;
        if (hotbarManager == null || hotbarManager.hotbarSlots == null) return 0;

        foreach (var slot in hotbarManager.hotbarSlots)
        {
            if (slot != null && slot.GetItemData() == targetItem)
            {
                count += slot.GetItemCount();
            }
        }
        return count;
    }

    private void RemoveItemFromHotbar(ItemDataSO targetItem, int amountToRemove)
    {
        if (hotbarManager == null || hotbarManager.hotbarSlots == null) return;

        int remainingToRemove = amountToRemove;

        foreach (var slot in hotbarManager.hotbarSlots)
        {
            if (remainingToRemove <= 0) break;

            if (slot != null && slot.GetItemData() == targetItem)
            {
                int slotCount = slot.GetItemCount();

                if (slotCount <= remainingToRemove)
                {
                    remainingToRemove -= slotCount;
                    ClearSlotSafely(slot);
                }
                else
                {
                    ReduceCountSafely(slot, remainingToRemove);
                    remainingToRemove = 0;
                }
            }
        }
    }

    private void ClearSlotSafely(HotbarSlot slot)
    {
        if (slot == null) return;

        // Try standard ClearSlot method
        var clearMethod = slot.GetType().GetMethod("ClearSlot");
        if (clearMethod != null)
        {
            clearMethod.Invoke(slot, null);
            return;
        }

        // Fallback to InventoryItem ClearItem if attached to slot
        InventoryItem item = slot.GetComponentInChildren<InventoryItem>();
        if (item != null)
        {
            item.ClearItem();
        }
    }

    private void ReduceCountSafely(HotbarSlot slot, int amount)
    {
        if (slot == null) return;

        // Try standard ReduceCount method
        var reduceMethod = slot.GetType().GetMethod("ReduceCount");
        if (reduceMethod != null)
        {
            reduceMethod.Invoke(slot, new object[] { amount });
            return;
        }

        // Fallback to InventoryItem RemoveFromStack if attached to slot
        InventoryItem item = slot.GetComponentInChildren<InventoryItem>();
        if (item != null)
        {
            item.RemoveFromStack(amount);
        }
    }
}