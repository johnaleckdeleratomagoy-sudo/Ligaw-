using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction")]
    public float interactionDistance = 5f;
    public Camera playerCamera;

    [Header("Easy Resource Pickup")]
    [Tooltip("Distance at which logs, rocks and other Gatherables can be picked up.")]
    public float easyPickupDistance = 3f;

    [Tooltip("How often the game searches for nearby resources.")]
    public float pickupSearchInterval = 0.1f;

    [Header("Interaction UI")]
    public GameObject interactionTextUI;

    [Header("Interaction Text")]
    public UnityEngine.UI.Text interactionText;

    [Header("Optional TextMesh")]
    public TMPro.TextMeshProUGUI interactionTextMesh;

    [Header("Interaction Layers")]
    public LayerMask interactionLayers = ~0;

    [Header("Animation")]
    public Animator playerAnimator;

    [Header("Hotbar")]
    public HotbarManager hotbarManager;

    private Gatherable nearbyGatherable;

    private float nextPickupSearchTime;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (hotbarManager == null)
        {
            hotbarManager =
                FindFirstObjectByType<HotbarManager>();
        }

        HideInteractionUI();
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        CheckNearbyGatherable();

        CheckNormalInteraction();

        HandleHotbarSelection();

        HandleEasyPickup();

        UpdateAimDescription();
    }

    // =========================================================
    // DESCRIPTION PANEL WHEN AIMING AT A WORLD ITEM
    // =========================================================

    private bool aimDescriptionShown;

    private void UpdateAimDescription()
    {
        if (UIDescriptionPanel.Instance == null)
        {
            return;
        }

        // Cursor is free (inventory / menus open): inventory slot hover
        // owns the panel, so don't touch it.
        bool cursorFree =
            Cursor.lockState != CursorLockMode.Locked;

        ItemDataSO aimedItem = null;

        if (!cursorFree && playerCamera != null)
        {
            Ray ray =
                new Ray(
                    playerCamera.transform.position,
                    playerCamera.transform.forward
                );

            if (Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    interactionDistance,
                    interactionLayers,
                    QueryTriggerInteraction.Collide))
            {
                DroppedItem dropped =
                    hit.collider.GetComponentInParent<DroppedItem>();

                if (dropped != null)
                {
                    aimedItem = dropped.GetItemData();
                }
                else
                {
                    Gatherable gatherable =
                        hit.collider.GetComponentInParent<Gatherable>();

                    if (gatherable != null)
                    {
                        aimedItem = gatherable.itemDataAsset;
                    }
                }
            }
        }

        if (aimedItem != null)
        {
            UIDescriptionPanel.Instance.Show(
                aimedItem.itemName,
                aimedItem.description
            );

            aimDescriptionShown = true;
        }
        else if (aimDescriptionShown)
        {
            UIDescriptionPanel.Instance.Hide();
            aimDescriptionShown = false;
        }
    }

    // =========================================================
    // EASY NEARBY RESOURCE PICKUP
    // =========================================================

    private void CheckNearbyGatherable()
    {
        if (Time.time < nextPickupSearchTime)
        {
            return;
        }

        nextPickupSearchTime =
            Time.time + pickupSearchInterval;

        nearbyGatherable = null;

        Collider[] nearbyObjects =
            Physics.OverlapSphere(
                transform.position,
                easyPickupDistance,
                interactionLayers,
                QueryTriggerInteraction.Collide
            );

        float closestDistance =
            Mathf.Infinity;

        foreach (Collider col in nearbyObjects)
        {
            if (col == null)
            {
                continue;
            }

            // Ignore player
            if (col.gameObject == gameObject ||
                col.transform.IsChildOf(transform))
            {
                continue;
            }

            // =================================================
            // DROPPED ITEMS ARE NOT NORMAL GATHERABLES
            // =================================================

            DroppedItem droppedItem =
                col.GetComponentInParent<DroppedItem>();

            if (droppedItem != null)
            {
                continue;
            }

            // =================================================
            // NORMAL GATHERABLE
            // =================================================

            Gatherable gatherable =
                col.GetComponentInParent<Gatherable>();

            if (gatherable == null)
            {
                continue;
            }

            if (!gatherable.gameObject.activeInHierarchy)
            {
                continue;
            }

            float distance =
                Vector3.Distance(
                    transform.position,
                    gatherable.transform.position
                );

            if (distance <= easyPickupDistance &&
                distance < closestDistance)
            {
                closestDistance =
                    distance;

                nearbyGatherable =
                    gatherable;
            }
        }

        if (nearbyGatherable == null)
        {
            return;
        }

        ItemDataSO itemData =
            nearbyGatherable.itemDataAsset;

        if (itemData == null)
        {
            nearbyGatherable = null;
            HideInteractionUI();
            return;
        }

        ShowInteractionUI(
            "Press E to pick up " +
            itemData.itemName
        );
    }

    // =========================================================
    // EASY WORLD GATHERABLE PICKUP
    // =========================================================

    private void HandleEasyPickup()
    {
        if (nearbyGatherable == null)
        {
            return;
        }

        if (!nearbyGatherable.gameObject.activeInHierarchy)
        {
            nearbyGatherable = null;
            HideInteractionUI();
            return;
        }

        float distance =
            Vector3.Distance(
                transform.position,
                nearbyGatherable.transform.position
            );

        if (distance > easyPickupDistance)
        {
            nearbyGatherable = null;
            HideInteractionUI();
            return;
        }

        // NEW INPUT SYSTEM
        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            Debug.Log(
                "[PlayerInteraction] Easy pickup: " +
                nearbyGatherable.gameObject.name
            );

            TriggerPickupAnimation();

            Gatherable target =
                nearbyGatherable;

            nearbyGatherable = null;

            target.Gather();

            HideInteractionUI();
        }
    }

    // =========================================================
    // NORMAL CENTER-SCREEN INTERACTION
    // =========================================================

    private void CheckNormalInteraction()
    {
        if (playerCamera == null)
        {
            HideInteractionUI();
            return;
        }

        Ray ray =
            new Ray(
                playerCamera.transform.position,
                playerCamera.transform.forward
            );

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                interactionDistance,
                interactionLayers,
                QueryTriggerInteraction.Collide))
        {
            if (nearbyGatherable == null)
            {
                HideInteractionUI();
            }

            return;
        }

        // =====================================================
        // DROPPED ITEM
        // =====================================================

        DroppedItem droppedItem =
            hit.collider.GetComponentInParent<DroppedItem>();

        if (droppedItem != null)
        {
            HandleDroppedItem(droppedItem);
            return;
        }

        // =====================================================
        // TREE
        // =====================================================

        TreeResource tree =
            hit.collider.GetComponentInParent<TreeResource>();

        if (tree != null)
        {
            ShowInteractionUI(
                "Press E to gather"
            );

            // NEW INPUT SYSTEM
            bool pressE =
                Keyboard.current != null &&
                Keyboard.current.eKey.wasPressedThisFrame;

            bool pressMouse =
                Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame;

            if (pressE || pressMouse)
            {
                tree.HitTree(1);
            }

            return;
        }

        if (nearbyGatherable == null)
        {
            HideInteractionUI();
        }
    }

    // =========================================================
    // DROPPED ITEMS
    // =========================================================

    private void HandleDroppedItem(
        DroppedItem droppedItem)
    {
        if (droppedItem == null)
        {
            if (nearbyGatherable == null)
            {
                HideInteractionUI();
            }

            return;
        }

        if (!droppedItem.CanBePickedUp())
        {
            if (nearbyGatherable == null)
            {
                HideInteractionUI();
            }

            return;
        }

        ItemDataSO itemData =
            droppedItem.GetItemData();

        if (itemData == null)
        {
            if (nearbyGatherable == null)
            {
                HideInteractionUI();
            }

            return;
        }

        float distance =
            Vector3.Distance(
                transform.position,
                droppedItem.transform.position
            );

        if (distance > interactionDistance)
        {
            if (nearbyGatherable == null)
            {
                HideInteractionUI();
            }

            return;
        }

        ShowInteractionUI(
            "Press E to pick up " +
            itemData.itemName
        );

        // NEW INPUT SYSTEM
        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            PlayerInventory inventory =
                FindFirstObjectByType<PlayerInventory>();

            if (inventory == null)
            {
                Debug.LogWarning(
                    "[PlayerInteraction] PlayerInventory not found."
                );

                return;
            }

            TriggerPickupAnimation();

            bool pickedUp =
                inventory.PickUpDroppedItem(
                    droppedItem.gameObject,
                    itemData,
                    droppedItem.GetAmount()
                );

            if (pickedUp)
            {
                Debug.Log(
                    "[PlayerInteraction] Picked up dropped item WITHOUT " +
                    "adding to Day 1 task progress: " +
                    itemData.itemName
                );

                HideInteractionUI();
            }
        }
    }

    // =========================================================
    // HOTBAR
    // =========================================================

    private void HandleHotbarSelection()
    {
        if (hotbarManager == null)
        {
            return;
        }

        Keyboard keyboard =
            Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        // NEW INPUT SYSTEM

        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            hotbarManager.SelectSlot(0);
        }

        if (keyboard.digit2Key.wasPressedThisFrame)
        {
            hotbarManager.SelectSlot(1);
        }

        if (keyboard.digit3Key.wasPressedThisFrame)
        {
            hotbarManager.SelectSlot(2);
        }

        if (keyboard.digit4Key.wasPressedThisFrame)
        {
            hotbarManager.SelectSlot(3);
        }

        if (keyboard.digit5Key.wasPressedThisFrame)
        {
            hotbarManager.SelectSlot(4);
        }

        if (keyboard.digit6Key.wasPressedThisFrame)
        {
            hotbarManager.SelectSlot(5);
        }

        if (keyboard.digit7Key.wasPressedThisFrame)
        {
            hotbarManager.SelectSlot(6);
        }

        if (keyboard.digit8Key.wasPressedThisFrame)
        {
            hotbarManager.SelectSlot(7);
        }

        if (keyboard.digit9Key.wasPressedThisFrame)
        {
            hotbarManager.SelectSlot(8);
        }
    }

    // =========================================================
    // UI
    // =========================================================

    private void ShowInteractionUI(
        string message)
    {
        if (interactionTextUI != null)
        {
            interactionTextUI.SetActive(true);
        }

        if (interactionText != null)
        {
            interactionText.text =
                message;
        }

        if (interactionTextMesh != null)
        {
            interactionTextMesh.text =
                message;
        }
    }

    private void HideInteractionUI()
    {
        if (interactionTextUI != null)
        {
            interactionTextUI.SetActive(false);
        }

        if (interactionText != null)
        {
            interactionText.text = "";
        }

        if (interactionTextMesh != null)
        {
            interactionTextMesh.text = "";
        }
    }

    // =========================================================
    // ANIMATION
    // =========================================================

    private void TriggerPickupAnimation()
    {
        if (playerAnimator == null)
        {
            return;
        }

        playerAnimator.SetTrigger(
            "Pickup"
        );
    }

    // =========================================================
    // DEBUG
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            easyPickupDistance
        );
    }
}