using UnityEngine;
using System.Collections.Generic;

public class RockSpawner : MonoBehaviour
{
    [Header("Rock Prefab")]
    public GameObject rockPrefab;

    [Header("Rock Item Data")]
    public ItemDataSO rockItemData;

    [Header("Terrain")]
    [Tooltip("Assign the Terrain where rocks should spawn.")]
    public Terrain targetTerrain;

    [Tooltip("How far above the terrain the rock should be placed.")]
    public float terrainHeightOffset = 0.05f;

    [Header("Random Spawn Settings")]
    [Tooltip("How many rocks should spawn.")]
    public int numberOfRocks = 30;

    [Tooltip("Minimum distance between rocks.")]
    public float minimumDistanceBetweenRocks = 4f;

    [Tooltip("Distance from the edge of the terrain.")]
    public float terrainEdgeMargin = 5f;

    [Tooltip("Maximum attempts to find a valid location.")]
    public int maximumSpawnAttempts = 50;

    [Header("World Pickup ID")]
    [Tooltip("Example: Forest_Rock_")]
    public string worldPickupIDPrefix = "Forest_Rock_";

    [Header("Spawn Settings")]
    public bool spawnOnStart = true;

    private readonly List<GameObject> spawnedRocks =
        new List<GameObject>();

    private readonly List<Vector3> usedSpawnPositions =
        new List<Vector3>();

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnRocks();
        }
    }

    public void SpawnRocks()
    {
        if (rockPrefab == null)
        {
            Debug.LogError(
                "[RockSpawner] Rock Prefab is not assigned!"
            );

            return;
        }

        if (rockItemData == null)
        {
            Debug.LogError(
                "[RockSpawner] Rock Item Data is not assigned!"
            );

            return;
        }

        if (targetTerrain == null)
        {
            Debug.LogError(
                "[RockSpawner] Target Terrain is not assigned!"
            );

            return;
        }

        if (numberOfRocks <= 0)
        {
            Debug.LogWarning(
                "[RockSpawner] Number of Rocks must be greater than 0."
            );

            return;
        }

        spawnedRocks.Clear();
        usedSpawnPositions.Clear();

        TerrainData terrainData =
            targetTerrain.terrainData;

        Vector3 terrainPosition =
            targetTerrain.transform.position;

        Vector3 terrainSize =
            terrainData.size;

        int successfullySpawned = 0;

        for (int i = 0; i < numberOfRocks; i++)
        {
            string worldID =
                worldPickupIDPrefix +
                (i + 1).ToString("000");

            // Check if this rock was already collected
            if (WorldPickupSave.Instance != null &&
                WorldPickupSave.Instance.IsCollected(worldID))
            {
                Debug.Log(
                    "[RockSpawner] Skipping collected rock: " +
                    worldID
                );

                continue;
            }

            Vector3 spawnPosition;

            bool foundPosition =
                FindRandomTerrainPosition(
                    terrainPosition,
                    terrainSize,
                    out spawnPosition
                );

            if (!foundPosition)
            {
                Debug.LogWarning(
                    "[RockSpawner] Could not find a valid position for " +
                    worldID
                );

                continue;
            }

            // Find terrain height
            float terrainHeight =
                targetTerrain.SampleHeight(
                    spawnPosition
                );

            terrainHeight += terrainPosition.y;

            // Put rock on top of terrain
            spawnPosition.y =
                terrainHeight +
                terrainHeightOffset;

            // Random rotation
            Quaternion randomRotation =
                Quaternion.Euler(
                    0f,
                    Random.Range(0f, 360f),
                    0f
                );

            // Spawn rock
            GameObject newRock =
                Instantiate(
                    rockPrefab,
                    spawnPosition,
                    randomRotation,
                    transform
                );

            newRock.name =
                "Rock_" +
                (i + 1).ToString("000");

            // Find Gatherable
            Gatherable gatherable =
                newRock.GetComponent<Gatherable>();

            if (gatherable == null)
            {
                gatherable =
                    newRock.AddComponent<Gatherable>();
            }

            // Assign rock ItemData
            gatherable.itemDataAsset =
                rockItemData;

            // Assign unique World Pickup ID
            gatherable.worldPickupID =
                worldID;

            spawnedRocks.Add(newRock);

            usedSpawnPositions.Add(
                spawnPosition
            );

            successfullySpawned++;

            Debug.Log(
                "[RockSpawner] Spawned " +
                newRock.name +
                " at " +
                spawnPosition +
                " | ID: " +
                worldID
            );
        }

        Debug.Log(
            "[RockSpawner] Finished spawning. " +
            "Rocks spawned: " +
            successfullySpawned
        );
    }

    private bool FindRandomTerrainPosition(
        Vector3 terrainPosition,
        Vector3 terrainSize,
        out Vector3 result
    )
    {
        result = Vector3.zero;

        for (int attempt = 0;
             attempt < maximumSpawnAttempts;
             attempt++)
        {
            float randomX =
                Random.Range(
                    terrainEdgeMargin,
                    terrainSize.x - terrainEdgeMargin
                );

            float randomZ =
                Random.Range(
                    terrainEdgeMargin,
                    terrainSize.z - terrainEdgeMargin
                );

            Vector3 candidate =
                new Vector3(
                    terrainPosition.x + randomX,
                    terrainPosition.y,
                    terrainPosition.z + randomZ
                );

            bool tooClose = false;

            foreach (Vector3 usedPosition
                     in usedSpawnPositions)
            {
                Vector2 candidateXZ =
                    new Vector2(
                        candidate.x,
                        candidate.z
                    );

                Vector2 usedXZ =
                    new Vector2(
                        usedPosition.x,
                        usedPosition.z
                    );

                float distance =
                    Vector2.Distance(
                        candidateXZ,
                        usedXZ
                    );

                if (distance <
                    minimumDistanceBetweenRocks)
                {
                    tooClose = true;
                    break;
                }
            }

            if (tooClose)
            {
                continue;
            }

            result = candidate;

            return true;
        }

        return false;
    }

    public void ClearSpawnedRocks()
    {
        foreach (GameObject rock
                 in spawnedRocks)
        {
            if (rock != null)
            {
                Destroy(rock);
            }
        }

        spawnedRocks.Clear();
        usedSpawnPositions.Clear();
    }
}