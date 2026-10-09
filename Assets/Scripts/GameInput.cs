using UnityEngine;

// Reads rebindable actions from RebindManager; falls back to the given key
// when no RebindManager exists (e.g. scene started directly).
public static class GameInput
{
    private static UnityEngine.InputSystem.InputAction Find(string actionName)
    {
        if (RebindManager.Instance == null ||
            RebindManager.Instance.inputActions == null)
        {
            return null;
        }

        return RebindManager.Instance.inputActions.FindAction(actionName, false);
    }

    public static bool Pressed(string actionName, KeyCode fallback)
    {
        var action = Find(actionName);

        return action != null
            ? action.WasPressedThisFrame()
            : Input.GetKeyDown(fallback);
    }

    public static bool Held(string actionName, KeyCode fallback)
    {
        var action = Find(actionName);

        return action != null
            ? action.IsPressed()
            : Input.GetKey(fallback);
    }
}
