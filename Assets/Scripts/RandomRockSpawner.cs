using UnityEngine;

public class RandomRockSpawner : MonoBehaviour
{
    public Terrain terrain;
    public GameObject rockPrefab; // Prefab with PickupItem script attached
    public int rockCount = 50;

    // Parent object to keep your Hierarchy clean
    public Transform container;

    private void Start()
    {
        if (terrain == null) terrain = Terrain.activeTerrain;
        SpawnRocks();
    }

    public void SpawnRocks()
    {
        Vector3 terrainSize = terrain.terrainData.size;
        Vector3 terrainPos = terrain.transform.position;

        for (int i = 0; i < rockCount; i++)
        {
            // Pick random X and Z within terrain bounds
            float randomX = Random.Range(terrainPos.x, terrainPos.x + terrainSize.x);
            float randomZ = Random.Range(terrainPos.z, terrainPos.z + terrainSize.z);

            // Get exact Y height of the terrain at those coordinates
            float terrainY = terrain.SampleHeight(new Vector3(randomX, 0, randomZ)) + terrainPos.y;

            Vector3 spawnPosition = new Vector3(randomX, terrainY, randomZ);
            Quaternion randomRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

            // Instantiate and set parent
            GameObject newRock = Instantiate(rockPrefab, spawnPosition, randomRotation);
            if (container != null)
            {
                newRock.transform.SetParent(container);
            }
        }
    }
}