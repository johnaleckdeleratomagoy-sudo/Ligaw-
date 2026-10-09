using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(1000)]
public class PlaceableItemManager : MonoBehaviour
{
    [Header("References")]
    public HotbarManager hotbarManager;

    [Header("Placement Settings")]
    public float placementDistance = 5f;
    public LayerMask placementSurfaceMask;
    public bool allowPlacementWhenMaskIsEmpty = true;

    [Header("Preview")]
    public Color validPreviewColor = Color.green;
    public Color invalidPreviewColor = Color.red;
    public bool checkObjectCollisions = true;
    public float collisionPadding = 0.05f;
    [Range(0f, 89f)]
    public float maxSlopeAngle = 40f;

    [Header("Rotation")]
    public KeyCode rotateKey = KeyCode.R;
    public float rotationSpeed = 100f;

    [Header("Rotation Guide")]
    public GameObject rotateGuideText;

    [Header("Placement Offset")]
    public float heightOffset = 0f;

    private GameObject previewObject;
    private ItemDataSO currentItemData;

    private Vector3 previewPosition;
    private Quaternion previewRotation = Quaternion.identity;

    private Camera playerCamera;
    private Transform playerTransform;

    private bool hasValidPlacementPosition;

    private void Start()
    {
        FindReferences();

        HideRotateGuide();
    }

