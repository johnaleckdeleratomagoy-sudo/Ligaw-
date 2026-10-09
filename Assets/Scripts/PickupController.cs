using UnityEngine;

public class PickupController : MonoBehaviour
{
    [Header("Pickup Settings")]
    [Tooltip("How far in front of the player the object should float")]
    public float forwardOffset = 2.0f;
    [Tooltip("Height offset relative to the player's pivot (e.g., chest height)")]
    public float heightOffset = 0.5f;
    [Tooltip("How fast the object interpolates to the holding position")]
    public float carrySpeed = 10f;

    private GameObject carriedObject;
    private bool isCarrying = false;

    void Update()
    {
        // Example: Press 'E' to drop the object if carrying
        if (isCarrying && GameInput.Pressed("Interact", KeyCode.E))
        {
            DropObject();
        }
    }

    void LateUpdate()
    {
        if (isCarrying && carriedObject != null)
        {
            KeepObjectInFront();
        }
    }

    /// <summary>
    /// Call this function when you pick up an object (e.g., via Raycast or Trigger)
    /// </summary>
    public void PickupObject(GameObject objToPickUp)
    {
        carriedObject = objToPickUp;
        isCarrying = true;

        // Optional: Disable physics while carrying so it doesn't fight the player
        if (carriedObject.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Optional: Disable colliders so it doesn't push the player around
        if (carriedObject.TryGetComponent<Collider>(out Collider col))
        {
            col.enabled = false;
        }
    }

    private void KeepObjectInFront()
    {
        // 1. Calculate the target position in front of the player
        Vector3 targetPosition = transform.position
                                 + (transform.forward * forwardOffset)
                                 + (transform.up * heightOffset);

        // 2. Smoothly move the object to that position
        carriedObject.transform.position = Vector3.Lerp(carriedObject.transform.position, targetPosition, Time.deltaTime * carrySpeed);

        // 3. Make the object match the player's rotation (optional)
        carriedObject.transform.rotation = Quaternion.Slerp(carriedObject.transform.rotation, transform.rotation, Time.deltaTime * carrySpeed);
    }

    public void DropObject()
    {
        if (carriedObject == null) return;

        // Re-enable physics
        if (carriedObject.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        // Re-enable collision
        if (carriedObject.TryGetComponent<Collider>(out Collider col))
        {
            col.enabled = true;
        }

        carriedObject = null;
        isCarrying = false;
    }
}