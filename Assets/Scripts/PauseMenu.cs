using UnityEngine;
using UnityEngine.Video;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance;

    [Header("Player Control To Freeze")]
    [Tooltip("Drag your main Player GameObject here.")]
    public GameObject playerGameObject;

    [Tooltip("Drag your Player Movement/Controller script here.")]
    public MonoBehaviour playerControllerScript;

    [Tooltip("Drag your Mouse Look / Camera Look script here.")]
    public MonoBehaviour cameraLookScript;

    [Header("Panels")]
    public GameObject pausePanel;
    public GameObject settingsPanel;

    [Header("UI")]
    public GameObject hotbar;
    public GameObject timerUI;
    public GameObject crosshairUI;
    public GameObject survivalUIContainer;
    public GameObject descriptionPanel;
    public GameObject taskPanel;

    [Header("Background")]
    public GameObject darkOverlay;
    public GameObject videoBackground;

    private VideoPlayer videoPlayer;

    [Header("HDRP Cinematic Darkening")]
    public Volume settingsDarknessVolume;

    [Header("Save System")]
    public SaveSystem saveSystem;

    private CanvasGroup pauseCG;
    private CanvasGroup settingsCG;
    private CanvasGroup hotbarCG;

    private Stack<GameObject> menuStack =
        new Stack<GameObject>();

    private bool isPaused = false;

    // Prevents rapid ESC presses from starting
    // multiple pause/resume transitions.
    private bool isTransitioning = false;

    [SerializeField]
    private float fadeSpeed = 8f;


    // ==========================================
    // AWAKE
    // ==========================================

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        pauseCG =
            GetOrAddCanvasGroup(pausePanel);

        settingsCG =
            GetOrAddCanvasGroup(settingsPanel);

        hotbarCG =
            GetOrAddCanvasGroup(hotbar);

        if (videoBackground != null)
        {
            videoPlayer =
                videoBackground.GetComponent<VideoPlayer>();
        }

        if (saveSystem == null)
        {
            saveSystem =
                Object.FindFirstObjectByType<SaveSystem>();
        }

        ResetAll();
    }


    // ==========================================
    // UPDATE
    // ==========================================

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleBackAction();
        }

        EnforceCursorState();
    }


    // ==========================================
    // CURSOR
    // ==========================================

    private void EnforceCursorState()
    {
        if (isPaused)
        {
            Cursor.lockState =
                CursorLockMode.None;

            Cursor.visible = true;
        }
    }


    // ==========================================
    // RESET
    // ==========================================

    private void ResetAll()
    {
        StopAllCoroutines();

        isPaused = false;
        isTransitioning = false;

        Time.timeScale = 1f;

        menuStack.Clear();

        SetActiveInstant(pausePanel, false);
        SetActiveInstant(settingsPanel, false);
        SetActiveInstant(darkOverlay, false);
        SetActiveInstant(videoBackground, false);

        if (settingsDarknessVolume != null)
        {
            settingsDarknessVolume.weight = 0f;
        }

        SetActiveInstant(timerUI, true);
        SetActiveInstant(crosshairUI, true);
        SetActiveInstant(survivalUIContainer, true);
        SetActiveInstant(taskPanel, true);

        SetActiveInstant(descriptionPanel, false);

        SetUIState(hotbarCG, true);

        SetPlayerFrozen(false);

        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible = false;
    }


    // ==========================================
    // PAUSE
    // ==========================================

    public void Pause()
    {
        // Do not allow another transition
        // while one is already running.
        if (isTransitioning)
            return;

        if (isPaused)
            return;

        isTransitioning = true;

        // Close Inventory first.
        InventoryToggle inventory =
            Object.FindFirstObjectByType<InventoryToggle>();

        if (inventory != null)
        {
            inventory.CloseInventory();
        }

        isPaused = true;

        Time.timeScale = 0f;

        SetPlayerFrozen(true);

        HideInteractionUIText();

        if (timerUI)
        {
            timerUI.SetActive(false);
        }

        if (crosshairUI)
        {
            crosshairUI.SetActive(false);
        }

        if (survivalUIContainer)
        {
            survivalUIContainer.SetActive(false);
        }

        if (descriptionPanel)
        {
            descriptionPanel.SetActive(false);
        }

        if (taskPanel)
        {
            taskPanel.SetActive(false);
        }

        if (settingsDarknessVolume != null)
        {
            settingsDarknessVolume.weight = 1f;
        }

        if (darkOverlay)
        {
            darkOverlay.SetActive(true);
        }

        if (videoBackground)
        {
            videoBackground.SetActive(true);

            if (videoPlayer != null)
            {
                videoPlayer.Play();
            }
        }

        // Stop any old fade operations.
        StopCoroutine(nameof(FadeHotbarOut));

        StartCoroutine(FadeHotbarOut());

        OpenMenu(pausePanel);

        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible = true;

        // Pause transition is finished after
        // the menu has been opened.
        isTransitioning = false;
    }


    // ==========================================
    // HOTBAR FADE OUT
    // ==========================================

    private IEnumerator FadeHotbarOut()
    {
        if (hotbarCG == null)
            yield break;

        yield return StartCoroutine(
            FadeUI(
                hotbarCG,
                0f,
                false
            )
        );
    }


    // ==========================================
    // RESUME
    // ==========================================

    public void Resume()
    {
        if (isTransitioning)
            return;

        if (!isPaused)
            return;

        isTransitioning = true;

        isPaused = false;

        Time.timeScale = 1f;

        SetPlayerFrozen(false);

        if (darkOverlay)
        {
            darkOverlay.SetActive(false);
        }

        if (settingsDarknessVolume != null)
        {
            settingsDarknessVolume.weight = 0f;
        }

        if (videoBackground)
        {
            if (videoPlayer != null)
            {
                videoPlayer.Pause();
            }

            videoBackground.SetActive(false);
        }

        // Stop old fade operations before starting
        // the resume fade.
        StopCoroutine(nameof(FadeHotbarIn));

        // Hide all menus immediately.
        while (menuStack.Count > 0)
        {
            GameObject menu =
                menuStack.Pop();

            if (menu == null)
                continue;

            CanvasGroup cg =
                menu.GetComponent<CanvasGroup>();

            if (cg != null)
            {
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }

            menu.SetActive(false);
        }

        StartCoroutine(FadeHotbarIn());

        if (timerUI)
        {
            timerUI.SetActive(true);
        }

        if (crosshairUI)
        {
            crosshairUI.SetActive(true);
        }

        if (survivalUIContainer)
        {
            survivalUIContainer.SetActive(true);
        }

        if (taskPanel)
        {
            taskPanel.SetActive(true);
        }

        if (descriptionPanel)
        {
            descriptionPanel.SetActive(false);
        }

        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible = false;

        isTransitioning = false;
    }


    // ==========================================
    // HOTBAR FADE IN
    // ==========================================

    private IEnumerator FadeHotbarIn()
    {
        if (hotbarCG == null)
            yield break;

        yield return StartCoroutine(
            FadeUI(
                hotbarCG,
                1f,
                true
            )
        );
    }


    // ==========================================
    // OPEN SETTINGS
    // ==========================================

    public void OpenSettings()
    {
        if (isTransitioning)
            return;

        if (!isPaused)
            return;

        OpenMenu(settingsPanel);
    }


    // ==========================================
    // BACK FROM SETTINGS
    // ==========================================

    public void BackFromSettings()
    {
        if (isTransitioning)
            return;

        if (!isPaused)
        {
            Debug.LogWarning(
                "BackFromSettings called, but Pause Menu is not paused."
            );

            return;
        }

        if (menuStack.Count > 1)
        {
            isTransitioning = true;

            GameObject current =
                menuStack.Pop();

            if (current != null)
            {
                CanvasGroup currentCG =
                    current.GetComponent<CanvasGroup>();

                if (currentCG != null)
                {
                    currentCG.alpha = 0f;
                    currentCG.interactable = false;
                    currentCG.blocksRaycasts = false;
                }

                current.SetActive(false);
            }

            GameObject previous =
                menuStack.Peek();

            if (previous != null)
            {
                previous.SetActive(true);

                CanvasGroup previousCG =
                    previous.GetComponent<CanvasGroup>();

                if (previousCG != null)
                {
                    previousCG.alpha = 1f;
                    previousCG.interactable = true;
                    previousCG.blocksRaycasts = true;
                }
            }

            isTransitioning = false;

            Debug.Log(
                "Returned to Pause Menu."
            );
        }
        else
        {
            Debug.LogWarning(
                "There is no previous menu in the Pause Menu stack."
            );
        }
    }


    // ==========================================
    // ESC / BACK ACTION
    // ==========================================

    public void HandleBackAction()
    {
        // IMPORTANT:
        // Ignore ESC if a transition is already happening.
        if (isTransitioning)
            return;

        if (!isPaused)
        {
            Pause();
        }
        else if (menuStack.Count > 1)
        {
            BackFromSettings();
        }
        else
        {
            Resume();
        }
    }


    // ==========================================
    // QUIT TO MAIN MENU
    // ==========================================

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene("MainMenu 1");
    }

    // ==========================================
    // SAVE GAME
    // ==========================================

    public void SaveGame()
    {
        if (saveSystem == null)
        {
            saveSystem =
                Object.FindFirstObjectByType<SaveSystem>();
        }

        if (saveSystem != null)
        {
            saveSystem.SaveGame();
        }
        else
        {
            Debug.LogError(
                "[PauseMenu] SaveSystem not found!"
            );
        }
    }


    // ==========================================
    // OPEN MENU
    // ==========================================

    private void OpenMenu(GameObject menu)
    {
        if (menu == null)
        {
            Debug.LogError(
                "Menu is NOT assigned!"
            );

            return;
        }

        // Disable the currently visible menu.
        if (menuStack.Count > 0)
        {
            GameObject top =
                menuStack.Peek();

            if (top != null && top != menu)
            {
                CanvasGroup topCG =
                    top.GetComponent<CanvasGroup>();

                if (topCG != null)
                {
                    topCG.alpha = 0f;
                    topCG.interactable = false;
                    topCG.blocksRaycasts = false;
                }

                top.SetActive(false);
            }
        }

        menu.SetActive(true);

        CanvasGroup menuCG =
            menu.GetComponent<CanvasGroup>();

        if (menuCG != null)
        {
            menuCG.alpha = 1f;
            menuCG.interactable = true;
            menuCG.blocksRaycasts = true;
        }

        // Avoid adding the same menu multiple times.
        if (menuStack.Count == 0 ||
            menuStack.Peek() != menu)
        {
            menuStack.Push(menu);
        }
    }


    // ==========================================
    // PLAYER FREEZE
    // ==========================================

    private void SetPlayerFrozen(bool freeze)
    {
        if (playerControllerScript != null)
        {
            playerControllerScript.enabled =
                !freeze;
        }

        if (cameraLookScript != null)
        {
            cameraLookScript.enabled =
                !freeze;
        }

        PlayerInteraction interactionScript =
            Object.FindFirstObjectByType<PlayerInteraction>();

        if (interactionScript != null)
        {
            interactionScript.enabled =
                !freeze;
        }

        if (playerControllerScript == null ||
            cameraLookScript == null)
        {
            GameObject targetPlayer =
                playerGameObject;

            if (targetPlayer == null)
            {
                targetPlayer =
                    GameObject.FindGameObjectWithTag("Player");
            }

            if (targetPlayer != null)
            {
                CharacterController cc =
                    targetPlayer.GetComponent<CharacterController>();

                if (cc != null)
                {
                    cc.enabled = !freeze;
                }

                Rigidbody rb =
                    targetPlayer.GetComponent<Rigidbody>();

                if (rb != null)
                {
                    rb.isKinematic = freeze;
                }
            }
        }
    }


    // ==========================================
    // FADE UI
    // ==========================================

    private IEnumerator FadeUI(
        CanvasGroup cg,
        float target,
        bool interactable,
        System.Action onComplete = null)
    {
        if (cg == null)
        {
            yield break;
        }

        cg.interactable =
            interactable;

        cg.blocksRaycasts =
            interactable;

        while (!Mathf.Approximately(
            cg.alpha,
            target))
        {
            cg.alpha =
                Mathf.MoveTowards(
                    cg.alpha,
                    target,
                    fadeSpeed *
                    Time.unscaledDeltaTime
                );

            yield return null;
        }

        cg.alpha = target;

        cg.interactable =
            interactable;

        cg.blocksRaycasts =
            interactable;

        onComplete?.Invoke();
    }


    // ==========================================
    // CANVAS GROUP
    // ==========================================

    private CanvasGroup GetOrAddCanvasGroup(
        GameObject obj)
    {
        if (obj == null)
        {
            return null;
        }

        CanvasGroup cg =
            obj.GetComponent<CanvasGroup>();

        if (cg == null)
        {
            cg =
                obj.AddComponent<CanvasGroup>();
        }

        return cg;
    }


    // ==========================================
    // SET ACTIVE
    // ==========================================

    private void SetActiveInstant(
        GameObject obj,
        bool state)
    {
        if (obj != null)
        {
            obj.SetActive(state);
        }
    }


    // ==========================================
    // UI STATE
    // ==========================================

    private void SetUIState(
        CanvasGroup cg,
        bool active)
    {
        if (cg == null)
        {
            return;
        }

        cg.alpha =
            active ? 1f : 0f;

        cg.interactable =
            active;

        cg.blocksRaycasts =
            active;
    }


    // ==========================================
    // HIDE INTERACTION UI
    // ==========================================

    private void HideInteractionUIText()
    {
        PlayerInteraction interactionScript =
            Object.FindFirstObjectByType<PlayerInteraction>();

        if (interactionScript != null)
        {
            if (interactionScript.interactionTextUI != null)
            {
                interactionScript.interactionTextUI.SetActive(false);
            }
        }
    }
}