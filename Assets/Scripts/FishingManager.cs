using UnityEngine;
using UnityEngine.InputSystem;

public class FishingManager : MonoBehaviour
{
    public static FishingManager Instance;

    [Header("References")]
    [SerializeField] private HotbarManager hotbarManager;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform rodTip;
    [SerializeField] private GameObject bobberPrefab;

    [Header("Fishing")]
    [SerializeField] private float waterLevel = 26f;
    [SerializeField] private float castDistance = 12f;
    [SerializeField] private float maxCastDistance = 30f;
    [SerializeField] private float biteMinTime = 2f;
    [SerializeField] private float biteMaxTime = 8f;

    [Header("Fish")]
    [SerializeField] private ItemDataSO fishItem;
    [SerializeField] private int fishAmount = 1;

    [Header("Line")]
    [SerializeField] private float lineWidth = 0.015f;

    private FishingBobber currentBobber;
    private FishAI bitingFish;

    private LineRenderer fishingLine;

    private bool isCasting;
    private bool fishIsBiting;

    private float biteTimer;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (hotbarManager == null)
        {
            hotbarManager =
                FindFirstObjectByType<HotbarManager>();
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        CreateFishingLine();
    }

    private void Update()
    {
        if (!IsHoldingFishingRod())
        {
            if (isCasting)
            {
                CancelFishing();
            }

            return;
        }

        if (Mouse.current == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleFishingClick();
        }

        UpdateFishingLine();
        UpdateBiteTimer();
    }

    // =========================================================
    // INPUT
    // =========================================================

    private void HandleFishingClick()
    {
        if (!isCasting)
        {
            CastFishingLine();
            return;
        }

        if (fishIsBiting)
        {
            CatchFish();
        }
        else
        {
            Debug.Log(
                "[Fishing] The fish has not bitten yet."
            );
        }
    }

    // =========================================================
    // CAST
    // =========================================================

