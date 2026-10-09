using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [HideInInspector] public bool canRotateMouse = true;

    [Header("Dependencies")]
    [SerializeField] private SurvivalUIManager survivalUI;
    [SerializeField] private HoldProgressUI holdCircleUI;
    [SerializeField] private Animator animator;

    [Header("Cinemachine Cameras & Perspective")]
    [SerializeField] private CinemachineCamera vCamFirstPerson;
    [SerializeField] private CinemachineCamera vCamThirdPerson;
    [SerializeField] private Transform cameraSocket;
    [SerializeField] private Renderer[] playerMeshes;
    [SerializeField] private KeyCode togglePerspectiveKey = KeyCode.V;

    private bool isThirdPerson = true;

    // =========================================================
    // EQUIPMENT STATE
    // =========================================================

    [Header("Equipment State")]
    [Tooltip("Automatically detected. You normally do NOT need to change this manually.")]
    public bool isHoldingAxe = false;

    [Tooltip("Automatically detected when the Fishing Rod is selected.")]
    public bool isHoldingFishingRod = false;

    private HotbarManager hotbarManager;

    // =========================================================
    // WOODCUTTING
    // =========================================================

    [Header("Woodcutting / Interaction Settings")]
    [SerializeField] private float chopDistance = 6f;
    [SerializeField] private KeyCode chopKey = KeyCode.Mouse0;
    [SerializeField] private float chopDuration = 1.5f;
    [SerializeField] private LayerMask interactionLayerMask = ~0;
    [SerializeField] private float terrainTreeChopRadius = 3.5f;
    [SerializeField] private GameObject choppableTreePrefab;
    [Tooltip("Choppable prefab per terrain tree prototype (same order as the Terrain's Trees list). Falls back to 'Choppable Tree Prefab' if an entry is empty.")]
    [SerializeField] private GameObject[] choppableTreePrefabsByPrototype;
    [Tooltip("Show the terrain tree's own model on the felled tree instead of the choppable prefab's mesh.")]
    [SerializeField] private bool useTerrainTreeModel = true;
    [Tooltip("Played on every chop hit if the choppable prefab has no Hit Sound of its own.")]
    [SerializeField] private AudioClip chopHitSound;
    [Tooltip("Played when the tree is chopped down if the choppable prefab has no Fall Sound of its own.")]
    [SerializeField] private AudioClip treeFallSound;

    private bool isConvertingTree;

    private float chopHoldTimer = 0f;
    private float chopAnimCooldown = 0f;

    // =========================================================
    // MOVEMENT SETTINGS
    // =========================================================

    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 6f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;
    [SerializeField] private float backwardSpeedMultiplier = 0.65f;

    // =========================================================
    // JUMP SETTINGS
    // =========================================================

    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 2f;

    // =========================================================
    // GRAVITY SETTINGS
    // =========================================================

    [Header("Gravity Settings")]
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedGravity = -2f;

    private float verticalVelocity;
    private bool isGroundedPhysically;

    // =========================================================
    // SMOOTHING
    // =========================================================

    [Header("Smoothing Settings")]
    [Range(0.01f, 0.5f)]
    [SerializeField] private float movementSmoothTime = 0.12f;

    // =========================================================
    // CAMERA EFFECTS
    // =========================================================

    [Header("Camera Effects")]
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float sprintFOV = 70f;
    [SerializeField] private float fovTransitionSpeed = 5f;

    // =========================================================
    // STAMINA
    // =========================================================

    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDrainRate = 20f;
    [SerializeField] private float staminaRegenRate = 15f;

    private float currentStamina;
    private bool isExhausted = false;

    // =========================================================
    // MOUSE LOOK
    // =========================================================

    [Header("Mouse Look Settings")]
    public float mouseSensitivity = 2f;

    [SerializeField] private float minPitch = -85f;
    [SerializeField] private float maxPitch = 85f;

    private float cameraPitch = 0f;

    private const string MouseSensitivityKey = "MouseSensitivity";

    // =========================================================
    // TURN IN PLACE
    // =========================================================

    [Header("Turn In Place Settings")]
    [SerializeField] private float turnThresholdAngle = 45f;

    // =========================================================
    // CROUCH
    // =========================================================

    [Header("Crouch Settings")]
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchTransitionSpeed = 10f;
    [SerializeField] private float standingSocketY = 1.6f;
    [SerializeField] private float crouchSocketY = 0.8f;

    // =========================================================
    // FOOTSTEPS
    // =========================================================

    [Header("Footstep Settings")]
    [SerializeField] private AudioSource footstepSource;
    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] private float walkFootstepInterval = 0.5f;
    [SerializeField] private float sprintFootstepInterval = 0.3f;
    [SerializeField] private float crouchFootstepInterval = 0.7f;

    private float footstepTimer;

    // =========================================================
    // PRIVATE MOVEMENT VARIABLES
    // =========================================================

    private CharacterController controller;
    private bool isCrouching = false;
    private bool isSprinting = false;

    private Vector3 currentMoveDirection;
    private Vector3 smoothMoveVelocity;

    private bool isReadyToMove = false;

    // =========================================================
    // POWER BOOST
    // =========================================================

    private bool isPowerBoosted = false;
    private float originalSprintSpeed;
    private Coroutine boostCoroutine;

    // =========================================================
    // ANIMATION
    // =========================================================

    private float currentAnimHorizontal = 0f;
    private float currentAnimVertical = 0f;
    private float accumulatedTurnAngle = 0f;

    private HashSet<int> validAnimParameters =
        new HashSet<int>();

    // =========================================================
    // CAMERA
    // =========================================================

    private Camera mainCamera;

    // =========================================================
    // UNITY
    // =========================================================

    // =========================================================

    void Awake()
    {
        if (SceneManager.GetActiveScene().name == "MainMenu 1")
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        isSprinting = false;

        if (survivalUI != null)
            survivalUI.isRunning = false;

        isCrouching = false;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu 1")
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Destroy(gameObject);
            return;
        }

        StartCoroutine(FindSurvivalUIAfterSceneLoad());
    }

    private IEnumerator FindSurvivalUIAfterSceneLoad()
    {
        yield return null;

        survivalUI =
            FindFirstObjectByType<SurvivalUIManager>();

        if (survivalUI != null)
        {
            survivalUI.stamina = currentStamina;

            Debug.Log(
                "[PlayerMovement] SurvivalUIManager found after scene load."
            );
        }
        else
        {
            Debug.LogWarning(
                "[PlayerMovement] SurvivalUIManager was not found after scene load."
            );
        }

        mainCamera = Camera.main;

        FindHotbarManager();

        UpdateCameraPerspective();
    }

    void Start()
    {
        controller =
            GetComponent<CharacterController>();

        mainCamera = Camera.main;

        LoadMouseSensitivity();

        isCrouching = false;

        if (controller != null)
        {
            controller.height =
                standingHeight;

            controller.center =
                new Vector3(
                    controller.center.x,
                    standingHeight / 2f,
                    controller.center.z
                );
        }

        if (cameraSocket != null)
        {
            Vector3 localPos =
                cameraSocket.localPosition;

            localPos.y =
                standingSocketY;

            cameraSocket.localPosition =
                localPos;
        }

        if (animator == null)
        {
            animator =
                GetComponentInChildren<Animator>();
        }

        CacheAnimatorParameters();

        if (SceneManager.GetActiveScene().name != "MainMenu 1")
        {
            Cursor.lockState =
                CursorLockMode.Locked;

            Cursor.visible = false;
        }

        currentStamina =
            maxStamina;

        originalSprintSpeed =
            sprintSpeed;

        if (survivalUI == null)
        {
            survivalUI =
                FindFirstObjectByType<SurvivalUIManager>();
        }

        if (survivalUI != null)
        {
            survivalUI.stamina =
                currentStamina;
        }

        FindHotbarManager();

        UpdateAxeState();

        UpdateCameraPerspective();

        StartCoroutine(
            TerrainSafetySequence()
        );
    }

    // =========================================================
    // HOTBAR / AXE DETECTION
    // =========================================================

    private void FindHotbarManager()
    {
        if (hotbarManager != null)
            return;

        hotbarManager =
            FindFirstObjectByType<HotbarManager>();

        if (hotbarManager == null)
        {
            Debug.LogWarning(
                "[PlayerMovement] HotbarManager was not found."
            );
        }
    }

    private void UpdateAxeState()
    {
        FindHotbarManager();

        bool previousState =
            isHoldingAxe;

        isHoldingAxe = false;

        if (hotbarManager == null)
            return;

        if (hotbarManager.hotbarSlots == null ||
            hotbarManager.hotbarSlots.Count == 0)
        {
            return;
        }

        int selectedIndex =
            hotbarManager.currentSelectedSlot;

        if (selectedIndex < 0 ||
            selectedIndex >=
            hotbarManager.hotbarSlots.Count)
        {
            return;
        }

        HotbarSlot selectedSlot =
            hotbarManager.hotbarSlots[
                selectedIndex
            ];

        if (selectedSlot == null)
            return;

        InventoryItem itemUI =
            selectedSlot.GetComponentInChildren<
                InventoryItem>();

        if (itemUI == null)
            return;

        ItemDataSO itemData =
            GetItemDataFromInventoryItem(
                itemUI
            );

        if (itemData == null)
            return;

        // =====================================================
        // AUTOMATIC AXE DETECTION
        // =====================================================

        if (itemData.itemType == ItemType.Tool &&
            !string.IsNullOrEmpty(itemData.itemName) &&
            itemData.itemName.ToLower().Contains("axe"))
        {
            isHoldingAxe = true;
        }

        if (previousState != isHoldingAxe)
        {
            if (isHoldingAxe)
            {
                Debug.Log(
                    "[PlayerMovement] Axe equipped. Tree chopping enabled."
                );
            }
            else
            {
                Debug.Log(
                    "[PlayerMovement] Axe unequipped. Tree chopping disabled."
                );

                ResetChopState();
            }
        }
    }

    private void UpdateFishingState()
    {
        FindHotbarManager();

        bool previousState =
            isHoldingFishingRod;

        isHoldingFishingRod = false;

        if (hotbarManager == null ||
            hotbarManager.hotbarSlots == null ||
            hotbarManager.hotbarSlots.Count == 0)
        {
            SetAnimBool(
                "IsFishing",
                false
            );

            return;
        }

        int selectedIndex =
            hotbarManager.currentSelectedSlot;

        if (selectedIndex < 0 ||
            selectedIndex >=
            hotbarManager.hotbarSlots.Count)
        {
            SetAnimBool(
                "IsFishing",
                false
            );

            return;
        }

        HotbarSlot selectedSlot =
            hotbarManager.hotbarSlots[
                selectedIndex
            ];

        if (selectedSlot == null)
        {
            SetAnimBool(
                "IsFishing",
                false
            );

            return;
        }

        InventoryItem itemUI =
            selectedSlot.GetComponentInChildren<
                InventoryItem>();

        if (itemUI == null)
        {
            SetAnimBool(
                "IsFishing",
                false
            );

            return;
        }

        ItemDataSO itemData =
            GetItemDataFromInventoryItem(
                itemUI
            );

        if (itemData == null)
        {
            SetAnimBool(
                "IsFishing",
                false
            );

            return;
        }

        if (!string.IsNullOrEmpty(itemData.itemName))
        {
            string itemName =
                itemData.itemName.ToLower();

            if (itemName.Contains("fishing rod"))
            {
                isHoldingFishingRod = true;
            }
        }

        SetAnimBool(
            "IsFishing",
            isHoldingFishingRod
        );

        if (previousState != isHoldingFishingRod)
        {
            if (isHoldingFishingRod)
            {
                Debug.Log(
                    "[PlayerMovement] Fishing Rod equipped. Fishing Idle enabled."
                );
            }
            else
            {
                Debug.Log(
                    "[PlayerMovement] Fishing Rod unequipped. Fishing Idle disabled."
                );
            }
        }
    }

    private ItemDataSO GetItemDataFromInventoryItem(
        InventoryItem itemUI)
    {
        if (itemUI == null)
            return null;

        System.Type type =
            itemUI.GetType();

        System.Reflection.BindingFlags flags =
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance;

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
            object result =
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
            object result =
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

        foreach (string fieldName
                 in fieldNames)
        {
            var field =
                type.GetField(
                    fieldName,
                    flags
                );

            if (field != null)
            {
                object result =
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
    // TERRAIN SAFETY
    // =========================================================

    IEnumerator TerrainSafetySequence()
    {
        isReadyToMove = false;

        yield return new WaitForFixedUpdate();
        yield return new WaitForEndOfFrame();

        if (Terrain.activeTerrain != null)
        {
            float currentHeight =
                Terrain.activeTerrain.SampleHeight(
                    transform.position
                ) +
                Terrain.activeTerrain.transform.position.y;

            if (transform.position.y <
                currentHeight + 0.2f)
            {
                controller.enabled = false;

                transform.position =
                    new Vector3(
                        transform.position.x,
                        currentHeight + 0.5f,
                        transform.position.z
                    );

                controller.enabled = true;
            }
        }

        verticalVelocity =
            groundedGravity;

        isReadyToMove = true;
    }

    // =========================================================
    // REBINDABLE INPUT (falls back to legacy input if no RebindManager)
    // =========================================================

    private bool warnedNoRebindManager;

    private UnityEngine.InputSystem.InputAction GetAction(string actionName)
    {
        if (RebindManager.Instance == null ||
            RebindManager.Instance.inputActions == null)
        {
            if (!warnedNoRebindManager)
            {
                warnedNoRebindManager = true;

                Debug.LogWarning(
                    "[PlayerMovement] No RebindManager found - using default keys. " +
                    "Start the game from the MainMenu scene so rebinds apply."
                );
            }

            return null;
        }

        return RebindManager.Instance.inputActions.FindAction(actionName, false);
    }

    private Vector2 GetMoveInput()
    {
        var action = GetAction("Move");

        if (action != null)
        {
            Vector2 v = action.ReadValue<Vector2>();

            return new Vector2(
                Mathf.Abs(v.x) > 0.01f ? Mathf.Sign(v.x) : 0f,
                Mathf.Abs(v.y) > 0.01f ? Mathf.Sign(v.y) : 0f
            );
        }

        return new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );
    }

    private bool IsActionHeld(string actionName, KeyCode fallback)
    {
        var action = GetAction(actionName);

        return action != null
            ? action.IsPressed()
            : Input.GetKey(fallback);
    }

    private bool WasActionPressed(string actionName, KeyCode fallback)
    {
        var action = GetAction(actionName);

        return action != null
            ? action.WasPressedThisFrame()
            : Input.GetKeyDown(fallback);
    }

    // =========================================================
    // UPDATE
    // =========================================================

    void Update()
    {
        if (!isReadyToMove)
            return;

        // Automatically check selected hotbar item.
        UpdateAxeState();
        UpdateFishingState();

        if (WasActionPressed(
            "Camera Switch",
            togglePerspectiveKey))
        {
            isThirdPerson =
                !isThirdPerson;

            UpdateCameraPerspective();
        }

        if (canRotateMouse)
        {
            HandleMouseLookInput();
        }

        HandleSprintLogic();

        HandleCrouch();

        HandleChopInteraction();

        HandleMovement();

        HandleCameraEffects();

        HandleFootsteps();

        UpdateAnimatorParameters();
    }

    // Movement and rotation are driven by code only. Overriding this
    // discards root motion so swing/chop animations can't turn or
    // shift the player (and the camera attached to it).
    void OnAnimatorMove()
    {
    }

    void LateUpdate()
    {
        if (!isReadyToMove)
            return;

        ApplyCameraRotation();
    }

    // =========================================================
    // CHOPPING
    // =========================================================

    private void HandleChopInteraction()
    {
        if (!isHoldingAxe)
        {
            ResetChopState();
            return;
        }

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            ResetChopState();
            return;
        }

        bool isHoldingClick =
            Input.GetKey(chopKey) &&
            !Input.GetKey(KeyCode.LeftControl);

        if (!isHoldingClick)
        {
            ResetChopState();
            return;
        }

        // Make sure we have a camera
        if (mainCamera == null)
            mainCamera = Camera.main;

        Vector3 playerPosition = transform.position;

        // Find trees around the player instead of relying
        // completely on the crosshair raycast.
        Collider[] nearbyColliders = Physics.OverlapSphere(
            playerPosition,
            chopDistance,
            interactionLayerMask,
            QueryTriggerInteraction.Ignore
        );

        ChoppableTree closestTree = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider col in nearbyColliders)
        {
            if (col == null)
                continue;

            // Ignore the player's own colliders
            if (col.gameObject == gameObject ||
                col.transform.IsChildOf(transform))
            {
                continue;
            }

            ChoppableTree tree =
                col.GetComponentInParent<ChoppableTree>();

            if (tree == null)
                continue;

            Vector3 closestPoint =
                col.ClosestPoint(playerPosition);

            float distance =
                Vector3.Distance(
                    playerPosition,
                    closestPoint
                );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTree = tree;
            }
        }

        // ---------------------------------------------------------
        // CHOP NORMAL CHOPPABLE TREE
        // ---------------------------------------------------------

        if (closestTree != null)
        {
            ExecuteChopProgress(
                onChopHit: () =>
                {
                    closestTree.PlayChopEffects();
                    SetAnimTrigger("Chop");
                },

                onChopFinished: () =>
                {
                    closestTree.TakeDamage(1);
                }
            );

            return;
        }

        // ---------------------------------------------------------
        // TERRAIN TREES
        // ---------------------------------------------------------

        Terrain terrain =
            Terrain.activeTerrain;

        if (terrain != null)
        {
            TreeInstance[] trees =
                terrain.terrainData.treeInstances;

            int closestTerrainTree = -1;
            float closestTreeDistance =
                Mathf.Infinity;

            for (int i = 0;
                 i < trees.Length;
                 i++)
            {
                Vector3 treeWorldPosition =
                    Vector3.Scale(
                        trees[i].position,
                        terrain.terrainData.size
                    ) +
                    terrain.transform.position;

                float distance =
                    Vector3.Distance(
                        playerPosition,
                        treeWorldPosition
                    );

                if (distance > chopDistance)
                    continue;

                // Prefer the tree the camera is looking at over whichever
                // tree happens to be nearest to the player's feet.
                float angle = 180f;

                if (mainCamera != null)
                {
                    Vector3 toTree =
                        (treeWorldPosition + Vector3.up * 2f) -
                        mainCamera.transform.position;

                    angle = Vector3.Angle(
                        mainCamera.transform.forward,
                        toTree
                    );
                }

                // Score: aimed trees (<= 35 degrees) always beat unaimed ones.
                float score =
                    (angle <= 35f ? 0f : 1000f) +
                    (angle <= 35f ? angle : distance);

                if (score < closestTreeDistance)
                {
                    closestTreeDistance =
                        score;

                    closestTerrainTree =
                        i;
                }
            }

            // The terrain tree is swapped for a choppable copy on the first
            // hit so it can shake and play sounds. Wait for that copy to
            // exist so the next terrain tree isn't converted by mistake.
            if (isConvertingTree)
            {
                ExecuteChopProgress(
                    onChopHit: () => SetAnimTrigger("Chop"),
                    onChopFinished: null
                );

                return;
            }

            if (closestTerrainTree != -1)
            {
                ExecuteChopProgress(
                    onChopHit: () =>
                    {
                        SetAnimTrigger("Chop");

                        if (!isConvertingTree)
                        {
                            ConvertTerrainTreeToPrefab(
                                terrain,
                                closestTerrainTree
                            );
                        }
                    },

                    onChopFinished: null
                );

                return;
            }
        }

        // Nothing close enough to chop
        ResetChopState();
    }
    private void ResetChopState()
    {
        chopHoldTimer = 0f;
        chopAnimCooldown = 0f;

        if (holdCircleUI != null)
            holdCircleUI.HideUI();
    }

    private void ExecuteChopProgress(
        System.Action onChopHit,
        System.Action onChopFinished)
    {
        float safeDuration =
            Mathf.Max(
                chopDuration,
                0.1f
            );

        chopHoldTimer +=
            Time.deltaTime;

        float normalizedProgress =
            Mathf.Clamp01(
                chopHoldTimer /
                safeDuration
            );

        if (holdCircleUI != null)
        {
            holdCircleUI.ShowUI(
                normalizedProgress
            );
        }

        chopAnimCooldown -=
            Time.deltaTime;

        if (chopAnimCooldown <= 0f)
        {
            onChopHit?.Invoke();

            chopAnimCooldown =
                0.5f;
        }

        if (normalizedProgress >= 1f)
        {
            onChopFinished?.Invoke();

            chopHoldTimer = 0f;

            if (holdCircleUI != null)
                holdCircleUI.HideUI();
        }
    }

    // =========================================================
    // TERRAIN TREE
    // =========================================================

    private int FindClosestTerrainTreeIndex(
        Terrain terrain,
        Ray ray,
        float maxDistance,
        float maxRadius)
    {
        TerrainData tData =
            terrain.terrainData;

        if (tData == null)
            return -1;

        TreeInstance[] trees =
            tData.treeInstances;

        int closestIndex = -1;

        float closestDistSqr =
            maxRadius * maxRadius;

        for (int i = 0;
             i < trees.Length;
             i++)
        {
            Vector3 treeWorldPos =
                Vector3.Scale(
                    trees[i].position,
                    tData.size
                ) +
                terrain.transform.position;

            Vector2 treePos2D =
                new Vector2(
                    treeWorldPos.x,
                    treeWorldPos.z
                );

            Vector2 rayOrigin2D =
                new Vector2(
                    ray.origin.x,
                    ray.origin.z
                );

            Vector2 rayDir2D =
                new Vector2(
                    ray.direction.x,
                    ray.direction.z
                ).normalized;

            Vector2 originToTree2D =
                treePos2D -
                rayOrigin2D;

            float projectionDist =
                Vector2.Dot(
                    originToTree2D,
                    rayDir2D
                );

            if (projectionDist < 0 ||
                projectionDist > maxDistance)
            {
                continue;
            }

            Vector2 closestPointOnRay2D =
                rayOrigin2D +
                rayDir2D *
                projectionDist;

            float sqrPerpDistance =
                (
                    treePos2D -
                    closestPointOnRay2D
                ).sqrMagnitude;

            if (sqrPerpDistance <
                closestDistSqr)
            {
                closestDistSqr =
                    sqrPerpDistance;

                closestIndex =
                    i;
            }
        }

        return closestIndex;
    }

    private void ConvertTerrainTreeToPrefab(
        Terrain terrain,
        int treeIndex)
    {
        TerrainData tData =
            terrain.terrainData;

        TreeInstance[] currentTrees =
            tData.treeInstances;

        if (treeIndex >= 0 &&
            treeIndex < currentTrees.Length)
        {
            TreeInstance terrainTree =
                currentTrees[treeIndex];

            Vector3 treeWorldPos =
                Vector3.Scale(
                    terrainTree.position,
                    tData.size
                ) +
                terrain.transform.position;

            Quaternion treeRotation =
                Quaternion.Euler(
                    0,
                    terrainTree.rotation *
                    Mathf.Rad2Deg,
                    0
                );

            TreeInstance[] updatedTrees =
                new TreeInstance[
                    currentTrees.Length - 1
                ];

            System.Array.Copy(
                currentTrees,
                0,
                updatedTrees,
                0,
                treeIndex
            );

            System.Array.Copy(
                currentTrees,
                treeIndex + 1,
                updatedTrees,
                treeIndex,
                currentTrees.Length -
                treeIndex -
                1
            );

            isConvertingTree = true;

            tData.treeInstances =
                updatedTrees;

            terrain.Flush();

            StartCoroutine(
                FlushTerrainColliderAndSpawn(
                    terrain,
                    treeWorldPos,
                    treeRotation,
                    terrainTree
                )
            );
        }
    }

    // Hides the choppable prefab's own mesh and shows the terrain tree's
    // original model instead, so the felled tree matches the one chopped.
    private void ApplyTerrainTreeModel(
        Terrain terrain,
        TreeInstance terrainTree,
        GameObject spawned,
        ChoppableTree treeScript)
    {
        TreePrototype[] prototypes =
            terrain.terrainData.treePrototypes;

        if (terrainTree.prototypeIndex < 0 ||
            terrainTree.prototypeIndex >= prototypes.Length)
            return;

        GameObject modelPrefab =
            prototypes[terrainTree.prototypeIndex].prefab;

        if (modelPrefab == null)
            return;

        // Remember what the choppable prefab shows so it can be restored
        // if the terrain tree's model turns out to be unusable.
        LODGroup[] oldLods = spawned.GetComponentsInChildren<LODGroup>();
        System.Collections.Generic.List<Renderer> oldRenderers =
            new System.Collections.Generic.List<Renderer>();

        foreach (Renderer r in spawned.GetComponentsInChildren<Renderer>())
        {
            // Hide every visual (meshes, LODs, billboards) but keep
            // particle effects such as the wood chips.
            if (!(r is ParticleSystemRenderer) && r.enabled)
            {
                oldRenderers.Add(r);
                r.enabled = false;
            }
        }

        foreach (LODGroup lod in oldLods)
            lod.enabled = false;

        GameObject model =
            Instantiate(modelPrefab, treeScript.VisualModel);

        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;

        // Make sure the copied model is actually drawn at full detail.
        foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
            r.enabled = true;

        foreach (LODGroup lod in model.GetComponentsInChildren<LODGroup>(true))
        {
            lod.enabled = true;
            lod.ForceLOD(0);
        }

        if (model.GetComponentsInChildren<Renderer>(true).Length == 0)
        {
            Debug.LogWarning(
                $"[PlayerMovement] Terrain tree model '{modelPrefab.name}' has no renderers; keeping the choppable prefab's own visuals."
            );

            Destroy(model);

            foreach (Renderer r in oldRenderers)
                r.enabled = true;

            foreach (LODGroup lod in oldLods)
                lod.enabled = true;
        }
    }

    private IEnumerator FlushTerrainColliderAndSpawn(
        Terrain terrain,
        Vector3 treeWorldPos,
        Quaternion treeRotation,
        TreeInstance terrainTree)
    {
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        GameObject prefabToSpawn = choppableTreePrefab;

        // Best case: the terrain's own tree prefab already has a
        // ChoppableTree, so spawn that exact prefab. Nothing can mismatch.
        bool spawnsTerrainPrefab = false;
        TreePrototype[] terrainProtos = terrain.terrainData.treePrototypes;

        if (terrainTree.prototypeIndex >= 0 &&
            terrainTree.prototypeIndex < terrainProtos.Length)
        {
            GameObject protoPrefab =
                terrainProtos[terrainTree.prototypeIndex].prefab;

            if (protoPrefab != null &&
                protoPrefab.GetComponent<ChoppableTree>() != null)
            {
                prefabToSpawn = protoPrefab;
                spawnsTerrainPrefab = true;
            }
        }

        if (!spawnsTerrainPrefab &&
            choppableTreePrefabsByPrototype != null &&
            terrainTree.prototypeIndex >= 0 &&
            terrainTree.prototypeIndex < choppableTreePrefabsByPrototype.Length &&
            choppableTreePrefabsByPrototype[terrainTree.prototypeIndex] != null)
        {
            prefabToSpawn =
                choppableTreePrefabsByPrototype[terrainTree.prototypeIndex];
        }

        {
            TreePrototype[] protos = terrain.terrainData.treePrototypes;
            string protoName =
                terrainTree.prototypeIndex >= 0 &&
                terrainTree.prototypeIndex < protos.Length &&
                protos[terrainTree.prototypeIndex].prefab != null
                    ? protos[terrainTree.prototypeIndex].prefab.name
                    : "none";

            Debug.Log(
                $"[PlayerMovement] Chopped terrain tree prototype #{terrainTree.prototypeIndex} " +
                $"('{protoName}'), spawning choppable prefab '{(prefabToSpawn != null ? prefabToSpawn.name : "NULL")}'"
            );
        }

        if (prefabToSpawn != null)
        {
            GameObject spawnedTreeObj =
                Instantiate(
                    prefabToSpawn,
                    treeWorldPos,
                    treeRotation
                );

            Vector3 prefabScale = prefabToSpawn.transform.localScale;

            spawnedTreeObj.transform.localScale =
                new Vector3(
                    prefabScale.x * terrainTree.widthScale,
                    prefabScale.y * terrainTree.heightScale,
                    prefabScale.z * terrainTree.widthScale
                );

            if (spawnedTreeObj.TryGetComponent(
                out ChoppableTree treeScript))
            {
                if (useTerrainTreeModel && !spawnsTerrainPrefab)
                    ApplyTerrainTreeModel(
                        terrain, terrainTree, spawnedTreeObj, treeScript);

                if (treeScript.hitSound == null)
                    treeScript.hitSound = chopHitSound;

                if (treeScript.fallSound == null)
                    treeScript.fallSound = treeFallSound;

                // Terrain trees fall after a single full chop.
                treeScript.SetHealth(1);
                treeScript.PlayChopEffects();
            }
        }
        else
        {
            Debug.LogWarning(
                "[PlayerMovement] 'Choppable Tree Prefab' field is unassigned in the Inspector!"
            );
        }

        isConvertingTree = false;
    }

    // =========================================================
    // CAMERA PERSPECTIVE
    // =========================================================

    private void UpdateCameraPerspective()
    {
        if (vCamThirdPerson != null)
        {
            vCamThirdPerson.Priority.Value =
                isThirdPerson
                ? 20
                : 10;

            if (cameraSocket != null)
            {
                vCamThirdPerson.Target
                    .TrackingTarget =
                    cameraSocket;
            }
        }

        if (vCamFirstPerson != null)
        {
            vCamFirstPerson.Priority.Value =
                isThirdPerson
                ? 10
                : 20;

            if (cameraSocket != null)
            {
                vCamFirstPerson.Target
                    .TrackingTarget =
                    cameraSocket;
            }
        }

        UnityEngine.Rendering.ShadowCastingMode mode =
            isThirdPerson
            ? UnityEngine.Rendering.ShadowCastingMode.On
            : UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

        if (playerMeshes != null)
        {
            foreach (Renderer mesh
                     in playerMeshes)
            {
                if (mesh != null)
                {
                    mesh.shadowCastingMode =
                        mode;
                }
            }
        }
    }

    // =========================================================
    // MOUSE LOOK
    // =========================================================

    private void HandleMouseLookInput()
    {
        float mouseX =
            Input.GetAxis("Mouse X") *
            mouseSensitivity;

        float mouseY =
            Input.GetAxis("Mouse Y") *
            mouseSensitivity;

        transform.Rotate(
            Vector3.up * mouseX
        );

        cameraPitch -=
            mouseY;

        cameraPitch =
            Mathf.Clamp(
                cameraPitch,
                minPitch,
                maxPitch
            );
    }

    private void ApplyCameraRotation()
    {
        if (cameraSocket != null)
        {
            cameraSocket.localRotation =
                Quaternion.Euler(
                    cameraPitch,
                    0f,
                    0f
                );
        }
    }

    // =========================================================
    // MOUSE SENSITIVITY
    // =========================================================

    public void SetMouseSensitivity(
        float sensitivity)
    {
        mouseSensitivity =
            Mathf.Clamp(
                sensitivity,
                0.1f,
                10f
            );

        PlayerPrefs.SetFloat(
            MouseSensitivityKey,
            mouseSensitivity
        );

        PlayerPrefs.Save();
    }

    public void LoadMouseSensitivity()
    {
        mouseSensitivity =
            PlayerPrefs.GetFloat(
                MouseSensitivityKey,
                mouseSensitivity
            );
    }

    // =========================================================
    // SPRINT
    // =========================================================

    private void HandleSprintLogic()
    {
        bool isMoving =
            GetMoveInput() != Vector2.zero;

        bool isTryingToSprint =
            IsActionHeld("Sprint", KeyCode.LeftShift) &&
            isMoving;

        if (isTryingToSprint)
        {
            if (!HasCeilingObstruction())
            {
                isCrouching = false;
            }
            else
            {
                isTryingToSprint = false;
            }
        }

        bool isStarving =
            survivalUI != null &&
            survivalUI.hunger <= 0;

        if ((currentStamina <= 0 ||
             isStarving) &&
            !isPowerBoosted)
        {
            isExhausted = true;
            isSprinting = false;
        }

        if (isExhausted &&
            currentStamina >
            (maxStamina * 0.2f) &&
            !isStarving)
        {
            isExhausted = false;
        }

        if (isTryingToSprint &&
            (!isExhausted ||
             isPowerBoosted))
        {
            isSprinting = true;

            if (!isPowerBoosted)
            {
                currentStamina -=
                    staminaDrainRate *
                    Time.deltaTime;
            }
        }
        else
        {
            isSprinting = false;

            if (currentStamina < maxStamina &&
                !isStarving)
            {
                currentStamina +=
                    staminaRegenRate *
                    Time.deltaTime;
            }
        }

        currentStamina =
            Mathf.Clamp(
                currentStamina,
                0,
                maxStamina
            );

        if (survivalUI != null)
        {
            survivalUI.stamina =
                currentStamina;

            survivalUI.isRunning =
                isSprinting;
        }
    }

    // =========================================================
    // MOVEMENT
    // =========================================================

    private void HandleMovement()
    {
        Vector2 moveInput = GetMoveInput();

        float x = moveInput.x;

        float z = moveInput.y;

        Vector3 targetMove =
            transform.right * x +
            transform.forward * z;

        if (targetMove.magnitude > 1f)
            targetMove.Normalize();

        // Ignore the ground probe while rising, otherwise the player counts
        // as grounded right after jumping (breaks jump anim and allows re-jump).
        bool groundRaycast =
            verticalVelocity <= 0f &&
            Physics.Raycast(
                transform.position +
                Vector3.up * 0.2f,
                Vector3.down,
                0.5f
            );

        isGroundedPhysically =
            controller.isGrounded ||
            groundRaycast;

        if (isGroundedPhysically)
        {
            if (WasActionPressed("Jump", KeyCode.Space) &&
                (!isCrouching ||
                 isSprinting))
            {
                isCrouching = false;

                verticalVelocity =
                    Mathf.Sqrt(
                        jumpHeight *
                        -2f *
                        gravity
                    );

                currentMoveDirection =
                    targetMove;

                smoothMoveVelocity =
                    Vector3.zero;

                isGroundedPhysically =
                    false;

                SetAnimBool(
                    "IsSprinting",
                    isSprinting
                );

                SetAnimFloat(
                    "Speed",
                    isSprinting
                    ? sprintSpeed
                    : targetMove.magnitude *
                      walkSpeed
                );

                SetAnimTrigger(
                    "JumpTrigger"
                );
            }
            else
            {
                currentMoveDirection =
                    Vector3.SmoothDamp(
                        currentMoveDirection,
                        targetMove,
                        ref smoothMoveVelocity,
                        movementSmoothTime
                    );

                if (verticalVelocity < 0f)
                {
                    verticalVelocity =
                        groundedGravity;
                }
            }
        }

        if (!isGroundedPhysically)
        {
            currentMoveDirection =
                Vector3.Lerp(
                    currentMoveDirection,
                    targetMove,
                    Time.deltaTime * 10f
                );

            verticalVelocity +=
                gravity *
                Time.deltaTime;
        }

        float currentSpeed =
            isSprinting
            ? sprintSpeed
            : walkSpeed;

        if (isCrouching &&
            !isSprinting)
        {
            currentSpeed *=
                crouchSpeedMultiplier;
        }

        if (z < 0f &&
            !isSprinting)
        {
            currentSpeed *=
                backwardSpeedMultiplier;
        }

        Vector3 move =
            currentMoveDirection *
            currentSpeed;

        if (Terrain.activeTerrain != null)
        {
            float terrainHeight =
                Terrain.activeTerrain.SampleHeight(
                    transform.position
                ) +
                Terrain.activeTerrain.transform.position.y;

            if (transform.position.y <
                terrainHeight + 0.05f)
            {
                controller.enabled = false;

                transform.position =
                    new Vector3(
                        transform.position.x,
                        terrainHeight + 0.1f,
                        transform.position.z
                    );

                controller.enabled = true;

                verticalVelocity =
                    groundedGravity;

                isGroundedPhysically =
                    true;
            }
        }

        move.y =
            verticalVelocity;

        controller.Move(
            move *
            Time.deltaTime
        );
    }

    // =========================================================
    // CROUCH
    // =========================================================

    private void HandleCrouch()
    {
        bool wantsToCrouch =
            IsActionHeld(
                "Crouch",
                KeyCode.LeftControl
            );

        if (isSprinting)
        {
            wantsToCrouch = false;
            isCrouching = false;
        }

        if (wantsToCrouch)
        {
            isCrouching = true;
        }
        else if (!isSprinting)
        {
            if (!HasCeilingObstruction())
            {
                isCrouching = false;
            }
        }

        float targetHeight =
            isCrouching
            ? crouchHeight
            : standingHeight;

        float targetCenterY =
            isCrouching
            ? crouchHeight / 2f
            : standingHeight / 2f;

        controller.height =
            Mathf.MoveTowards(
                controller.height,
                targetHeight,
                Time.deltaTime *
                crouchTransitionSpeed
            );

        controller.center =
            new Vector3(
                controller.center.x,
                Mathf.MoveTowards(
                    controller.center.y,
                    targetCenterY,
                    Time.deltaTime *
                    crouchTransitionSpeed
                ),
                controller.center.z
            );

        if (cameraSocket != null)
        {
            float targetSocketY =
                isCrouching
                ? crouchSocketY
                : standingSocketY;

            Vector3 currentLocalPos =
                cameraSocket.localPosition;

            currentLocalPos.y =
                Mathf.MoveTowards(
                    currentLocalPos.y,
                    targetSocketY,
                    Time.deltaTime *
                    crouchTransitionSpeed
                );

            cameraSocket.localPosition =
                currentLocalPos;
        }
    }

    private bool HasCeilingObstruction()
    {
        Vector3 origin =
            transform.position +
            Vector3.up *
            (crouchHeight + 0.1f);

        float distance =
            standingHeight -
            (crouchHeight + 0.1f);

        if (distance <= 0f)
            return false;

        return Physics.SphereCast(
            origin,
            controller.radius * 0.8f,
            Vector3.up,
            out _,
            distance,
            interactionLayerMask,
            QueryTriggerInteraction.Ignore
        );
    }

    // =========================================================
    // ANIMATOR
    // =========================================================

    private void UpdateAnimatorParameters()
    {
        if (animator == null)
            return;

        Vector2 rawMove = GetMoveInput();

        float rawX = rawMove.x;

        float rawY = rawMove.y;

        float targetVertical =
            rawY;

        if (isSprinting &&
            rawY > 0f)
        {
            targetVertical = 2f;
        }
        else if (isSprinting &&
                 rawY < 0f)
        {
            targetVertical = -2f;
        }

        float targetHorizontal =
            rawX;

        // Blend tree samples: Run Left/Right sit at (+-1, 2) and
        // Run Backward Left/Right at (+-3, -3), so only the backward
        // sprint strafe reaches them. Pure strafing stays at +-1 (strafe walk clips).
        bool backwardStrafe =
            rawX != 0f &&
            rawY < 0f &&
            !isCrouching;

        if (backwardStrafe)
        {
            // S+A / S+D always play Run Backward Left/Right (walking or sprinting).
            targetHorizontal = rawX * 3f;
            targetVertical = -3f;
        }
        else if (rawX != 0f &&
                 rawY == 0f)
        {
            // Pure A / D: exactly the strafe walk clip at (+-1, 0).
            targetHorizontal = isCrouching ? rawX * 0.5f : rawX;
            targetVertical = 0f;
        }
        else if (isCrouching)
        {
            targetHorizontal =
                rawX * 0.5f;
        }

        // Walking forward diagonals (e.g. W+D = (1,1)) sit between three clips
        // and read as "run right". Normalize so they blend forward + strafe only.
        if (!isSprinting && !backwardStrafe)
        {
            Vector2 walkAnim =
                new Vector2(targetHorizontal, targetVertical);

            if (walkAnim.sqrMagnitude > 1f)
            {
                walkAnim.Normalize();

                targetHorizontal = walkAnim.x;
                targetVertical = walkAnim.y;
            }
        }

        currentAnimHorizontal =
            Mathf.MoveTowards(
                currentAnimHorizontal,
                targetHorizontal,
                Time.deltaTime * 6f
            );

        currentAnimVertical =
            Mathf.MoveTowards(
                currentAnimVertical,
                targetVertical,
                Time.deltaTime * 6f
            );

        Vector3 localVelocity =
            transform.InverseTransformDirection(
                controller.velocity
            );

        float currentSpeed =
            new Vector3(
                localVelocity.x,
                0f,
                localVelocity.z
            ).magnitude;

        SetAnimFloat(
            "MoveX",
            currentAnimHorizontal
        );

        SetAnimFloat(
            "MoveY",
            currentAnimVertical
        );

        SetAnimFloat(
            "Horizontal",
            currentAnimHorizontal
        );

        SetAnimFloat(
            "Vertical",
            currentAnimVertical
        );

        SetAnimFloat(
            "Speed",
            currentSpeed
        );

        SetAnimBool(
            "IsGrounded",
            isGroundedPhysically
        );

        SetAnimFloat(
            "AirYVelocity",
            verticalVelocity
        );

        SetAnimBool(
            "IsCrouching",
            isCrouching
        );

        SetAnimBool(
            "IsSprinting",
            isSprinting
        );

        bool isMoving =
            rawX != 0f ||
            rawY != 0f;

        if (!isMoving &&
            canRotateMouse &&
            isGroundedPhysically)
        {
            float mouseX =
                Input.GetAxis("Mouse X") *
                mouseSensitivity;

            accumulatedTurnAngle +=
                mouseX;

            if (accumulatedTurnAngle >
                turnThresholdAngle)
            {
                SetAnimBool(
                    "TurnRight",
                    true
                );

                SetAnimBool(
                    "TurnLeft",
                    false
                );

                accumulatedTurnAngle =
                    0f;
            }
            else if (accumulatedTurnAngle <
                     -turnThresholdAngle)
            {
                SetAnimBool(
                    "TurnLeft",
                    true
                );

                SetAnimBool(
                    "TurnRight",
                    false
                );

                accumulatedTurnAngle =
                    0f;
            }
            else
            {
                SetAnimBool(
                    "TurnLeft",
                    false
                );

                SetAnimBool(
                    "TurnRight",
                    false
                );
            }
        }
        else
        {
            accumulatedTurnAngle =
                0f;

            SetAnimBool(
                "TurnLeft",
                false
            );

            SetAnimBool(
                "TurnRight",
                false
            );
        }
    }

    // TEMP DEBUG: shows what the blend tree receives. Untick in Inspector when done.
    [Header("Debug")]
    [SerializeField] private bool showAnimDebug = true;

    void OnGUI()
    {
        if (!showAnimDebug || animator == null)
            return;

        Vector2 raw = GetMoveInput();

        GUI.Label(
            new Rect(10, 10, 600, 80),
            $"rawInput=({raw.x}, {raw.y})  sprinting={isSprinting}  crouch={isCrouching}\n" +
            $"MoveX={animator.GetFloat("MoveX"):F2}  MoveY={animator.GetFloat("MoveY"):F2}  Speed={animator.GetFloat("Speed"):F2}\n" +
            $"controller={(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "NONE")}"
        );
    }

    // =========================================================
    // PICKUP ANIMATION
    // =========================================================

    public void TriggerPickup()
    {
        SetAnimTrigger("Pickup");
    }

    // =========================================================
    // SAFE ANIMATOR HELPERS
    // =========================================================

    private void SetAnimBool(
        string paramName,
        bool value)
    {
        if (animator == null)
            return;

        int hash =
            Animator.StringToHash(
                paramName
            );

        if (validAnimParameters.Contains(hash))
        {
            animator.SetBool(
                hash,
                value
            );
        }
    }

    private void SetAnimFloat(
        string paramName,
        float value)
    {
        if (animator == null)
            return;

        int hash =
            Animator.StringToHash(
                paramName
            );

        if (validAnimParameters.Contains(hash))
        {
            animator.SetFloat(
                hash,
                value
            );
        }
    }

    private void SetAnimTrigger(
        string paramName)
    {
        if (animator == null)
            return;

        int hash =
            Animator.StringToHash(
                paramName
            );

        if (validAnimParameters.Contains(hash))
        {
            animator.SetTrigger(
                hash
            );
        }
    }

    private void CacheAnimatorParameters()
    {
        validAnimParameters.Clear();

        if (animator != null &&
            animator.runtimeAnimatorController != null)
        {
            foreach (
                AnimatorControllerParameter param
                in animator.parameters)
            {
                validAnimParameters.Add(
                    param.nameHash
                );
            }
        }
    }

    // =========================================================
    // CAMERA EFFECTS
    // =========================================================

    private void HandleCameraEffects()
    {
        CinemachineCamera activeCam =
            isThirdPerson
            ? vCamThirdPerson
            : vCamFirstPerson;

        if (activeCam == null)
            return;

        float targetFOV =
            isSprinting
            ? sprintFOV
            : normalFOV;

        activeCam.Lens.FieldOfView =
            Mathf.Lerp(
                activeCam.Lens.FieldOfView,
                targetFOV,
                Time.deltaTime *
                fovTransitionSpeed
            );
    }

    // =========================================================
    // FOOTSTEPS
    // =========================================================

    private void HandleFootsteps()
    {
        if (!isGroundedPhysically ||
            currentMoveDirection.magnitude <
            0.1f)
        {
            footstepTimer = 0f;
            return;
        }

        footstepTimer -=
            Time.deltaTime;

        float interval =
            walkFootstepInterval;

        if (isSprinting)
            interval =
                sprintFootstepInterval;
        else if (isCrouching)
            interval =
                crouchFootstepInterval;

        if (footstepTimer <= 0f)
        {
            PlayFootstep();

            footstepTimer =
                interval;
        }
    }

    private void PlayFootstep()
    {
        if (footstepSource == null ||
            footstepSounds == null ||
            footstepSounds.Length == 0)
        {
            return;
        }

        AudioClip clip =
            footstepSounds[
                Random.Range(
                    0,
                    footstepSounds.Length
                )
            ];

        footstepSource.pitch =
            Random.Range(
                0.95f,
                1.05f
            );

        footstepSource.PlayOneShot(
            clip,
            1f
        );
    }

    // =========================================================
    // POWER BOOST
    // =========================================================

    public void ActivatePowerBoost(
        float duration)
    {
        if (boostCoroutine != null)
        {
            StopCoroutine(
                boostCoroutine
            );
        }

        boostCoroutine =
            StartCoroutine(
                BoostSequence(
                    duration
                )
            );
    }

    private IEnumerator BoostSequence(
        float duration)
    {
        isPowerBoosted = true;

        isExhausted = false;

        sprintSpeed = 12f;

        currentStamina =
            maxStamina;

        if (survivalUI != null)
        {
            survivalUI.stamina =
                currentStamina;
        }

        yield return new WaitForSeconds(
            duration
        );

        sprintSpeed =
            originalSprintSpeed;

        isPowerBoosted = false;
    }

    // =========================================================
    // STAMINA ACCESS FOR SAVE SYSTEM
    // =========================================================

    public float GetCurrentStamina()
    {
        return currentStamina;
    }

    public float GetMaxStamina()
    {
        return maxStamina;
    }

    public void SetCurrentStamina(
        float value)
    {
        currentStamina =
            Mathf.Clamp(
                value,
                0f,
                maxStamina
            );

        if (currentStamina <= 0f)
        {
            isExhausted = true;
        }

        if (survivalUI != null)
        {
            survivalUI.stamina =
                currentStamina;
        }
    }

    public void SetSurvivalUI(
        SurvivalUIManager newSurvivalUI)
    {
        survivalUI =
            newSurvivalUI;

        if (survivalUI != null)
        {
            survivalUI.stamina =
                currentStamina;
        }
    }

    // =========================================================
    // SPEAR ANIMATION METHODS
    // =========================================================

    public void SetSpearCharging(bool charging)
    {
        if (animator == null)
            return;

        SetAnimBool("IsSpearCharging", charging);

        if (!charging)
        {
            SetAnimBool("SpearCharging", false);
        }
        else
        {
            SetAnimBool("SpearCharging", true);
        }
    }

    public void TriggerSpearAttack()
    {
        SetAnimTrigger("SpearStab");
    }

    public void TriggerSpearThrow()
    {
        SetAnimTrigger("SpearThrow");
    }

}