using UnityEngine;
public class TreeResource : MonoBehaviour
{
    [Header("Tree Stats")]
    public string treeName = "Oak Tree";
    public int maxHealth = 3;
    private int currentHealth;

    [Header("Rewards / Prefabs")]
    [Tooltip("The item given or log prefab spawned when tree falls")]
    public GameObject logPrefab;
    public int logsToSpawn = 2;

    [Header("Effects")]
    public ParticleSystem woodChipFX; // Optional particle system

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void HitTree(int damage = 1)
    {
        currentHealth -= damage;

        // Play chop effect if assigned
        if (woodChipFX != null)
        {
            woodChipFX.Play();
        }

        Debug.Log($"{treeName} took {damage} damage! Health remaining: {currentHealth}");

        if (currentHealth <= 0)
        {
            ChopDown();
        }
    }

    private void ChopDown()
    {
        // Spawn logs/wood at the tree position
        if (logPrefab != null)
        {
            for (int i = 0; i < logsToSpawn; i++)
            {
                Vector3 spawnPos = transform.position + Vector3.up * 0.5f + Random.insideUnitSphere * 0.5f;
                Instantiate(logPrefab, spawnPos, Quaternion.identity);
            }
        }

        // Destroy tree object
        Destroy(gameObject);
    }
}