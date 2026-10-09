using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraSwitch : MonoBehaviour
{
    [Header("Cinemachine Cameras")]
    [SerializeField] private CinemachineCamera vCamFirstPerson;
    [SerializeField] private CinemachineCamera vCamThirdPerson;

    [Header("Player Mesh Settings")]
    [SerializeField] private Renderer[] playerMeshes; // Drag both Alpha_Joints & Alpha_Surface here

    private bool isThirdPerson = true;

    void Start()
    {
        SetPriority(vCamThirdPerson, 20);
        SetPriority(vCamFirstPerson, 10);
    }

    void Update()
    {
        if (GameInput.Pressed("Camera Switch", KeyCode.V))
        {
            TogglePerspective();
        }
    }

    // Used on death so the death animation is visible.
    public void ForceThirdPerson()
    {
        if (isThirdPerson) return;
        TogglePerspective();
    }

    public void TogglePerspective()
    {
        isThirdPerson = !isThirdPerson;

        SetPriority(vCamThirdPerson, isThirdPerson ? 20 : 10);
        SetPriority(vCamFirstPerson, isThirdPerson ? 10 : 20);

        // Toggle mesh visibility
        ShadowCastingMode mode = isThirdPerson ? ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly;
        foreach (Renderer rend in playerMeshes)
        {
            if (rend != null) rend.shadowCastingMode = mode;
        }
    }

    private void SetPriority(CinemachineCamera cam, int priorityValue)
    {
        if (cam != null) cam.Priority.Value = priorityValue;
    }
}