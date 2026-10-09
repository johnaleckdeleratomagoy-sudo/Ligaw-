using UnityEngine;

public static class MeatDrop
{
    public static void Spawn(GameObject meatPrefab, int count, Vector3 origin)
    {
        if (meatPrefab == null) return;

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPosition = origin + new Vector3(
                Random.Range(-0.5f, 0.5f),
                0.5f,
                Random.Range(-0.5f, 0.5f)
            );

            GameObject meat = Object.Instantiate(meatPrefab, spawnPosition, Quaternion.identity);

            // A non-convex MeshCollider can't be used with a Rigidbody.
            MeshCollider meshCol = meat.GetComponent<MeshCollider>();
            if (meshCol != null) meshCol.convex = true;

            // Some meat prefabs have no Rigidbody and would hang in the air.
            Rigidbody rb = meat.GetComponent<Rigidbody>();
            if (rb == null) rb = meat.AddComponent<Rigidbody>();

            rb.AddForce(
                new Vector3(Random.Range(-1f, 1f), 2f, Random.Range(-1f, 1f)),
                ForceMode.Impulse
            );
        }
    }
}
