using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AnimalSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnableAnimal
    {
        public string animalName = "Wild Boar";
        public GameObject animalPrefab;
        [Range(1, 500)] public int spawnCount = 10;
        public float yOffset = 0f;
    }

    [Header("Animals to Spawn")]
    public List<SpawnableAnimal> animalsToSpawn = new List<SpawnableAnimal>();

    [Header("Placement Rules")]
    public Terrain targetTerrain;
    public Transform spawnContainer;
    [Range(0f, 90f)] public float maxSlopeAngle = 45f;
    public float minSpawnRadius = 0f; // Set to 0 to prevent overlap failures
    public bool useNavMeshSnap = false; // Turn OFF if you haven't baked NavMesh yet
    public int maxAttemptsPerAnimal = 30;

    private List<GameObject> spawnedInstances = new List<GameObject>();

    private void Start()
    {
        if (targetTerrain == null) targetTerrain = Terrain.activeTerrain;
        SpawnAll();
    }

    public void SpawnAll()
    {
        if (targetTerrain == null)
        {
            Debug.LogError("[AnimalSpawner] No Active Terrain found in scene!");
            return;
        }

        Vector3 terrainPos = targetTerrain.transform.position;
        Vector3 terrainSize = targetTerrain.terrainData.size;

        foreach (var animal in animalsToSpawn)
        {
            if (animal.animalPrefab == null)
            {
                Debug.LogWarning($"[AnimalSpawner] No Prefab assigned for '{animal.animalName}'!");
                continue;
            }

            int count = 0;
            for (int i = 0; i < animal.spawnCount; i++)
            {
                for (int attempt = 0; attempt < maxAttemptsPerAnimal; attempt++)
                {
                    float rx = Random.Range(terrainPos.x, terrainPos.x + terrainSize.x);
                    float rz = Random.Range(terrainPos.z, terrainPos.z + terrainSize.z);

                    // Slope Angle Check
                    float normX = (rx - terrainPos.x) / terrainSize.x;
                    float normZ = (rz - terrainPos.z) / terrainSize.z;
                    Vector3 normal = targetTerrain.terrainData.GetInterpolatedNormal(normX, normZ);

                    if (Vector3.Angle(normal, Vector3.up) > maxSlopeAngle)
                        continue;

                    // Calculate Height
                    float ry = targetTerrain.SampleHeight(new Vector3(rx, 0, rz)) + terrainPos.y;
                    Vector3 candidatePos = new Vector3(rx, ry + animal.yOffset, rz);

                    // NavMesh Check (if enabled)
                    if (useNavMeshSnap)
                    {
                        NavMeshHit navHit;
                        if (NavMesh.SamplePosition(candidatePos, out navHit, 5.0f, NavMesh.AllAreas))
                        {
                            candidatePos = navHit.position;
                        }
                        else
                        {
                            continue; // Invalid NavMesh spot
                        }
                    }

                    // Radius / Overlap Check
                    if (minSpawnRadius > 0f)
                    {
                        if (Physics.OverlapSphere(candidatePos, minSpawnRadius).Length > 0)
                            continue;
                    }

                    // Instantiate Animal
                    Quaternion rot = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                    Transform parent = spawnContainer != null ? spawnContainer : transform;

                    GameObject obj = Instantiate(animal.animalPrefab, candidatePos, rot, parent);
                    spawnedInstances.Add(obj);
                    count++;
                    break;
                }
            }

            Debug.Log($"[AnimalSpawner] Successfully spawned {count}/{animal.spawnCount} '{animal.animalName}'(s).");
        }
    }
}