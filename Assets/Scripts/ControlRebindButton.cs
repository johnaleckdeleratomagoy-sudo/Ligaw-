using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class ControlRebindButton : MonoBehaviour
{
    [Header("Input Action")]
    public InputActionReference actionReference;

    [Tooltip("Binding index inside the Input Action.")]
    public int bindingIndex = 0;

    [Header("UI")]
    public TMP_Text keyText;
    public Button button;

    [Header("Rebinding")]
    public string waitingText = "Press a key...";

    private InputActionRebindingExtensions.RebindingOperation rebindingOperation;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(StartRebinding);
    }

    private void Start()
    {
        UpdateKeyText();
    }

    public void StartRebinding()
    {
        if (actionReference == null)
        {
            Debug.LogError(
                gameObject.name +
                ": Input Action Reference is not assigned."
            );

            return;
        }

        InputAction action = actionReference.action;

        if (action == null)
        {
            Debug.LogError(
                gameObject.name +
                ": Input Action is missing."
            );

            return;
        }

        if (bindingIndex < 0 ||
            bindingIndex >= action.bindings.Count)
        {
            Debug.LogError(
                gameObject.name +
                ": Binding index is invalid."
            );

            return;
        }

        if (rebindingOperation != null)
            return;

        if (keyText != null)
            keyText.text = waitingText;

        action.Disable();

        rebindingOperation = action.PerformInteractiveRebinding(bindingIndex)

            .WithControlsExcluding("<Mouse>")

            .WithControlsExcluding("<Gamepad>")

            .OnMatchWaitForAnother(0.1f)

            .OnComplete(operation =>
            {
                operation.Dispose();
                rebindingOperation = null;

                action.Enable();

                UpdateKeyText();

                if (RebindManager.Instance != null)
                    RebindManager.Instance.SaveBindings();
            })

            .OnCancel(operation =>
            {
                operation.Dispose();
                rebindingOperation = null;

                action.Enable();

                UpdateKeyText();
            });

        rebindingOperation.Start();
    }

    public void CancelRebinding()
    {
        if (rebindingOperation != null)
        {
            rebindingOperation.Cancel();
        }
    }

    public void UpdateKeyText()
    {
        if (keyText == null)
            return;

        if (actionReference == null)
        {
            keyText.text = "-";
            return;
        }

        InputAction action = actionReference.action;

        if (action == null)
        {
            keyText.text = "-";
            return;
        }

        if (bindingIndex < 0 ||
            bindingIndex >= action.bindings.Count)
        {
            keyText.text = "-";
            return;
        }

        keyText.text = action.GetBindingDisplayString(
            bindingIndex,
            InputBinding.DisplayStringOptions.DontIncludeInteractions
        );
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(StartRebinding);

        if (rebindingOperation != null)
        {
            rebindingOperation.Cancel();
            rebindingOperation.Dispose();
            rebindingOperation = null;
        }
    }
}