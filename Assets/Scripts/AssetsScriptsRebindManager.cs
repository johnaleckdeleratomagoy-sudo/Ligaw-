using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class RebindManager : MonoBehaviour
{
    public static RebindManager Instance;

    [Header("Input Actions")]
    public InputActionAsset inputActions;

    private const string PlayerPrefsKey = "LIGAW_REBINDS";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadBindings();
    }

    private void OnEnable()
    {
        if (inputActions != null)
            inputActions.Enable();
    }

    private void OnDisable()
    {
        if (inputActions != null)
            inputActions.Disable();
    }

    public InputAction FindAction(string actionName)
    {
        if (inputActions == null)
        {
            Debug.LogError("RebindManager: Input Action Asset is not assigned.");
            return null;
        }

        InputAction action = inputActions.FindAction(actionName, false);

        if (action == null)
        {
            Debug.LogError(
                "RebindManager: Could not find action: " + actionName
            );
        }

        return action;
    }

    public void SaveBindings()
    {
        if (inputActions == null)
            return;

        string overrides = inputActions.SaveBindingOverridesAsJson();

        PlayerPrefs.SetString(PlayerPrefsKey, overrides);
        PlayerPrefs.Save();

        Debug.Log("LIGAW key bindings saved.");
    }

    public void LoadBindings()
    {
        if (inputActions == null)
            return;

        if (!PlayerPrefs.HasKey(PlayerPrefsKey))
            return;

        string overrides = PlayerPrefs.GetString(PlayerPrefsKey);

        if (!string.IsNullOrEmpty(overrides))
        {
            inputActions.LoadBindingOverridesFromJson(overrides);

            Debug.Log("LIGAW key bindings loaded.");
        }
    }

    public void ResetAllBindings()
    {
        if (inputActions == null)
            return;

        foreach (InputActionMap map in inputActions.actionMaps)
        {
            foreach (InputAction action in map.actions)
            {
                action.RemoveAllBindingOverrides();
            }
        }

        PlayerPrefs.DeleteKey(PlayerPrefsKey);
        PlayerPrefs.Save();

        Debug.Log("LIGAW key bindings reset.");
    }

    public string GetBindingDisplayString(
        string actionName,
        int bindingIndex
    )
    {
        InputAction action = FindAction(actionName);

        if (action == null)
            return "-";

        if (bindingIndex < 0 ||
            bindingIndex >= action.bindings.Count)
        {
            return "-";
        }

        return action.GetBindingDisplayString(
            bindingIndex,
            InputBinding.DisplayStringOptions.DontIncludeInteractions
        );
    }
}