    private void CastFishingLine()
    {
        if (bobberPrefab == null)
        {
            Debug.LogError(
                "[Fishing] Bobber Prefab is not assigned."
            );

            return;
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (playerCamera == null)
        {
            Debug.LogError(
                "[Fishing] Player camera not found."
            );

            return;
        }

        Vector3 castPosition;

        if (!GetWaterAimPosition(out castPosition))
        {
            Debug.Log(
                "[Fishing] Aim at the lake water."
            );

            return;
        }

        GameObject bobberObject =
            Instantiate(
                bobberPrefab,
                castPosition,
                Quaternion.identity
            );

        currentBobber =
            bobberObject.GetComponent<FishingBobber>();

        if (currentBobber == null)
        {
            Debug.LogError(
                "[Fishing] Bobber prefab needs FishingBobber."
            );

            Destroy(bobberObject);
            return;
        }

        currentBobber.Initialize(
            this,
            castPosition
        );

        isCasting = true;
        fishIsBiting = false;
        bitingFish = null;

        biteTimer =
            Random.Range(
                biteMinTime,
                biteMaxTime
            );

        Debug.Log(
            "[Fishing] Cast successful. Waiting for fish..."
        );
    }

    // =========================================================
    // FIND WATER POSITION
    // =========================================================

    private bool GetWaterAimPosition(
        out Vector3 position)
    {
        position = Vector3.zero;

        Ray ray =
            playerCamera.ScreenPointToRay(
                new Vector2(
                    Screen.width * 0.5f,
                    Screen.height * 0.5f
                )
            );

        Plane waterPlane =
            new Plane(
                Vector3.up,
                new Vector3(
                    0f,
                    waterLevel,
                    0f
                )
            );

        float distance;

        if (!waterPlane.Raycast(
            ray,
            out distance
        ))
        {
            return false;
        }

        if (distance <= 0f ||
            distance > maxCastDistance)
        {
            return false;
        }

        position =
            ray.GetPoint(distance);

        return true;
    }

    // =========================================================
    // BITE TIMER
    // =========================================================

    private void UpdateBiteTimer()
    {
        if (!isCasting)
            return;

        if (fishIsBiting)
            return;

        biteTimer -= Time.deltaTime;

        if (biteTimer <= 0f)
        {
            // The fish AI will actually trigger the bite.
            biteTimer = 0f;
        }
    }

    // =========================================================
    // FISH BITE
    // =========================================================

    public void FishBites(FishAI fish)
    {
        if (!isCasting)
            return;

        if (fishIsBiting)
            return;

        if (fish == null)
            return;

        fishIsBiting = true;
        bitingFish = fish;

        if (currentBobber != null)
        {
            currentBobber.SetBiting(true);
        }

        Debug.Log(
            "[Fishing] FISH BITES! REEL NOW!"
        );
    }

    // =========================================================
    // CATCH
    // =========================================================

    private void CatchFish()
    {
        if (!fishIsBiting)
            return;

        if (fishItem == null)
        {
            Debug.LogError(
                "[Fishing] Fish ItemDataSO is not assigned."
            );

            return;
        }

        if (hotbarManager == null)
        {
            hotbarManager =
                FindFirstObjectByType<HotbarManager>();
        }

        if (hotbarManager == null)
        {
            Debug.LogError(
                "[Fishing] HotbarManager not found."
            );

            return;
        }

        bool added =
            hotbarManager.TryAddToHotbar(
                fishItem,
                fishAmount
            );

        if (!added)
        {
            Debug.Log(
                "[Fishing] No space for the fish."
            );

            return;
        }

        if (bitingFish != null)
        {
            bitingFish.OnCaught();
        }

        Debug.Log(
            "[Fishing] CAUGHT: " +
            fishItem.itemName
        );

        StopFishing();
    }

    // =========================================================
    // STOP
    // =========================================================

    private void StopFishing()
    {
        if (currentBobber != null)
        {
            Destroy(
                currentBobber.gameObject
            );

            currentBobber = null;
        }

        fishIsBiting = false;
        bitingFish = null;
        isCasting = false;
        biteTimer = 0f;
    }

    public void CancelFishing()
    {
        StopFishing();
    }

    // =========================================================
    // LINE
    // =========================================================

    private void CreateFishingLine()
    {
        GameObject lineObject =
            new GameObject(
                "FishingLine"
            );

        lineObject.transform.SetParent(
            transform
        );

        fishingLine =
            lineObject.AddComponent<LineRenderer>();

        fishingLine.positionCount = 2;
        fishingLine.startWidth = lineWidth;
        fishingLine.endWidth = lineWidth;

        fishingLine.enabled = false;

        Shader shader =
            Shader.Find("HDRP/Unlit");

        if (shader != null)
        {
            fishingLine.material =
                new Material(shader);
        }
    }

    private void UpdateFishingLine()
    {
        if (fishingLine == null)
            return;

        if (!isCasting ||
            currentBobber == null)
        {
            fishingLine.enabled = false;
            return;
        }

        Transform startPoint =
            rodTip;

        if (startPoint == null)
        {
            startPoint =
                playerCamera.transform;
        }

        fishingLine.enabled = true;

        fishingLine.SetPosition(
            0,
            startPoint.position
        );

        fishingLine.SetPosition(
            1,
            currentBobber.transform.position
        );
    }

    // =========================================================
    // CHECK ROD
    // =========================================================

    public bool IsHoldingFishingRod()
    {
        if (hotbarManager == null)
        {
            hotbarManager =
                FindFirstObjectByType<HotbarManager>();
        }

        if (hotbarManager == null)
            return false;

        HotbarSlot slot =
            GetCurrentHotbarSlot();

        if (slot == null)
            return false;

        InventoryItem itemUI =
            slot.GetComponentInChildren<
                InventoryItem
            >();

        if (itemUI == null)
            return false;

        int itemID = itemUI.GetItemID();

        return itemID > 0;
    }

    private HotbarSlot GetCurrentHotbarSlot()
    {
        if (hotbarManager == null)
            return null;

        if (hotbarManager.hotbarSlots == null)
            return null;

        int index =
            hotbarManager.currentSelectedSlot;

        if (index < 0 ||
            index >= hotbarManager.hotbarSlots.Count)
        {
            return null;
        }

        return hotbarManager.hotbarSlots[index];
    }

}