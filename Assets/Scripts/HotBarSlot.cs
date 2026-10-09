using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HotbarSlot : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private InventoryItem innerItem;
    [SerializeField] private Image selectionHighlight;

    private HotbarManager hotbarManager;

    private void Awake()
    {
        if (innerItem == null)
        {
            innerItem =
                GetComponentInChildren<InventoryItem>();
        }

        hotbarManager =
            GetComponentInParent<HotbarManager>();

        if (hotbarManager == null)
        {
            hotbarManager =
                FindFirstObjectByType<HotbarManager>();
        }
    }

    public void OnPointerClick(
        PointerEventData eventData)
    {
        if (eventData.button ==
            PointerEventData.InputButton.Left &&
            eventData.clickCount == 2)
        {
            if (IsEmpty() ||
                hotbarManager == null)
            {
                return;
            }

            int slotIndex =
                hotbarManager.hotbarSlots.IndexOf(this);

            if (slotIndex != -1)
            {
                hotbarManager.ConsumeSlotItem(
                    slotIndex
                );
            }
        }
    }

    public void SetSelected(bool isActive)
    {
        if (selectionHighlight != null)
        {
            selectionHighlight.enabled =
                isActive;
        }
    }

    public bool IsEmpty()
    {
        return innerItem == null ||
               innerItem.GetItemID() == -1;
    }

    public void AssignPiece(
        ItemDataSO itemData,
        int amount = 1)
    {
        if (innerItem != null &&
            itemData != null)
        {
            innerItem.SetupItem(
                itemData,
                amount
            );
        }
    }

    public int AddToStack(
        int amountToAdd)
    {
        if (innerItem == null ||
            IsEmpty())
        {
            return amountToAdd;
        }

        return innerItem.AddToStack(
            amountToAdd
        );
    }

    public bool RemoveFromStack(
        int amountToRemove = 1)
    {
        if (innerItem == null ||
            IsEmpty())
        {
            return false;
        }

        return innerItem.RemoveFromStack(
            amountToRemove
        );
    }

    public int GetStoredPieceID()
    {
        return innerItem != null
            ? innerItem.GetItemID()
            : -1;
    }

    public void ClearSlot()
    {
        if (innerItem != null)
        {
            innerItem.ClearItem();
        }
    }

    public ItemDataSO GetItemData()
    {
        if (innerItem == null ||
            IsEmpty())
        {
            return null;
        }

        return innerItem.GetItemData();
    }

    public int GetItemCount()
    {
        if (innerItem == null ||
            IsEmpty())
        {
            return 0;
        }

        return innerItem.GetStackCount();
    }
}