using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

public class HotbarManager : MonoBehaviour
{
    [Header("Hotbar Slots")]
    public List<HotbarSlot> hotbarSlots =
        new List<HotbarSlot>();

    [Header("Hand Settings")]
    [Tooltip("Drag your Player's HandPoint Transform here.")]
    public Transform handPoint;

    [Header("Selection & Double-Tap Settings")]
    public int currentSelectedSlot = 0;

    [Header("Drop Settings")]
    [Tooltip("Key used to drop the selected item.")]
    public KeyCode dropKey = KeyCode.G;

    [Tooltip("How far in front of the player the item is dropped.")]
    public float dropDistance = 1.5f;

    [Tooltip("How strongly the dropped item is thrown forward.")]
    public float dropForce = 3f;

    [Tooltip("How long before the dropped item can be picked up again.")]
    public float pickupDelay = 0.5f;


    // =========================================================
    // SPEAR SETTINGS
    // =========================================================

    [Header("Spear Settings")]

    [Tooltip("How long the player must hold left click before charging starts.")]
    [SerializeField] private float spearChargeStartDelay = 0.15f;

    [Tooltip("Maximum time used to reach full spear throw charge.")]
    [SerializeField] private float spearChargeTime = 1.5f;

    [Tooltip("Distance in front of the camera where the thrown spear appears.")]
    [SerializeField] private float spearThrowDistance = 1.5f;

    [Tooltip("Vertical offset of the thrown spear from the camera.")]
    [SerializeField] private float spearThrowHeight = -0.1f;

    [Tooltip("How far the melee stab can hit an animal.")]
    [SerializeField] private float spearAttackDistance = 2.5f;

    [Tooltip("Damage dealt by a normal spear stab.")]
    [SerializeField] private int spearAttackDamage = 25;

    [Header("Spear Trajectory")]
    [Tooltip("Drag the separate SpearTrajectory component here.")]
    [SerializeField] private SpearTrajectory spearTrajectory;

    // These values are also used by the actual spear throw.
    // Keep them the same as the values in SpearTrajectory.cs.
    [Header("Spear Aim")]
    [Tooltip("Normalized X position of the aiming point on screen.")]
    [SerializeField] private float spearAimScreenX = 0.5f;

    [Tooltip("Normalized Y position of the aiming point on screen. 0 = bottom, 1 = top.")]
    [SerializeField] private float spearAimScreenY = 0.603f;




    // =========================================================
    // SPEAR STATE
    // =========================================================

    private bool isSpearButtonHeld = false;
    private bool isChargingSpear = false;

    private float spearChargeStartTime = 0f;
    private float spearChargeAmount = 0f;

    private PlayerMovement playerMovement;

    private SurvivalUIManager survivalUI;

    private GameObject currentHeldObject;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (handPoint == null)
        {
            GameObject player =
                GameObject.FindWithTag("Player");

            if (player != null)
            {
                Transform found =
                    player.transform.Find("HandPoint");

                if (found != null)
                {
                    handPoint = found;
                }
            }
        }

        FindPlayerMovement();

        if (spearTrajectory == null)
        {
            spearTrajectory =
                FindFirstObjectByType<SpearTrajectory>();
        }

        if (hotbarSlots.Count > 0)
        {
            SelectSlot(currentSelectedSlot);
        }
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        HandleNumberKeyInput();

        if (GameInput.Pressed("DropItem", dropKey))
        {
            DropSelectedItem();
        }

        HandleSpearInput();