    private void Update()
    {
        FindReferences();

        if (hotbarManager == null)
        {
            HidePreview();
            HideRotateGuide();
            return;
        }

        ItemDataSO selectedItem = GetSelectedItemData();

        if (selectedItem == null ||
            selectedItem.itemType != ItemType.Placeable)
        {
            HidePreview();
            HideRotateGuide();
            return;
        }

        if (selectedItem != currentItemData)
        {
            CreatePreview(selectedItem);
        }

        if (previewObject == null || playerCamera == null)
        {
            return;
        }

        if (Keyboard.current != null &&
            Keyboard.current.rKey.isPressed)
        {
            RotatePreview();
        }

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            PlaceObject();
        }
        if (Time.timeScale <= 0f)
        {
            HidePreview();
            HideRotateGuide();
            return;
        }
    }

    // Runs after Cinemachine has moved the camera this frame, so the
    // preview follows the crosshair instead of lagging one frame behind.
    private void LateUpdate()
    {
        if (previewObject == null ||
            playerCamera == null ||
            Time.timeScale <= 0f)
        {
            return;
        }

        UpdatePlacementPreview();
    }

    private void FindReferences()
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

        if (playerCamera == null)
        {
            GameObject player =
                GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                playerCamera =
                    player.GetComponentInChildren<Camera>();
            }
        }
    }

    private ItemDataSO GetSelectedItemData()
    {
        if (hotbarManager == null)
        {
            return null;
        }

        HotbarSlot currentSlot =
            hotbarManager.GetCurrentSlot();

        if (currentSlot == null)
        {
            return null;
        }

        InventoryItem inventoryItem =
            currentSlot.GetComponentInChildren<InventoryItem>();

        if (inventoryItem == null)
        {
            return null;
        }

        return GetItemDataFromInventoryItem(inventoryItem);
    }

    private ItemDataSO GetItemDataFromInventoryItem(
        InventoryItem inventoryItem)
    {
        if (inventoryItem == null)
        {
            return null;
        }

        MethodInfo method =
            inventoryItem.GetType().GetMethod(
                "GetItemData",
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance
            );

        if (method != null)
        {
            object result =
                method.Invoke(inventoryItem, null);

            if (result is ItemDataSO itemData)
            {
                return itemData;
            }
        }

        PropertyInfo property =
            inventoryItem.GetType().GetProperty(
                "itemData",
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance
            );

        if (property != null)
        {
            object result =
                property.GetValue(inventoryItem);

            if (result is ItemDataSO itemData)
            {
                return itemData;
            }
        }

        FieldInfo field =
            inventoryItem.GetType().GetField(
                "itemData",
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance
            );

        if (field != null)
        {
            object result =
                field.GetValue(inventoryItem);

            if (result is ItemDataSO itemData)
            {
                return itemData;
            }
        }

        return null;
    }

    private void CreatePreview(ItemDataSO itemData)
    {
        HidePreview();

        currentItemData = itemData;

        previewRotation = Quaternion.identity;
        hasValidPlacementPosition = false;

        if (itemData == null)
        {
            HideRotateGuide();
            return;
        }

        if (itemData.physicalPrefab == null)
        {
            Debug.LogWarning(
                "Placeable item '" +
                itemData.itemName +
                "' does not have a Physical Prefab assigned."
            );

            HideRotateGuide();
            return;
        }

        previewObject =
            Instantiate(itemData.physicalPrefab);

        previewObject.name = "PlacementPreview";

        DisablePreviewPhysics();

        SetPreviewVisualState(true);

        ShowRotateGuide();
    }

    private void UpdatePlacementPreview()
    {
        hasValidPlacementPosition = false;

        if (previewObject == null ||
            playerCamera == null)
        {
            return;
        }

        Ray ray =
            playerCamera.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0f)
            );

        // In third person the camera sits behind the player, so start the
        // ray at the player's distance along the view direction. This keeps
        // the placement range measured from the player, not the camera.
        float rayStartOffset = 0f;

        Transform player = GetPlayerTransform();

        if (player != null)
        {
            rayStartOffset = Mathf.Max(
                0f,
                Vector3.Dot(
                    player.position - ray.origin,
                    ray.direction
                )
            );

            ray.origin += ray.direction * rayStartOffset;
        }

        RaycastHit hit = default(RaycastHit);

        bool hitSurface = false;

        int mask = -1;

        if (placementSurfaceMask.value != 0)
        {
            mask = placementSurfaceMask.value;
        }
        else if (allowPlacementWhenMaskIsEmpty)
        {
            mask = Physics.DefaultRaycastLayers;
        }

        if (mask != -1 || placementSurfaceMask.value != 0 ||
            allowPlacementWhenMaskIsEmpty)
        {
            RaycastHit[] hits = Physics.RaycastAll(
                ray,
                placementDistance,
                mask,
                QueryTriggerInteraction.Ignore
            );

            float closest = float.MaxValue;

            foreach (RaycastHit candidate in hits)
            {
                // Never place onto the player's own body.
                if (IsPlayerCollider(candidate.collider))
                {
                    continue;
                }

                if (candidate.distance < closest)
                {
                    closest = candidate.distance;
                    hit = candidate;
                    hitSurface = true;
                }
            }
        }

        if (!hitSurface)
        {
            // Nothing in range: hide the preview instead of leaving it
            // at a stale position.
            previewObject.SetActive(false);

            hasValidPlacementPosition = false;

            return;
        }

        previewObject.SetActive(true);

        previewPosition =
            hit.point +
            Vector3.up * heightOffset;

        previewObject.transform.SetPositionAndRotation(
            previewPosition,
            previewRotation
        );

        // Sit the bottom of the model on the surface, so the pivot
        // position doesn't sink it into (or float it above) the terrain.
        if (TryGetPreviewBounds(out Bounds previewBounds))
        {
            float lift =
                (hit.point.y + heightOffset) - previewBounds.min.y;

            previewPosition += Vector3.up * lift;

            previewObject.transform.position = previewPosition;
        }

        bool valid = true;

        // Reject surfaces that are too steep (cliffs, walls).
        if (valid &&
            Vector3.Angle(hit.normal, Vector3.up) > maxSlopeAngle)
        {
            valid = false;
        }

        if (valid && checkObjectCollisions)
        {
            valid =
                !IsOverlappingAnotherObject(
                    hit.collider
                );
        }

        hasValidPlacementPosition = valid;

        if (valid)
        {
            SetPreviewColor(validPreviewColor);
        }
        else
        {
            SetPreviewColor(invalidPreviewColor);
        }
    }

    private bool IsOverlappingAnotherObject(
        Collider surfaceCollider)
    {
        if (previewObject == null)
        {
            return true;
        }

        // Preview colliders are disabled, so their bounds are empty.
        // Use the renderer bounds instead.
        if (!TryGetPreviewBounds(out Bounds bounds))
        {
            return false;
        }

        // Shrink the box so merely touching the ground or a neighbour
        // doesn't count, and raise it slightly off the surface.
        Vector3 halfExtents =
            Vector3.Max(
                bounds.extents - Vector3.one * collisionPadding,
                Vector3.one * 0.01f
            );

        Vector3 center =
            bounds.center +
            Vector3.up * collisionPadding;

        Collider[] overlaps =
            Physics.OverlapBox(
                center,
                halfExtents,
                Quaternion.identity,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore
            );

        foreach (Collider other in overlaps)
        {
            if (other == null)
            {
                continue;
            }

            if (other.transform.IsChildOf(
                previewObject.transform))
            {
                continue;
            }

            if (surfaceCollider != null &&
                (other == surfaceCollider ||
                 other.transform.IsChildOf(
                     surfaceCollider.transform)))
            {
                continue;
            }

            // The ground and the player never block placement.
            if (other is TerrainCollider || IsPlayerCollider(other))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private Transform GetPlayerTransform()
    {
        if (playerTransform == null)
        {
            GameObject player =
                GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        return playerTransform;
    }

    private bool IsPlayerCollider(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        Transform player = GetPlayerTransform();

        return other.CompareTag("Player") ||
               other.transform.root.CompareTag("Player") ||
               (player != null &&
                other.transform.IsChildOf(player.root));
    }

    private bool TryGetPreviewBounds(out Bounds bounds)
    {
        bounds = default(Bounds);

        if (previewObject == null)
        {
            return false;
        }

        Renderer[] renderers =
            previewObject.GetComponentsInChildren<Renderer>();

        bool found = false;

        foreach (Renderer renderer in renderers)
        {
            if (renderer is ParticleSystemRenderer)
            {
                continue;
            }

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return found;
    }

    private void SetPreviewColor(Color color)
    {
        if (previewObject == null)
        {
            return;
        }

        Renderer[] renderers =
            previewObject.GetComponentsInChildren<Renderer>(
                true
            );

        foreach (Renderer renderer in renderers)
        {
            Material[] materials =
                renderer.materials;

            foreach (Material material in materials)
            {
                if (material == null)
                {
                    continue;
                }

                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor(
                        "_BaseColor",
                        color
                    );
                }
                else if (material.HasProperty("_Color"))
                {
                    material.SetColor(
                        "_Color",
                        color
                    );
                }
            }
        }
    }

    private void RotatePreview()
    {
        if (previewObject == null)
        {
            return;
        }

        float rotationAmount =
            rotationSpeed * Time.deltaTime;

        previewRotation *=
            Quaternion.Euler(
                0f,
                rotationAmount,
                0f
            );

        previewObject.transform.rotation =
            previewRotation;
    }

    private void ShowRotateGuide()
    {
        if (rotateGuideText != null)
        {
            rotateGuideText.SetActive(true);
        }
    }

    private void HideRotateGuide()
    {
        if (rotateGuideText != null)
        {
            rotateGuideText.SetActive(false);
        }
    }

   
private void PlaceObject()
    {
        if (!hasValidPlacementPosition ||
            previewObject == null)
        {
            return;
        }

        if (currentItemData == null ||
            currentItemData.physicalPrefab == null)
        {
            return;
        }

        if (!HasOneItemInCurrentSlot())
        {
            Debug.Log(
                "Cannot place: no item remaining " +
                "in the selected hotbar slot."
            );

            return;
        }

        GameObject placedObject =
            Instantiate(
                currentItemData.physicalPrefab,
                previewPosition,
                previewRotation
            );

        placedObject.name =
            currentItemData.itemName;

        PlacedObjectMarker marker =
            placedObject.AddComponent<PlacedObjectMarker>();

        marker.itemID =
            currentItemData.itemID;

        Rigidbody[] rigidbodies =
            placedObject.GetComponentsInChildren<Rigidbody>();

        foreach (Rigidbody rb in rigidbodies)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Show the "Hold E to Light" UI for a newly placed campfire
        Campfire campfire =
            placedObject.GetComponentInChildren<Campfire>();

        if (campfire != null)
        {
            campfire.ShowLightingPrompt();
        }

        RemoveOneItemFromHotbar();

        Debug.Log(
            "Placed: " +
            currentItemData.itemName
        );
    }


    private bool HasOneItemInCurrentSlot()
    {
        if (hotbarManager == null)
        {
            return false;
        }

        HotbarSlot currentSlot =
            hotbarManager.GetCurrentSlot();

        if (currentSlot == null)
        {
            return false;
        }

        InventoryItem inventoryItem =
            currentSlot.GetComponentInChildren<InventoryItem>();

        if (inventoryItem == null)
        {
            return false;
        }

        return inventoryItem.gameObject.activeInHierarchy;
    }

    private void RemoveOneItemFromHotbar()
    {
        if (hotbarManager == null)
        {
            return;
        }

        HotbarSlot currentSlot =
            hotbarManager.GetCurrentSlot();

        if (currentSlot == null)
        {
            return;
        }

        InventoryItem inventoryItem =
            currentSlot.GetComponentInChildren<InventoryItem>();

        if (inventoryItem == null)
        {
            return;
        }

        inventoryItem.RemoveFromStack(1);

        hotbarManager.NotifyHotbarChanged();
        hotbarManager.UpdateHandVisual();
    }

    private void DisablePreviewPhysics()
    {
        if (previewObject == null)
        {
            return;
        }

        Collider[] colliders =
            previewObject.GetComponentsInChildren<Collider>();

        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }

        Rigidbody[] rigidbodies =
            previewObject.GetComponentsInChildren<Rigidbody>();

        foreach (Rigidbody rb in rigidbodies)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private void SetPreviewVisualState(bool state)
    {
        if (previewObject != null)
        {
            previewObject.SetActive(state);
        }
    }

    private void HidePreview()
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
        }

        currentItemData = null;
        hasValidPlacementPosition = false;

        HideRotateGuide();
    }

    private void OnDisable()
    {
        HidePreview();
        HideRotateGuide();
    }
}