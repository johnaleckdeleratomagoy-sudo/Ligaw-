using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class GameplayControlFeedback : MonoBehaviour
{
    [Header("Control Panel")]
    public GameObject controlPanel;

    [Header("Panels That Hide Controls")]
    public GameObject inventoryPanel;
    public GameObject craftingPanel;

    [Header("Control Images")]
    public Image movementImage;
    public Image mouseImage;
    public Image spaceImage;
    public Image shiftImage;
    public Image ctrlImage;
    public Image cImage;
    public Image eImage;
    public Image leftMouseImage;
    public Image gImage;
    public Image hotbarImage;
    public Image vImage;
    public Image escImage;
    public Image iImage;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color pressedColor = new Color(0.5f, 1f, 0.5f, 1f);

    [Header("Pressed Alpha")]
    [Range(0f, 1f)]
    public float pressedAlpha = 0.75f;

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        // ============================================
        // CHECK PANELS
        // ============================================

        bool gamePaused = Time.timeScale == 0f;

        bool inventoryOpen =
            inventoryPanel != null &&
            inventoryPanel.activeInHierarchy;

        bool craftingOpen =
            craftingPanel != null &&
            craftingPanel.activeInHierarchy;

        // Hide controls if the game is paused,
        // inventory is open, or crafting is open.
        bool shouldHideControls =
            gamePaused ||
            inventoryOpen ||
            craftingOpen ||
            HUDPanelToggle.ControlPanelHiddenByUser;

        if (controlPanel != null)
        {
            if (controlPanel.activeSelf == shouldHideControls)
            {
                controlPanel.SetActive(!shouldHideControls);
            }
        }

        // Don't process control feedback while
        // another panel is open.
        if (shouldHideControls)
            return;

        // ============================================
        // W A S D - MOVEMENT
        // ============================================

        bool movementPressed =
            Keyboard.current.wKey.isPressed ||
            Keyboard.current.aKey.isPressed ||
            Keyboard.current.sKey.isPressed ||
            Keyboard.current.dKey.isPressed;

        SetImageState(
            movementImage,
            movementPressed
        );

        // ============================================
        // MOUSE - LOOK / AIM
        // ============================================

        bool mouseMoving =
            Mouse.current != null &&
            Mouse.current.delta.ReadValue().sqrMagnitude > 0.01f;

        SetImageState(
            mouseImage,
            mouseMoving
        );

        // ============================================
        // SPACE - JUMP
        // ============================================

        SetImageState(
            spaceImage,
            Keyboard.current.spaceKey.isPressed
        );

        // ============================================
        // SHIFT - SPRINT
        // ============================================

        SetImageState(
            shiftImage,
            Keyboard.current.leftShiftKey.isPressed ||
            Keyboard.current.rightShiftKey.isPressed
        );

        // ============================================
        // CTRL - CROUCH
        // ============================================

        SetImageState(
            ctrlImage,
            Keyboard.current.leftCtrlKey.isPressed ||
            Keyboard.current.rightCtrlKey.isPressed
        );

        // ============================================
        // C - CRAFTING
        // ============================================

        SetImageState(
            cImage,
            Keyboard.current.cKey.isPressed
        );

        // ============================================
        // E - INTERACT
        // ============================================

        SetImageState(
            eImage,
            Keyboard.current.eKey.isPressed
        );

        // ============================================
        // LEFT MOUSE - ATTACK / USE ITEM
        // ============================================

        if (Mouse.current != null)
        {
            SetImageState(
                leftMouseImage,
                Mouse.current.leftButton.isPressed
            );
        }

        // ============================================
        // G - DROP ITEM
        // ============================================

        SetImageState(
            gImage,
            Keyboard.current.gKey.isPressed
        );

        // ============================================
        // 1 - 9 HOTBAR
        // ============================================

        bool hotbarPressed =
            Keyboard.current.digit1Key.isPressed ||
            Keyboard.current.digit2Key.isPressed ||
            Keyboard.current.digit3Key.isPressed ||
            Keyboard.current.digit4Key.isPressed ||
            Keyboard.current.digit5Key.isPressed ||
            Keyboard.current.digit6Key.isPressed ||
            Keyboard.current.digit7Key.isPressed ||
            Keyboard.current.digit8Key.isPressed ||
            Keyboard.current.digit9Key.isPressed;

        SetImageState(
            hotbarImage,
            hotbarPressed
        );

        // ============================================
        // V - CAMERA SWITCH
        // ============================================

        SetImageState(
            vImage,
            Keyboard.current.vKey.isPressed
        );

        // ============================================
        // ESC - PAUSE
        // ============================================

        SetImageState(
            escImage,
            Keyboard.current.escapeKey.isPressed
        );

        // ============================================
        // I - INVENTORY
        // ============================================

        SetImageState(
            iImage,
            Keyboard.current.iKey.isPressed
        );
    }

    // ==================================================
    // CONTROL IMAGE STATE
    // ==================================================

    private void SetImageState(Image image, bool pressed)
    {
        if (image == null)
            return;

        if (pressed)
        {
            Color color = pressedColor;
            color.a = pressedAlpha;

            image.color = color;
        }
        else
        {
            image.color = normalColor;
        }
    }
}