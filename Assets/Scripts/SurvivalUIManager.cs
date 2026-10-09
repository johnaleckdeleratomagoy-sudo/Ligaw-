using UnityEngine;
using UnityEngine.Events;

public class SurvivalUIManager : MonoBehaviour
{
    [System.Serializable]
    public class FloatEvent : UnityEvent<float> { }

    [Header("UI Progress Events (0.0 to 1.0)")]
    public FloatEvent OnHealthChanged;
    public FloatEvent OnStaminaChanged;
    public FloatEvent OnHungerChanged;
    public FloatEvent OnThirstChanged;

    [Header("Stat Values")]
    public float health = 100f;

    [HideInInspector]
    public float stamina = 100f;

    public float hunger = 100f;
    public float thirst = 100f;

    [Header("Base Decay Rates (Per Second)")]
    public float hungerDecayRate = 0.5f;
    public float thirstDecayRate = 0.8f;
    public float passiveHealthDecay = 0f;

    [Header("Penalty Modifiers")]
    public float thirstHealthDrainPenalty = 3.0f;

    [HideInInspector]
    public bool isRunning = false;

    private const float maxStat = 100f;

    // =========================================================
    // UNITY
    // =========================================================

    private void Start()
    {
        ClampStats();
        RefreshUI();
    }

    private void Update()
    {
        HandleDecay();
        ApplySurvivalRules();
        RefreshUI();
    }

    // =========================================================
    // SURVIVAL DECAY
    // =========================================================

    private void HandleDecay()
    {
        hunger = Mathf.Max(
            0f,
            hunger - hungerDecayRate * Time.deltaTime
        );

        thirst = Mathf.Max(
            0f,
            thirst - thirstDecayRate * Time.deltaTime
        );
    }

    // =========================================================
    // SURVIVAL RULES
    // =========================================================

    private void ApplySurvivalRules()
    {
        // If hunger reaches zero,
        // stamina is forced to zero.
        if (hunger <= 0f)
        {
            stamina = 0f;
        }

        // If thirst reaches zero,
        // health starts draining.
        if (thirst <= 0f)
        {
            health = Mathf.Max(
                0f,
                health -
                (passiveHealthDecay +
                 thirstHealthDrainPenalty) *
                Time.deltaTime
            );
        }
        else if (passiveHealthDecay > 0f)
        {
            health = Mathf.Max(
                0f,
                health -
                passiveHealthDecay *
                Time.deltaTime
            );
        }
    }

    // =========================================================
    // UI
    // =========================================================

    public void RefreshUI()
    {
        ClampStats();
        UpdateUI();
    }

    private void UpdateUI()
    {
        OnHealthChanged?.Invoke(
            Mathf.Clamp01(health / maxStat)
        );

        OnStaminaChanged?.Invoke(
            Mathf.Clamp01(stamina / maxStat)
        );

        OnHungerChanged?.Invoke(
            Mathf.Clamp01(hunger / maxStat)
        );

        OnThirstChanged?.Invoke(
            Mathf.Clamp01(thirst / maxStat)
        );
    }

    // =========================================================
    // STAT CLAMPING
    // =========================================================

    private void ClampStats()
    {
        health = Mathf.Clamp(
            health,
            0f,
            maxStat
        );

        stamina = Mathf.Clamp(
            stamina,
            0f,
            maxStat
        );

        hunger = Mathf.Clamp(
            hunger,
            0f,
            maxStat
        );

        thirst = Mathf.Clamp(
            thirst,
            0f,
            maxStat
        );
    }

    // =========================================================
    // CONSUMABLE ITEMS
    // =========================================================

    public bool ConsumeItem(ItemDataSO itemData)
    {
        if (itemData == null)
        {
            Debug.LogWarning(
                "[SurvivalUIManager] ItemDataSO is NULL!"
            );

            return false;
        }

        if (itemData.itemType != ItemType.Consumable)
        {
            Debug.LogWarning(
                "[SurvivalUIManager] Item is not set to Consumable!"
            );

            return false;
        }

        Heal(itemData.healthRestore);

        ReplenishHunger(
            itemData.hungerRestore
        );

        ReplenishThirst(
            itemData.thirstRestore
        );

        RefreshUI();

        Debug.Log(
            "[SurvivalUIManager] Consumed " +
            itemData.itemName +
            " | Health +" +
            itemData.healthRestore +
            " | Hunger +" +
            itemData.hungerRestore +
            " | Thirst +" +
            itemData.thirstRestore
        );

        return true;
    }

    // =========================================================
    // RESTORE HUNGER
    // =========================================================

    public void ReplenishHunger(float amount)
    {
        hunger = Mathf.Min(
            maxStat,
            hunger + amount
        );

        RefreshUI();
    }

    // =========================================================
    // RESTORE THIRST
    // =========================================================

    public void ReplenishThirst(float amount)
    {
        thirst = Mathf.Min(
            maxStat,
            thirst + amount
        );

        RefreshUI();
    }

    // =========================================================
    // RESTORE HEALTH
    // =========================================================

    public void Heal(float amount)
    {
        health = Mathf.Min(
            maxStat,
            health + amount
        );

        RefreshUI();
    }

    // =========================================================
    // SETTERS
    // =========================================================

    public void SetHealth(float value)
    {
        health = Mathf.Clamp(
            value,
            0f,
            maxStat
        );

        RefreshUI();
    }

    public void SetStamina(float value)
    {
        stamina = Mathf.Clamp(
            value,
            0f,
            maxStat
        );

        RefreshUI();
    }

    public void SetHunger(float value)
    {
        hunger = Mathf.Clamp(
            value,
            0f,
            maxStat
        );

        RefreshUI();
    }

    public void SetThirst(float value)
    {
        thirst = Mathf.Clamp(
            value,
            0f,
            maxStat
        );

        RefreshUI();
    }

    // =========================================================
    // GETTERS
    // =========================================================

    public float GetHealth()
    {
        return health;
    }

    public float GetStamina()
    {
        return stamina;
    }

    public float GetHunger()
    {
        return hunger;
    }

    public float GetThirst()
    {
        return thirst;
    }

    // =========================================================
    // SAVE / LOAD
    // =========================================================

    /// <summary>
    /// Loads the four survival statistics from a save file.
    /// Used by PlayerSaveData when Resume is pressed.
    /// </summary>
    public void LoadSurvivalStats(
        float savedHealth,
        float savedStamina,
        float savedHunger,
        float savedThirst)
    {
        health = Mathf.Clamp(
            savedHealth,
            0f,
            maxStat
        );

        stamina = Mathf.Clamp(
            savedStamina,
            0f,
            maxStat
        );

        hunger = Mathf.Clamp(
            savedHunger,
            0f,
            maxStat
        );

        thirst = Mathf.Clamp(
            savedThirst,
            0f,
            maxStat
        );

        RefreshUI();

        Debug.Log(
            "[SurvivalUIManager] Survival stats loaded." +
            "\nHealth: " + health +
            "\nStamina: " + stamina +
            "\nHunger: " + hunger +
            "\nThirst: " + thirst
        );
    }

    /// <summary>
    /// Returns the current survival statistics.
    /// Useful for PlayerSaveData.
    /// </summary>
    public void GetSurvivalStats(
        out float currentHealth,
        out float currentStamina,
        out float currentHunger,
        out float currentThirst)
    {
        currentHealth = health;
        currentStamina = stamina;
        currentHunger = hunger;
        currentThirst = thirst;
    }
}