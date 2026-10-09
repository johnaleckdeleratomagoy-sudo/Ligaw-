using UnityEngine;

public class PlayerGathering : MonoBehaviour
{
    [Header("Raycast Settings")]
    public float gatherDistance = 3f; // How close player needs to be
    public LayerMask gatherableLayer; // Set this to a custom layer for optimization

    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        // Check if the player presses the interact key (E)
        if (GameInput.Pressed("Interact", KeyCode.E))
        {
            TryGather();
        }
    }

    void TryGather()
    {
        // Create a ray from the center of the screen forward
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        // Cast the ray
        if (Physics.Raycast(ray, out hit, gatherDistance, gatherableLayer))
        {
            // Check if the object we hit has the Gatherable script
            Gatherable resource = hit.collider.GetComponent<Gatherable>();

            if (resource != null)
            {
                resource.Gather();
            }
        }
    }
}