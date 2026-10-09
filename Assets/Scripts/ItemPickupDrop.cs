using UnityEngine;

public class ItemPickupDrop : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField] private Transform handHoldPoint; // Drag your RightHand bone here
    [SerializeField] private float pickUpRange = 3f;
    [SerializeField] private KeyCode pickupKey = KeyCode.E;
    [SerializeField] private KeyCode dropKey = KeyCode.G;

    [Header("Throw Force")]
    [SerializeField] private float throwForce = 3f;

    private GameObject currentItem = null;
    private Rigidbody currentItemRb = null;
    private Collider[] currentItemColliders = null;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        // Press E to Pick Up
        if (GameInput.Pressed("Interact", pickupKey) && currentItem == null)
        {
            TryPickUp();
        }
        // Press G to Drop
        else if (GameInput.Pressed("DropItem", dropKey) && currentItem != null)
        {
            DropItem();
        }
    }

    private void TryPickUp()
    {
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickUpRange))
        {
            // First check if the object has a Gatherable component
            Gatherable gatherable = hit.collider.GetComponentInParent<Gatherable>();

            if (gatherable != null)
            {
                // Trigger the gather logic (adds to inventory/hotbar and destroys/cleans up)
                gatherable.Gather();
            }
            // Fallback: physical physics pickup if it's an object with a Rigidbody
            else if (hit.collider.attachedRigidbody != null)
            {
                PickUpPhysicalItem(hit.collider.attachedRigidbody.gameObject);
            }
        }
    }

    /// <summary>
    /// Call this function from your Crafting or Inventory script to equip items directly.
    /// </summary>
    public void EquipItem(GameObject item)
    {
        // Drop or clear existing held item if hand is occupied
        if (currentItem != null)
        {
            DropItem();
        }

        // Handle prefabs vs in-scene objects
        if (!item.scene.IsValid())
        {
            currentItem = Instantiate(item, handHoldPoint);
        }
        else
        {
            currentItem = item;
            currentItem.transform.SetParent(handHoldPoint);
        }

        // Force item active in case it was hidden in inventory
        currentItem.SetActive(true);

        // Apply custom offsets if EquipableItem script is attached to the item
        EquipableItem equipable = currentItem.GetComponent<EquipableItem>();
        if (equipable != null)
        {
            currentItem.transform.localPosition = equipable.positionOffset;
            currentItem.transform.localRotation = Quaternion.Euler(equipable.rotationOffset);
            currentItem.transform.localScale = equipable.scaleOffset;
        }
        else
        {
            // Default snap alignment
            currentItem.transform.localPosition = Vector3.zero;
            currentItem.transform.localRotation = Quaternion.identity;
            currentItem.transform.localScale = Vector3.one;
        }

        // Fetch physics references
        currentItemRb = currentItem.GetComponent<Rigidbody>();
        currentItemColliders = currentItem.GetComponentsInChildren<Collider>();

        // Disable physics while held
        if (currentItemRb != null)
        {
            currentItemRb.isKinematic = true;
        }

        // Disable all colliders so it doesn't push player around
        if (currentItemColliders != null)
        {
            foreach (Collider col in currentItemColliders)
            {
                col.enabled = false;
            }
        }
    }

    private void PickUpPhysicalItem(GameObject item)
    {
        EquipItem(item);
    }

    private void DropItem()
    {
        if (currentItem == null) return;

        // Detach from hand
        currentItem.transform.SetParent(null);

        // Re-enable colliders
        if (currentItemColliders != null)
        {
            foreach (Collider col in currentItemColliders)
            {
                col.enabled = true;
            }
        }

        // Re-enable physics and toss slightly forward
        if (currentItemRb != null)
        {
            currentItemRb.isKinematic = false;
            currentItemRb.AddForce(mainCamera.transform.forward * throwForce, ForceMode.Impulse);
        }

        // Clear reference
        currentItem = null;
        currentItemRb = null;
        currentItemColliders = null;
    }
}