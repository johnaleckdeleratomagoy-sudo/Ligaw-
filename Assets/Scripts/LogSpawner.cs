using UnityEngine;
using System.Collections.Generic;

public class LogSpawner : MonoBehaviour
{
    [Header("Log Prefab")]
    public GameObject logPrefab;

    [Header("Log Item Data")]
    public ItemDataSO logItemData;

    [Header("Terrain")]
    [Tooltip("Assign the Terrain where the logs should spawn.")]
    public Terrain targetTerrain;

    [Tooltip("Height above the terrain.")]
    public float terrainHeightOffset = 0.05f;

    [Header("Random Spawn Settings")]
    [Tooltip("How many logs should spawn.")]
    public int numberOfLogs = 20;

    [Tooltip("Minimum distance between spawned logs.")]
    public float minimumDistanceBetweenLogs = 5f;

    [Tooltip("Minimum distance from the edge of the terrain.")]
    public float terrainEdgeMargin = 5f;

    [Tooltip("Maximum attempts to find a valid position for each log.")]
    public int maximumSpawnAttempts = 50;

    [Header("World Pickup ID")]
    [Tooltip("Example: Forest_Log_")]
    public string worldPickupIDPrefix = "Forest_Log_";

    [Header("Spawn Settings")]
    public bool spawnOnStart = true;

    private readonly List<GameObject> spawnedLogs =
        new List<GameObject>();

    private readonly List<Vector3> usedSpawnPositions =
        new List<Vector3>();

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnLogs();
        }
    }

    public void SpawnLogs()
    {
        if (logPrefab == null)
        {
            Debug.LogError(
                "[LogSpawner] Log Prefab is not assigned!"
            );

            return;
        }

        if (logItemData == null)
        {
            Debug.LogError(
                "[LogSpawner] Log Item Data is not assigned!"
            );

            return;
        }

        if (targetTerrain == null)
        {
            Debug.LogError(
                "[LogSpawner] Target Terrain is not assigned!"
            );

            return;
        }

        if (numberOfLogs <= 0)
        {
            Debug.LogWarning(
                "[LogSpawner] Number of Logs must be greater than 0."
            );

            return;
        }

        spawnedLogs.Clear();
        usedSpawnPositions.Clear();

        TerrainData terrainData =
            targetTerrain.terrainData;

        Vector3 terrainPosition =
            targetTerrain.transform.position;

        Vector3 terrainSize =
            terrainData.size;

        int successfullySpawned = 0;

        for (int i = 0; i < numberOfLogs; i++)
        {
            string worldID =
                worldPickupIDPrefix +
                (i + 1).ToString("000");

            // If this particular log was already collected,
            // do not spawn it.
            if (WorldPickupSave.Instance != null &&
                WorldPickupSave.Instance.IsCollected(worldID))
            {
                Debug.Log(
                    "[LogSpawner] Skipping collected log: " +
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
                    "[LogSpawner] Could not find a valid position for " +
                    worldID
                );

                continue;
            }

            // Get terrain height
            float terrainHeight =
                targetTerrain.SampleHeight(
                    spawnPosition
                );

            terrainHeight += terrainPosition.y;

            // Put the log on top of the terrain
            spawnPosition.y =
                terrainHeight +
                terrainHeightOffset;

            // Random rotation around Y
            Quaternion randomRotation =
                Quaternion.Euler(
                    0f,
                    Random.Range(0f, 360f),
                    0f
                );

            // Spawn log
            GameObject newLog =
                Instantiate(
                    logPrefab,
                    spawnPosition,
                    randomRotation,
                    transform
                );

            newLog.name =
                "Log_" +
                (i + 1).ToString("000");

            // Find Gatherable
            Gatherable gatherable =
                newLog.GetComponent<Gatherable>();

            if (gatherable == null)
            {
                gatherable =
                    newLog.AddComponent<Gatherable>();
            }

            // Assign ItemData
            gatherable.itemDataAsset =
                logItemData;

            // Assign unique world save ID
            gatherable.worldPickupID =
                worldID;

            // Store position so another log
            // does not spawn too close
            usedSpawnPositions.Add(
                spawnPosition
            );

            spawnedLogs.Add(
                newLog
            );

            successfullySpawned++;

            Debug.Log(
                "[LogSpawner] Spawned " +
                newLog.name +
                " at " +
                spawnPosition +
                " | ID: " +
                worldID
            );
        }

        Debug.Log(
            "[LogSpawner] Finished spawning. " +
            "Logs spawned: " +
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
            // Random X position
            float randomX =
                Random.Range(
                    terrainEdgeMargin,
                    terrainSize.x - terrainEdgeMargin
                );

            // Random Z position
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

            // Check distance from existing logs
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
                    minimumDistanceBetweenLogs)
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

    public void ClearSpawnedLogs()
    {
        foreach (GameObject log
                 in spawnedLogs)
        {
            if (log != null)
            {
                Destroy(log);
            }
        }

        spawnedLogs.Clear();
        usedSpawnPositions.Clear();
    }
}