        HandleConsumeInput();
    }


    // =========================================================
    // FIND PLAYER MOVEMENT
    // =========================================================

    private void FindPlayerMovement()
    {
        if (playerMovement != null)
            return;

        GameObject player =
            GameObject.FindWithTag("Player");

        if (player == null)
        {
            Debug.LogWarning(
                "[HotbarManager] Player tag not found."
            );

            return;
        }

        playerMovement =
            player.GetComponent<PlayerMovement>();

        if (playerMovement == null)
        {
            playerMovement =
                player.GetComponentInChildren<PlayerMovement>();
        }

        if (playerMovement == null)
        {
            Debug.LogWarning(
                "[HotbarManager] PlayerMovement was not found."
            );
        }
    }


    // =========================================================
    // NUMBER KEY INPUT
    // =========================================================

    private void HandleNumberKeyInput()
    {
        for (int i = 0; i < 9; i++)
        {
            KeyCode key =
                KeyCode.Alpha1 + i;

            if (Input.GetKeyDown(key))
            {
                int slotIndex = i;

                SelectSlot(slotIndex);

                break;
            }
        }
    }


    // =========================================================
    // HOLD LEFT CLICK TO EAT / DRINK
    // =========================================================

    [Header("Eat / Drink")]
    [Tooltip("Seconds the left mouse button must be held to eat or drink.")]
    [SerializeField] private float consumeHoldTime = 1.5f;

    [Tooltip("Optional progress circle shown while holding.")]
    [SerializeField] private HoldProgressUI consumeProgressUI;

    private float consumeHoldTimer = 0f;
    private bool consumeHoldActive = false;

    private void HandleConsumeInput()
    {
        if (Mouse.current == null)
            return;

        // Ignore input while the cursor is free
        // (inventory, pause menu, etc. are open).
        bool canHold =
            Mouse.current.leftButton.isPressed &&
            Cursor.lockState == CursorLockMode.Locked &&
            IsSelectedSlotConsumable();

        if (!canHold)
        {
            CancelConsumeHold();
            return;
        }

        // Require a fresh press so a held attack
        // click doesn't start eating by accident.
        if (!consumeHoldActive)
        {
            if (!Mouse.current.leftButton.wasPressedThisFrame)
                return;

            consumeHoldActive = true;
            consumeHoldTimer = 0f;
        }

        consumeHoldTimer += Time.deltaTime;

        if (consumeProgressUI != null)
        {
            consumeProgressUI.ShowUI(
                consumeHoldTimer / consumeHoldTime
            );
        }

        if (consumeHoldTimer >= consumeHoldTime)
        {
            TryConsumeSlotItem(
                currentSelectedSlot
            );

            // Must release and press again to eat another.
            CancelConsumeHold();
        }
    }

    private void CancelConsumeHold()
    {
        consumeHoldActive = false;
        consumeHoldTimer = 0f;

        if (consumeProgressUI != null)
        {
            consumeProgressUI.HideUI();
        }
    }

    private bool IsSelectedSlotConsumable()
    {
        if (currentSelectedSlot < 0 ||
            currentSelectedSlot >= hotbarSlots.Count)
        {
            return false;
        }

        HotbarSlot slot =
            hotbarSlots[currentSelectedSlot];

        if (slot == null)
            return false;

        InventoryItem itemUI =
            slot.GetComponentInChildren<
                InventoryItem>();

        if (itemUI == null)
            return false;

        return IsItemConsumable(
            ExtractItemData(itemUI)
        );
    }


    // =========================================================
    // SPEAR INPUT
    // =========================================================

    private void HandleSpearInput()
    {
        if (Mouse.current == null)
            return;

        if (!IsHoldingSpear())
        {
            if (isSpearButtonHeld ||
                isChargingSpear)
            {
                CancelSpearCharge();
            }

            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            StartSpearInput();
        }

        if (Mouse.current.leftButton.isPressed)
        {
            UpdateSpearInput();
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            ReleaseSpearInput();
        }
    }


    // =========================================================
    // START SPEAR INPUT
    // =========================================================

    private void StartSpearInput()
    {
        if (!IsHoldingSpear())
            return;

        isSpearButtonHeld = true;
        isChargingSpear = false;

        spearChargeAmount = 0f;
        spearChargeStartTime = Time.time;

        FindPlayerMovement();
    }


    // =========================================================
    // UPDATE SPEAR INPUT
    // =========================================================

    private void UpdateSpearInput()
    {
        if (!isSpearButtonHeld)
            return;

        float heldTime =
            Time.time -
            spearChargeStartTime;

        if (!isChargingSpear)
        {
            if (heldTime >= spearChargeStartDelay)
            {
                BeginSpearCharge();
            }

            return;
        }

        spearChargeAmount =
            Mathf.Clamp01(
                (heldTime -
                 spearChargeStartDelay) /
                spearChargeTime
            );

        if (spearTrajectory != null)
        {
            spearTrajectory.SetCharge(
                spearChargeAmount
            );
        }

        if (playerMovement != null)
        {
            playerMovement.SetSpearCharging(true);
        }
    }


    // =========================================================
    // BEGIN SPEAR CHARGE
    // =========================================================

    private void BeginSpearCharge()
    {
        if (!IsHoldingSpear())
            return;

        isChargingSpear = true;

        spearChargeStartTime =
            Time.time;

        spearChargeAmount = 0f;

        FindPlayerMovement();

        if (playerMovement != null)
        {
            playerMovement.SetSpearCharging(true);
        }

        if (spearTrajectory != null)
        {
            spearTrajectory.BeginTrajectory(0f);
        }

        Debug.Log(
            "[HotbarManager] Spear charging started."
        );
    }


    // =========================================================
    // RELEASE SPEAR INPUT
    // =========================================================

    private void ReleaseSpearInput()
    {
        if (!isSpearButtonHeld)
            return;

        isSpearButtonHeld = false;

        if (isChargingSpear)
        {
            spearChargeAmount =
                Mathf.Clamp01(
                    spearChargeAmount
                );

            ThrowSpear(
                spearChargeAmount
            );

            return;
        }

        PerformSpearAttack();
    }


    // =========================================================
    // CANCEL SPEAR CHARGE
    // =========================================================

    private void CancelSpearCharge()
    {
        isSpearButtonHeld = false;
        isChargingSpear = false;
        spearChargeAmount = 0f;

        if (spearTrajectory != null)
        {
            spearTrajectory.HideTrajectory();
        }

        if (playerMovement != null)
        {
            playerMovement.SetSpearCharging(false);
        }
    }


    // =========================================================
    // SPEAR ATTACK / STAB
    // =========================================================

    private void PerformSpearAttack()
    {
        if (!IsHoldingSpear())
            return;

        FindPlayerMovement();

        if (playerMovement != null)
        {
            playerMovement.SetSpearCharging(false);
            playerMovement.TriggerSpearAttack();
        }

        Camera playerCamera =
            GetPlayerCamera();

        if (playerCamera == null)
        {
            Debug.LogWarning(
                "[HotbarManager] Player camera not found for spear attack."
            );

            return;
        }

        Ray ray =
            new Ray(
                playerCamera.transform.position,
                playerCamera.transform.forward
            );

        RaycastHit hit;

        if (Physics.Raycast(
            ray,
            out hit,
            spearAttackDistance))
        {
            TryDamageAnimal(
                hit.collider.gameObject
            );

            Debug.Log(
                "[HotbarManager] Spear stab hit: " +
                hit.collider.gameObject.name
            );
        }
        else
        {
            Debug.Log(
                "[HotbarManager] Spear stab missed."
            );
        }

        Debug.Log(
            "[HotbarManager] SPEAR ATTACK"
        );
    }


    // =========================================================
    // DAMAGE ANIMAL
    // =========================================================

    private bool TryDamageAnimal(
        GameObject hitObject)
    {
        if (hitObject == null)
            return false;

        BearAI bear =
            hitObject.GetComponentInParent<BearAI>();

        if (bear != null)
        {
            bear.TakeDamage(
                spearAttackDamage,
                hitObject.transform.position
            );

            Debug.Log(
                "[HotbarManager] Spear damaged Bear for " +
                spearAttackDamage
            );

            return true;
        }

        WildBoarAI boar =
            hitObject.GetComponentInParent<WildBoarAI>();

        if (boar != null)
        {
            boar.TakeDamage(
                spearAttackDamage
            );

            Debug.Log(
                "[HotbarManager] Spear damaged Wild Boar for " +
                spearAttackDamage
            );

            return true;
        }

        DeerAI deer =
            hitObject.GetComponentInParent<DeerAI>();

        if (deer != null)
        {
            deer.TakeDamage(
                spearAttackDamage
            );

            Debug.Log(
                "[HotbarManager] Spear damaged Deer for " +
                spearAttackDamage
            );

            return true;
        }

        return false;
    }


    // =========================================================
    // THROW SPEAR
    // =========================================================

    private void ThrowSpear(float chargeAmount)
    {
        if (spearTrajectory != null)
        {
            spearTrajectory.HideTrajectory();
        }

        if (!IsHoldingSpear())
        {
            CancelSpearCharge();
            return;
        }

        HotbarSlot slot =
            GetCurrentSlot();

        if (slot == null)
        {
            CancelSpearCharge();
            return;
        }

        InventoryItem itemUI =
            slot.GetComponentInChildren<InventoryItem>();

        if (itemUI == null)
        {
            CancelSpearCharge();
            return;
        }

        ItemDataSO itemData =
            ExtractItemData(itemUI);

        if (itemData == null ||
            itemData.physicalPrefab == null)
        {
            Debug.LogWarning(
                "[HotbarManager] Spear ItemDataSO or physicalPrefab is missing."
            );

            CancelSpearCharge();
            return;
        }

        Camera playerCamera =
            GetPlayerCamera();

        if (playerCamera == null)
        {
            Debug.LogWarning(
                "[HotbarManager] Player camera not found."
            );

            CancelSpearCharge();
            return;
        }


        // =====================================================
        // FREE AIM FROM THE ACTUAL SCREEN AIMING POINT
        // =====================================================

        Vector2 aimScreenPosition =
            new Vector2(
                Screen.width * spearAimScreenX,
                Screen.height * spearAimScreenY
            );

        Ray aimRay =
            playerCamera.ScreenPointToRay(
                aimScreenPosition
            );

        // IMPORTANT:
        // Use the camera ray direction directly.
        // This makes the spear travel exactly where the crosshair points.
        // Do NOT calculate direction from the spear spawn position to
        // an aim point, because third-person parallax can offset it.
        Vector3 cameraForward =
            aimRay.direction.normalized;

        // Spawn the spear at the player's hand and aim it at whatever
        // the crosshair points at. Uses the same code as the trajectory
        // line so the two always match.
        Vector3 spawnPosition =
            playerCamera.transform.position +
            playerCamera.transform.forward *
            spearThrowDistance +
            playerCamera.transform.up *
            spearThrowHeight;

        if (spearTrajectory != null)
        {
            spawnPosition =
                spearTrajectory.GetStartPosition(playerCamera);

            cameraForward =
                spearTrajectory.GetAimDirection(
                    playerCamera,
                    spawnPosition
                );
        }


        // =====================================================
        // DEBUG
        // =====================================================

        Debug.DrawRay(
            aimRay.origin,
            aimRay.direction * 20f,
            Color.green,
            5f
        );

        Debug.DrawRay(
            spawnPosition,
            cameraForward * 20f,
            Color.red,
            5f
        );

        Debug.Log(
            "[HotbarManager] SPEAR AIM DIRECTION = " +
            cameraForward
        );


        // =====================================================
        // STOP CHARGING
        // =====================================================

        if (playerMovement != null)
        {
            playerMovement.SetSpearCharging(false);
            playerMovement.TriggerSpearThrow();
        }


        // =====================================================
        // CREATE SPEAR
        // =====================================================

        GameObject thrownSpear =
            Instantiate(
                itemData.physicalPrefab,
                spawnPosition,
                Quaternion.LookRotation(
                    cameraForward,
                    playerCamera.transform.up
                )
            );

        thrownSpear.name =
            itemData.itemName +
            "_Thrown";


        // =====================================================
        // REMOVE GATHERABLE IF PRESENT
        // =====================================================

        Gatherable gatherable =
            thrownSpear.GetComponent<Gatherable>();

        if (gatherable != null)
        {
            Destroy(gatherable);
        }


        // =====================================================
        // ENABLE COLLIDERS
        // =====================================================

        Collider[] colliders =
            thrownSpear
                .GetComponentsInChildren<Collider>();

        foreach (Collider col in colliders)
        {
            if (col != null)
            {
                col.enabled = true;
            }
        }


        // =====================================================
        // RIGIDBODY
        // =====================================================

        Rigidbody rb =
            thrownSpear.GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb =
                thrownSpear.AddComponent<Rigidbody>();
        }

        rb.isKinematic = true;
        rb.useGravity = false;

        rb.linearVelocity =
            Vector3.zero;

        rb.angularVelocity =
            Vector3.zero;


        // =====================================================
        // IGNORE PLAYER COLLISION
        // =====================================================

        IgnorePlayerCollisions(
            thrownSpear
        );


        // =====================================================
        // SPEAR THROW COMPONENT
        // =====================================================

        SpearThrow spearThrow =
            thrownSpear.GetComponent<SpearThrow>();

        if (spearThrow == null)
        {
            spearThrow =
                thrownSpear
                    .GetComponentInChildren<SpearThrow>();
        }

        if (spearThrow == null)
        {
            Debug.LogError(
                "[HotbarManager] The spear physicalPrefab does not have SpearThrow."
            );

            Destroy(
                thrownSpear
            );

            CancelSpearCharge();

            return;
        }


        spearThrow.spearItemData =
            itemData;


        // =====================================================
        // THROW USING CAMERA FORWARD
        // =====================================================

        spearThrow.Throw(
            cameraForward,
            chargeAmount
        );


        // =====================================================
        // REMOVE FROM HOTBAR
        // =====================================================

        itemUI.RemoveFromStack(1);

        NotifyHotbarChanged();

        UpdateHandVisual();

        isSpearButtonHeld = false;
        isChargingSpear = false;
        spearChargeAmount = 0f;
    }


    // =========================================================
    // GET CURRENT SLOT
    // =========================================================

    public HotbarSlot GetCurrentSlot()
    {
        if (currentSelectedSlot < 0 ||
            currentSelectedSlot >=
            hotbarSlots.Count)
        {
            return null;
        }

        return hotbarSlots[
            currentSelectedSlot
        ];
    }


    // =========================================================
    // GET PLAYER CAMERA
    // =========================================================

    private Camera GetPlayerCamera()
    {
        // IMPORTANT:
        // Use the camera that is actually rendering the screen first.
        // This is especially important when using Cinemachine / third-person cameras.
        // A Camera component inside the Player can be inactive or not be the camera
        // the player is currently looking through.
        Camera mainCamera = Camera.main;

        if (mainCamera != null && mainCamera.isActiveAndEnabled)
        {
            return mainCamera;
        }

        // Fallback to a camera under the Player.
        GameObject player =
            GameObject.FindWithTag("Player");

        if (player != null)
        {
            Camera playerCamera =
                player.GetComponentInChildren<Camera>();

            if (playerCamera != null && playerCamera.isActiveAndEnabled)
            {
                return playerCamera;
            }
        }

        return mainCamera;
    }


    // =========================================================
    // IGNORE PLAYER COLLISIONS
    // =========================================================

    private void IgnorePlayerCollisions(
        GameObject spearObject)
    {
        if (spearObject == null)
            return;

        GameObject player =
            GameObject.FindWithTag("Player");

        if (player == null)
            return;

        Collider[] spearColliders =
            spearObject
                .GetComponentsInChildren<Collider>();

        Collider[] playerColliders =
            player
                .GetComponentsInChildren<Collider>();

        foreach (
            Collider spearCollider
            in spearColliders)
        {
            if (spearCollider == null)
                continue;

            foreach (
                Collider playerCollider
                in playerColliders)
            {
                if (playerCollider == null)
                    continue;

                Physics.IgnoreCollision(
                    spearCollider,
                    playerCollider,
                    true
                );
            }
        }
    }


    // =========================================================
    // CHECK IF HOLDING SPEAR
    // =========================================================

    public bool IsHoldingSpear()
    {
        HotbarSlot slot =
            GetCurrentSlot();

        if (slot == null)
            return false;

        InventoryItem itemUI =
            slot.GetComponentInChildren<
                InventoryItem>();

        if (itemUI == null)
            return false;

        ItemDataSO data =
            ExtractItemData(itemUI);

        if (data == null)
            return false;

        if (data.itemName == null)
            return false;

        return data.itemName
            .ToLower()
            .Contains("spear");
    }


    // =========================================================
    // SELECT SLOT
    // =========================================================

    public void SelectSlot(int index)
    {
        if (index < 0 ||
            index >= hotbarSlots.Count)
        {
            return;
        }

        CancelSpearCharge();

        currentSelectedSlot =
            index;

        for (
            int i = 0;
            i < hotbarSlots.Count;
            i++)
        {
            if (hotbarSlots[i] != null)
            {
                hotbarSlots[i].SetSelected(
                    i == currentSelectedSlot
                );
            }
        }

        UpdateHandVisual();
    }


    // =========================================================
    // UPDATE HAND VISUAL
    // =========================================================

    public void UpdateHandVisual()
    {
        ClearHandVisual();

        if (handPoint == null)
        {
            Debug.LogWarning(
                "[HotbarManager] HandPoint is NULL!"
            );

            return;
        }

        if (currentSelectedSlot < 0 ||
            currentSelectedSlot >=
            hotbarSlots.Count)
        {
            return;
        }

        HotbarSlot activeSlot =
            hotbarSlots[
                currentSelectedSlot
            ];

        if (activeSlot == null)
            return;

        InventoryItem itemUI =
            activeSlot.GetComponentInChildren<
                InventoryItem>();

        if (itemUI == null)
            return;

        ItemDataSO itemData =
            ExtractItemData(itemUI);

        if (itemData == null)
            return;

        GameObject itemPrefab =
            itemData.pickupInHand != null
            ? itemData.pickupInHand
            : itemData.physicalPrefab;

        if (itemPrefab == null)
        {
            Debug.LogWarning(
                "[HotbarManager] Item '" +
                itemData.itemName +
                "' has no hand prefab."
            );

            return;
        }

        currentHeldObject =
            Instantiate(
                itemPrefab,
                handPoint
            );

        currentHeldObject.transform.localPosition =
            itemData.handPositionOffset;

        currentHeldObject.transform.localRotation =
            Quaternion.Euler(
                itemData.handRotationOffset
            );

        currentHeldObject.transform.localScale =
            itemData.handScale;

        SetLayerRecursively(
            currentHeldObject,
            handPoint.gameObject.layer
        );

        Collider[] heldColliders =
            currentHeldObject
                .GetComponentsInChildren<Collider>();

        foreach (Collider col in heldColliders)
        {
            if (col != null)
            {
                col.enabled = false;
            }
        }

        Rigidbody[] heldRigidbodies =
            currentHeldObject
                .GetComponentsInChildren<Rigidbody>();

        foreach (Rigidbody heldRb in heldRigidbodies)
        {
            if (heldRb != null)
            {
                heldRb.isKinematic = true;
                heldRb.useGravity = false;

                heldRb.linearVelocity =
                    Vector3.zero;

                heldRb.angularVelocity =
                    Vector3.zero;
            }
        }
    }


    // =========================================================
    // CLEAR HAND
    // =========================================================

    private void ClearHandVisual()
    {
        if (currentHeldObject != null)
        {
            Destroy(
                currentHeldObject
            );

            currentHeldObject = null;
        }
    }


    // =========================================================
    // SET LAYER
    // =========================================================

    private void SetLayerRecursively(
        GameObject obj,
        int newLayer)
    {
        if (obj == null)
            return;

        obj.layer =
            newLayer;

        foreach (
            Transform child
            in obj.transform)
        {
            SetLayerRecursively(
                child.gameObject,
                newLayer
            );
        }
    }


    // =========================================================
    // DROP SELECTED ITEM
    // =========================================================

    public void DropSelectedItem()
    {
        if (currentSelectedSlot < 0 ||
            currentSelectedSlot >=
            hotbarSlots.Count)
        {
            Debug.LogWarning(
                "[HotbarManager] Invalid selected slot."
            );

            return;
        }

        HotbarSlot slot =
            hotbarSlots[
                currentSelectedSlot
            ];

        if (slot == null)
            return;

        InventoryItem itemUI =
            slot.GetComponentInChildren<
                InventoryItem>();

        if (itemUI == null)
        {
            Debug.Log(
                "[HotbarManager] Selected slot is empty."
            );

            return;
        }

        ItemDataSO itemData =
            ExtractItemData(itemUI);

        if (itemData == null)
        {
            Debug.LogWarning(
                "[HotbarManager] Could not find ItemDataSO."
            );

            return;
        }

        if (itemData.physicalPrefab == null)
        {
            Debug.LogWarning(
                "[HotbarManager] '" +
                itemData.itemName +
                "' has no Physical Prefab assigned."
            );

            return;
        }

        GameObject player =
            GameObject.FindWithTag("Player");

        if (player == null)
        {
            Debug.LogWarning(
                "[HotbarManager] Player tag not found."
            );

            return;
        }

        Camera playerCamera =
            player.GetComponentInChildren<Camera>();

        if (playerCamera == null)
        {
            playerCamera =
                Camera.main;
        }

        if (playerCamera == null)
        {
            Debug.LogWarning(
                "[HotbarManager] Player camera not found."
            );

            return;
        }

        Vector3 dropPosition =
            playerCamera.transform.position +
            playerCamera.transform.forward *
            dropDistance;

        dropPosition +=
            Vector3.down * 0.2f;

        GameObject droppedObject =
            Instantiate(
                itemData.physicalPrefab,
                dropPosition,
                Quaternion.identity
            );

        droppedObject.name =
            itemData.itemName +
            "_Dropped";

        Gatherable droppedGatherable =
            droppedObject.GetComponent<Gatherable>();

        if (droppedGatherable != null)
        {
            Destroy(droppedGatherable);
        }

        Collider[] colliders =
            droppedObject
                .GetComponentsInChildren<Collider>();

        foreach (Collider col in colliders)
        {
            if (col != null)
            {
                col.enabled = true;
            }
        }

        Rigidbody rb =
            droppedObject
                .GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb =
                droppedObject.AddComponent<Rigidbody>();
        }

        rb.isKinematic = false;
        rb.useGravity = true;

        rb.AddForce(
            playerCamera.transform.forward *
            dropForce,
            ForceMode.Impulse
        );

        DroppedItem droppedItem =
            droppedObject
                .GetComponent<DroppedItem>();

        if (droppedItem == null)
        {
            droppedItem =
                droppedObject.AddComponent<DroppedItem>();
        }

        droppedItem.Initialize(
            itemData,
            pickupDelay,
            1
        );

        itemUI.RemoveFromStack(1);

        NotifyHotbarChanged();

        UpdateHandVisual();

        Debug.Log(
            "[HotbarManager] Dropped: " +
            itemData.itemName
        );
    }


    // =========================================================
    // CONSUME
    // =========================================================

    public void ConsumeActiveSlotItem()
    {
        TryConsumeSlotItem(
            currentSelectedSlot
        );
    }


    public void ConsumeSlotItem(
        int slotIndex)
    {
        TryConsumeSlotItem(
            slotIndex
        );
    }


    public void TryConsumeSlotItem(
        int slotIndex)
    {
        if (slotIndex < 0 ||
            slotIndex >=
            hotbarSlots.Count)
        {
            return;
        }

        HotbarSlot slot =
            hotbarSlots[slotIndex];

        if (slot == null)
            return;

        InventoryItem itemUI =
            slot.GetComponentInChildren<
                InventoryItem>();

        if (itemUI == null)
            return;

        ItemDataSO data =
            ExtractItemData(itemUI);

        if (data == null)
            return;

        if (!IsItemConsumable(data))
            return;

        ApplyItemEffects(data);

        itemUI.RemoveFromStack(1);

        NotifyHotbarChanged();

        UpdateHandVisual();
    }


    // =========================================================
    // CHECK CONSUMABLE
    // =========================================================

    private bool IsItemConsumable(
        ItemDataSO data)
    {
        if (data == null)
            return false;

        return data.itemType ==
                   ItemType.Consumable ||
               data.healthRestore > 0 ||
               data.hungerRestore > 0 ||
               data.thirstRestore > 0;
    }


    // =========================================================
    // GET ITEM DATA
    // =========================================================

    private ItemDataSO ExtractItemData(
        InventoryItem itemUI)
    {
        if (itemUI == null)
            return null;

        System.Type type =
            itemUI.GetType();

        BindingFlags flags =
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Instance;

        var method =
            type.GetMethod(
                "GetItemData",
                flags
            )
            ??
            type.GetMethod(
                "GetItem",
                flags
            );

        if (method != null &&
            method.GetParameters().Length == 0)
        {
            var result =
                method.Invoke(
                    itemUI,
                    null
                );

            if (result is ItemDataSO itemMethod)
            {
                return itemMethod;
            }
        }

        var prop =
            type.GetProperty(
                "itemData",
                flags
            )
            ??
            type.GetProperty(
                "ItemData",
                flags
            )
            ??
            type.GetProperty(
                "item",
                flags
            );

        if (prop != null &&
            prop.CanRead)
        {
            var result =
                prop.GetValue(itemUI);

            if (result is ItemDataSO itemProp)
            {
                return itemProp;
            }
        }

        string[] fieldNames =
        {
            "itemData",
            "item",
            "_itemData",
            "_item",
            "itemSO"
        };

        foreach (
            string name
            in fieldNames)
        {
            var field =
                type.GetField(
                    name,
                    flags
                );

            if (field != null)
            {
                var result =
                    field.GetValue(itemUI);

                if (result is ItemDataSO itemField)
                {
                    return itemField;
                }
            }
        }

        return null;
    }


    // =========================================================
    // APPLY CONSUMABLE EFFECTS
    // =========================================================

    private void ApplyItemEffects(
        ItemDataSO item)
    {
        // SurvivalUIManager lives on the UI object, not the Player,
        // so SendMessage on the Player never reached it.
        if (survivalUI == null)
        {
            survivalUI =
                FindFirstObjectByType<
                    SurvivalUIManager>();
        }

        if (survivalUI != null)
        {
            survivalUI.Heal(
                item.healthRestore
            );

            survivalUI.ReplenishHunger(
                item.hungerRestore
            );

            survivalUI.ReplenishThirst(
                item.thirstRestore
            );
        }
        else
        {
            Debug.LogWarning(
                "[HotbarManager] SurvivalUIManager " +
                "not found in scene."
            );
        }

        GameObject playerObj =
            GameObject.FindWithTag("Player");

        if (playerObj != null)
        {
            PlayerHealth playerHealth =
                playerObj.GetComponent<
                    PlayerHealth>();

            if (playerHealth != null &&
                item.healthRestore > 0f)
            {
                playerHealth.Heal(
                    Mathf.RoundToInt(
                        item.healthRestore
                    )
                );
            }

            playerObj.SendMessage(
                "EatFood",
                item,
                SendMessageOptions
                    .DontRequireReceiver
            );
        }
    }


    // =========================================================
    // ADD TO HOTBAR
    // =========================================================

    public bool TryAddToHotbar(
        ItemDataSO item,
        int amount)
    {
        if (item == null ||
            amount <= 0)
        {
            return false;
        }

        int remaining =
            amount;

        if (item.isStackable)
        {
            foreach (
                var slot
                in hotbarSlots)
            {
                if (slot == null)
                    continue;

                InventoryItem slotItem =
                    slot.GetComponentInChildren<
                        InventoryItem>();

                if (slotItem == null)
                    continue;

                ItemDataSO existingItem =
                    ExtractItemData(slotItem);

                if (existingItem != item)
                    continue;

                int leftover =
                    slotItem.AddToStack(
                        remaining
                    );

                remaining =
                    leftover;

                if (remaining <= 0)
                {
                    NotifyHotbarChanged();
                    return true;
                }
            }
        }

        foreach (
            var slot
            in hotbarSlots)
        {
            if (slot == null)
                continue;

            InventoryItem slotItem =
                slot.GetComponentInChildren<
                    InventoryItem>();

            if (slotItem == null)
                continue;

            ItemDataSO existingItem =
                ExtractItemData(slotItem);

            if (existingItem != null)
                continue;

            int amountForSlot;

            if (item.isStackable)
            {
                amountForSlot =
                    Mathf.Min(
                        remaining,
                        item.maxStackSize
                    );
            }
            else
            {
                amountForSlot = 1;
            }

            slotItem.SetupItem(
                item,
                amountForSlot
            );

            remaining -=
                amountForSlot;

            if (remaining <= 0)
            {
                NotifyHotbarChanged();
                return true;
            }
        }

        return false;
    }


    // =========================================================
    // NOTIFY INVENTORY
    // =========================================================

    public void NotifyHotbarChanged()
    {
        Inventory inventory =
            FindFirstObjectByType<Inventory>();

        if (inventory != null)
        {
            inventory.PopulateCraftingGrid();
        }
    }


    // =========================================================
    // PICK UP THROWN SPEAR
    // =========================================================

    public bool TryPickUpThrownSpear(
        SpearThrow thrownSpear)
    {
        if (thrownSpear == null)
        {
            return false;
        }

        if (!thrownSpear.HasBeenThrown)
        {
            return false;
        }

        ItemDataSO spearData =
            thrownSpear.spearItemData;

        if (spearData == null)
        {
            Debug.LogWarning(
                "[HotbarManager] Thrown spear has no ItemDataSO."
            );

            return false;
        }

        bool added =
            TryAddToHotbar(
                spearData,
                1
            );

        if (!added)
        {
            Debug.Log(
                "[HotbarManager] No space for picked-up spear."
            );

            return false;
        }

        Destroy(
            thrownSpear.gameObject
        );

        NotifyHotbarChanged();

        UpdateHandVisual();

        Debug.Log(
            "[HotbarManager] Picked up thrown spear: " +
            spearData.itemName
        );

        return true;
    }


    // =========================================================
    // CHARGE DISPLAY
    // =========================================================

    private void OnGUI()
    {
        if (!isChargingSpear)
            return;

        if (!IsHoldingSpear())
            return;

        float width = 220f;
        float height = 25f;

        float x =
            (Screen.width - width) / 2f;

        float y =
            Screen.height - 120f;

        GUI.Box(
            new Rect(
                x,
                y,
                width,
                height
            ),
            ""
        );

        GUI.Box(
            new Rect(
                x + 3f,
                y + 3f,
                (width - 6f) *
                spearChargeAmount,
                height - 6f
            ),
            ""
        );

        GUI.Label(
            new Rect(
                x,
                y - 25f,
                width,
                25f
            ),
            "Spear Force: " +
            Mathf.RoundToInt(
                spearChargeAmount * 100f
            ) +
            "%"
        );
    }
}