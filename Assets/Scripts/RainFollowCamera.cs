using UnityEngine;

public class RainFollowCamera : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("The camera or player transform to follow.")]
    public Transform target;

    [Tooltip("Height above the target to position the rain emitter.")]
    public float heightOffset = 15f;

    private void Start()
    {
        // Auto-detect main camera if target isn't set
        if (target == null && Camera.main != null)
        {
            target = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Position rain above target, but keep rotation flat
        Vector3 newPos = target.position;
        newPos.y += heightOffset;
        transform.position = newPos;
    }
}