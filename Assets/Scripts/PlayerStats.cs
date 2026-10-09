using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Current Stats")]
    public float currentHealth = 80f;
    public float maxHealth = 100f;

    public float currentHunger = 50f;
    public float maxHunger = 100f;

    public float currentThirst = 40f;
    public float maxThirst = 100f;

    public void RestoreStats(float health, float hunger, float thirst)
    {
        currentHealth = Mathf.Min(currentHealth + health, maxHealth);
        currentHunger = Mathf.Min(currentHunger + hunger, maxHunger);
        currentThirst = Mathf.Min(currentThirst + thirst, maxThirst);

        Debug.Log($"Stats Restored! Health: {currentHealth}/{maxHealth} | Hunger: {currentHunger}/{maxHunger} | Thirst: {currentThirst}/{maxThirst}");
    }
}