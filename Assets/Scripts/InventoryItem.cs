using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    // =========================================================
    // ITEM STATE
    // =========================================================

    [Header("Item State")]
    public int currentItemID = -1;
    public ItemDataSO currentItemData;
    public int stackCount = 0;


    // =========================================================
    // UI REFERENCES
    // =========================================================

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI countText;

    private Image slotImage;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Canvas rootCanvas;

    private bool isBeingDragged = false;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        slotImage = GetComponent<Image>();

        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        rootCanvas = GetComponentInParent<Canvas>();

        UpdateUI();
    }


    private void Update()
    {
        if (isBeingDragged)
        {
            if (Mouse.current != null)
            {
                transform.position = Mouse.current.position.ReadValue();
            }
        }
    }


    // =========================================================
    // GETTERS
    // =========================================================

    public int GetItemID()
    {
        return currentItemID;
    }


    public ItemDataSO GetItemData()
    {
        return currentItemData;
    }


    public int GetStackCount()
    {
        return stackCount;
    }


    // =========================================================
    // SET ITEM
    // =========================================================

    public void SetupItem(ItemDataSO data, int amount = 1)
    {
        if (data == null)
        {
            ClearItem();
            return;
        }

        currentItemData = data;
        currentItemID = data.itemID;

        // Make sure amount is valid.
        stackCount = Mathf.Max(1, amount);

        // Respect maximum stack size.
        int maxStack = data.isStackable
            ? Mathf.Max(1, data.maxStackSize)
            : 1;

        stackCount = Mathf.Min(stackCount, maxStack);

        // Update icon.
        if (slotImage != null)
        {
            Sprite iconSprite = GetSpriteFromItemData(data);

            if (iconSprite != null)
            {
                slotImage.sprite = iconSprite;
                slotImage.color = Color.white;
                slotImage.enabled = true;
                slotImage.raycastTarget = true;
            }
            else
            {
                slotImage.sprite = null;
                slotImage.enabled = false;
                slotImage.raycastTarget = false;
            }
        }

        UpdateUI();
    }


    // =========================================================
    // GET ITEM ICON
    // =========================================================

    private Sprite GetSpriteFromItemData(ItemDataSO data)
    {
        if (data == null)
        {
            return null;
        }

        // Your ItemDataSO already has:
        // public Sprite itemIcon;
        return data.itemIcon;
    }


    // =========================================================
    // ADD TO STACK
    // =========================================================

    public int AddToStack(int amountToAdd)
    {
        if (currentItemData == null)
        {
            return amountToAdd;
        }

        if (amountToAdd <= 0)
        {
            return 0;
        }

        int maxStack = currentItemData.isStackable
            ? Mathf.Max(1, currentItemData.maxStackSize)
            : 1;

        int spaceLeft = maxStack - stackCount;

        if (spaceLeft <= 0)
        {
            return amountToAdd;
        }

        int added = Mathf.Min(amountToAdd, spaceLeft);

        stackCount += added;

        UpdateUI();

        return amountToAdd - added;
    }


    // =========================================================
    // REMOVE FROM STACK
    // =========================================================

    public bool RemoveFromStack(int amountToRemove = 1)
    {
        if (amountToRemove <= 0)
        {
            return false;
        }

        if (stackCount < amountToRemove)
        {
            return false;
        }

        stackCount -= amountToRemove;

        if (stackCount <= 0)
        {
            ClearItem();
        }
        else
        {
            UpdateUI();
        }

        return true;
    }


    // =========================================================
    // CLEAR ITEM
    // =========================================================

    public void ClearItem()
    {
        currentItemID = -1;
        currentItemData = null;
        stackCount = 0;

        if (slotImage != null)
        {
            slotImage.sprite = null;

            Color c = slotImage.color;
            c.a = 0f;
            slotImage.color = c;

            slotImage.enabled = false;
            slotImage.raycastTarget = false;
        }

        if (UIDescriptionPanel.Instance != null)
        {
            UIDescriptionPanel.Instance.Hide();
        }

        UpdateUI();
    }


    // =========================================================
    // UPDATE UI
    // =========================================================

    private void UpdateUI()
    {
        if (countText != null)
        {
            if (stackCount > 1)
            {
                countText.text = stackCount.ToString();
            }
            else
            {
                countText.text = "";
            }
        }
    }


    // =========================================================
    // DRAG START
    // =========================================================

    public void OnPointerDown(PointerEventData eventData)
    {
        if (currentItemID == -1 || currentItemData == null)
        {
            return;
        }

        if (UIDescriptionPanel.Instance != null)
        {
            UIDescriptionPanel.Instance.Hide();
        }

        isBeingDragged = true;

        originalParent = transform.parent;

        if (rootCanvas == null)
        {
            rootCanvas = GetComponentInParent<Canvas>();
        }

        if (rootCanvas != null)
        {
            transform.SetParent(rootCanvas.transform, true);
        }

        transform.localScale = Vector3.one;
        transform.SetAsLastSibling();

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
        }
    }


    // =========================================================
    // DRAG END
    // =========================================================

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isBeingDragged)
        {
            return;
        }

        isBeingDragged = false;

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }

        GameObject targetUI = null;

        if (eventData != null)
        {
            targetUI = eventData.pointerCurrentRaycast.gameObject;
        }

        // =====================================================
        // TRY TO PLACE ITEM INTO ANOTHER INVENTORY SLOT
        // =====================================================

        if (targetUI != null)
        {
            InventoryItem targetSlotItem =
                targetUI.GetComponent<InventoryItem>();

            if (targetSlotItem == null)
            {
                targetSlotItem =
                    targetUI.GetComponentInChildren<InventoryItem>();
            }

            if (targetSlotItem == null)
            {
                targetSlotItem =
                    targetUI.GetComponentInParent<InventoryItem>();
            }

            // Try finding InventoryItem through HotbarSlot.
            if (targetSlotItem == null)
            {
                HotbarSlot baseSlot =
                    targetUI.GetComponentInParent<HotbarSlot>();

                if (baseSlot != null)
                {
                    targetSlotItem =
                        baseSlot.GetComponentInChildren<InventoryItem>();
                }
            }

            // =================================================
            // TARGET SLOT FOUND
            // =================================================

            if (targetSlotItem != null &&
                targetSlotItem != this)
            {
                // ---------------------------------------------
                // TARGET IS EMPTY
                // ---------------------------------------------

                if (targetSlotItem.currentItemID == -1)
                {
                    targetSlotItem.SetupItem(
                        currentItemData,
                        stackCount
                    );

                    ClearItem();
                }

                // ---------------------------------------------
                // SAME ITEM - STACK
                // ---------------------------------------------

                else if (
                    targetSlotItem.currentItemID == currentItemID &&
                    currentItemData != null &&
                    currentItemData.isStackable
                )
                {
                    int leftover =
                        targetSlotItem.AddToStack(stackCount);

                    if (leftover <= 0)
                    {
                        ClearItem();
                    }
                    else
                    {
                        stackCount = leftover;
                        UpdateUI();
                    }
                }

                // ---------------------------------------------
                // DIFFERENT ITEM - SWAP
                // ---------------------------------------------

                else
                {
                    ItemDataSO targetData =
                        targetSlotItem.currentItemData;

                    int targetCount =
                        targetSlotItem.stackCount;

                    ItemDataSO ourData =
                        currentItemData;

                    int ourCount =
                        stackCount;

                    targetSlotItem.SetupItem(
                        ourData,
                        ourCount
                    );

                    SetupItem(
                        targetData,
                        targetCount
                    );
                }

                ReturnToOriginalSlot();

                NotifyHotbarSystem();

                return;
            }
        }


        // =====================================================
        // DROP ITEM INTO WORLD
        // =====================================================

        bool pointerOverUI =
            EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject();

        if (targetUI == null || !pointerOverUI)
        {
            int amountToDrop = stackCount;

            // SHIFT = DROP ONE
            if (Keyboard.current != null &&
                (Keyboard.current.leftShiftKey.isPressed ||
                 Keyboard.current.rightShiftKey.isPressed))
            {
                amountToDrop = 1;
            }

            // CTRL = DROP HALF
            else if (Keyboard.current != null &&
                     (Keyboard.current.leftCtrlKey.isPressed ||
                      Keyboard.current.rightCtrlKey.isPressed))
            {
                amountToDrop = Mathf.Max(1, stackCount / 2);
            }

            DropItemIntoWorld(amountToDrop);
        }
        else
        {
            ReturnToOriginalSlot();
        }

        NotifyHotbarSystem();
    }


    // =========================================================
    // MOUSE HOVER
    // =========================================================

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentItemID == -1 ||
            isBeingDragged ||
            UIDescriptionPanel.Instance == null)
        {
            return;
        }

        if (currentItemData != null)
        {
            string description =
                currentItemData.description;

            UIDescriptionPanel.Instance.Show(
                currentItemData.itemName,
                description
            );
        }
    }


    public void OnPointerExit(PointerEventData eventData)
    {
        if (UIDescriptionPanel.Instance != null)
        {
            UIDescriptionPanel.Instance.Hide();
        }
    }


    // =========================================================
    // RETURN TO ORIGINAL SLOT
    // =========================================================

    private void ReturnToOriginalSlot()
    {
        if (originalParent != null)
        {
            transform.SetParent(
                originalParent,
                false
            );
        }

        RectTransform rect =
            GetComponent<RectTransform>();

        if (rect != null)
        {
            rect.anchoredPosition = Vector2.zero;
        }

        transform.localScale = Vector3.one;
    }


    // =========================================================
    // NOTIFY HOTBAR
    // =========================================================

    private void NotifyHotbarSystem()
    {
        HotbarManager manager =
            FindFirstObjectByType<HotbarManager>();

        if (manager != null)
        {
            manager.NotifyHotbarChanged();

            // Refresh the held item after moving things between hotbar and inventory.
            manager.UpdateHandVisual();
        }
    }


    // =========================================================
    // DROP ITEM INTO WORLD
    // =========================================================

    public void DropItemIntoWorld(int amountToDrop)
    {
        if (currentItemData == null)
        {
            ReturnToOriginalSlot();
            NotifyHotbarSystem();
            return;
        }

        if (currentItemData.physicalPrefab == null)
        {
            ReturnToOriginalSlot();
            NotifyHotbarSystem();
            return;
        }

        if (amountToDrop <= 0)
        {
            return;
        }

        amountToDrop =
            Mathf.Min(amountToDrop, stackCount);

        GameObject playerObj =
            GameObject.FindWithTag("Player");

        if (playerObj == null)
        {
            Debug.LogWarning(
                "[InventoryItem] Player with tag 'Player' not found."
            );

            ReturnToOriginalSlot();
            NotifyHotbarSystem();
            return;
        }

        Transform playerTransform =
            playerObj.transform;

        Collider playerCollider =
            playerObj.GetComponent<Collider>();


        // =====================================================
        // CREATE DROPPED ITEMS
        // =====================================================

        for (int i = 0; i < amountToDrop; i++)
        {
            Vector3 randomOffset =
                new Vector3(
                    Random.Range(-0.4f, 0.4f),
                    0f,
                    Random.Range(-0.4f, 0.4f)
                );

            Vector3 targetSpawnPos =
                playerTransform.position +
                (playerTransform.forward * 1.8f) +
                randomOffset +
                (Vector3.up * 0.8f);


            // Find ground.
            if (Physics.Raycast(
                    targetSpawnPos,
                    Vector3.down,
                    out RaycastHit hit,
                    10f))
            {
                targetSpawnPos =
                    hit.point +
                    (Vector3.up * 0.2f);
            }


            Quaternion randomRotation =
                currentItemData.physicalPrefab.transform.rotation *
                Quaternion.Euler(
                    0f,
                    Random.Range(0f, 360f),
                    0f
                );


            GameObject droppedObj =
                Instantiate(
                    currentItemData.physicalPrefab,
                    targetSpawnPos,
                    randomRotation
                );


            droppedObj.transform.localScale =
                currentItemData.physicalPrefab.transform.localScale;


            droppedObj.layer =
                currentItemData.physicalPrefab.layer;


            // Add Rigidbody if missing.
            if (droppedObj.GetComponent<Rigidbody>() == null)
            {
                droppedObj.AddComponent<Rigidbody>();
            }


            Collider itemCollider =
                droppedObj.GetComponent<Collider>();


            if (playerCollider != null &&
                itemCollider != null)
            {
                Physics.IgnoreCollision(
                    playerCollider,
                    itemCollider
                );
            }
        }


        // =====================================================
        // REMOVE FROM INVENTORY
        // =====================================================

        RemoveFromStack(amountToDrop);

        ReturnToOriginalSlot();

        NotifyHotbarSystem();
    }
}