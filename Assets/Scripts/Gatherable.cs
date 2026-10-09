using UnityEngine;
using System.Collections;

public class Gatherable : MonoBehaviour
{
    [Header("Item Configuration Link")]
    public ItemDataSO itemDataAsset;

    [Header("Gather Settings")]
    public int gatherAmount = 1;
    public float destroyDelay = 0.5f;

    [Header("World Pickup Save")]
    [Tooltip("Must be unique for saved world objects.")]
    public string worldPickupID;

    private bool isGathered = false;

    // =========================================================
    // UNITY
    // =========================================================

    private void Start()
    {
        StartCoroutine(InitializeWorldPickupSave());
    }

    private IEnumerator InitializeWorldPickupSave()
    {
        // Give the spawner time to assign the World Pickup ID.
        yield return null;

        CheckIfAlreadyCollected();
    }

    // =========================================================
    // CHECK WORLD PICKUP SAVE
    // =========================================================

    private void CheckIfAlreadyCollected()
    {
        // No ID means this object is not a persistent world pickup.
        if (string.IsNullOrEmpty(worldPickupID))
        {
            return;
        }

        if (WorldPickupSave.Instance == null)
        {
            Debug.LogWarning(
                "[Gatherable] WorldPickupSave not found for: " +
                gameObject.name
            );

            return;
        }

        // If this world object was already collected,
        // remove it when the scene loads again.
        if (WorldPickupSave.Instance.IsCollected(worldPickupID))
        {
            isGathered = true;

            Debug.Log(
                "[Gatherable] Removing previously collected object: " +
                worldPickupID
            );

            Destroy(gameObject);
        }
    }

    // =========================================================
    // GATHER
    // =========================================================

    public void Gather()
    {
        if (isGathered)
        {
            return;
        }

        if (itemDataAsset == null)
        {
            Debug.LogError(
                gameObject.name +
                " is missing its ItemDataAsset assignment!"
            );

            return;
        }

        PlayerInventory playerInv =
            FindFirstObjectByType<PlayerInventory>();

        // =====================================================
        // PLAYER INVENTORY
        // =====================================================

        if (playerInv != null)
        {
            bool successfullyPickedUp =
                playerInv.DirectPickUp(
                    gameObject,
                    itemDataAsset,
                    gatherAmount
                );

            if (successfullyPickedUp)
            {
                // Mark this specific world object as collected.
                MarkAsCollected();

                // Count this specific world object toward Day 1.
                NotifyTaskSystem();

                DisableColliders();

                if (destroyDelay > 0f)
                {
                    StartCoroutine(
                        DestroyAfterDelay()
                    );
                }
                else
                {
                    Destroy(gameObject);
                }
            }
            else
            {
                Debug.LogWarning(
                    "[Gatherable] Could not pick up " +
                    itemDataAsset.itemName +
                    ". Inventory/Hotbar may be full."
                );
            }

            return;
        }

        // =====================================================
        // FALLBACK HOTBAR
        // =====================================================

        HotbarManager hotbar =
            FindFirstObjectByType<HotbarManager>();

        if (hotbar != null)
        {
            bool successfullyAdded =
                hotbar.TryAddToHotbar(
                    itemDataAsset,
                    gatherAmount
                );

            if (successfullyAdded)
            {
                // Mark this specific world object as collected.
                MarkAsCollected();

                // Count this specific world object toward Day 1.
                NotifyTaskSystem();

                DisableColliders();

                if (destroyDelay > 0f)
                {
                    StartCoroutine(
                        DestroyAfterDelay()
                    );
                }
                else
                {
                    Destroy(gameObject);
                }
            }
            else
            {
                Debug.LogWarning(
                    "[Gatherable] Inventory/Hotbar slots are full!"
                );
            }
        }
        else
        {
            Debug.LogWarning(
                "[Gatherable] No PlayerInventory or HotbarManager found."
            );
        }
    }

    // =========================================================
    // MARK AS COLLECTED
    // =========================================================

    public void MarkAsCollected()
    {
        if (isGathered)
        {
            return;
        }

        isGathered = true;

        if (WorldPickupSave.Instance != null)
        {
            if (!string.IsNullOrEmpty(worldPickupID))
            {
                WorldPickupSave.Instance.MarkCollected(
                    worldPickupID
                );
            }
        }
    }

    // =========================================================
    // DAY 1 TASK SYSTEM
    // =========================================================

    private void NotifyTaskSystem()
    {
        if (Day1TaskManager.Instance == null)
        {
            return;
        }

        if (itemDataAsset == null)
        {
            return;
        }

        // -----------------------------------------------------
        // IMPORTANT:
        //
        // If this is a persistent world object with a unique ID,
        // tell Day1TaskManager exactly which object was gathered.
        //
        // This prevents:
        //
        // Pick up Log
        // Drop Log
        // Pick up Log again
        //
        // from counting as two logs.
        // -----------------------------------------------------

        if (!string.IsNullOrEmpty(worldPickupID))
        {
            Day1TaskManager.Instance.OnWorldItemGathered(
                worldPickupID,
                itemDataAsset.itemName,
                gatherAmount
            );
        }
        else
        {
            // -------------------------------------------------
            // Objects without a World Pickup ID are not tracked
            // as persistent world objects.
            //
            // We still allow the task system to count them using
            // the old method.
            // -------------------------------------------------

            Day1TaskManager.Instance.OnItemGathered(
                itemDataAsset.itemName,
                gatherAmount
            );
        }
    }

    // =========================================================
    // DISABLE COLLIDERS
    // =========================================================

    private void DisableColliders()
    {
        Collider[] colliders =
            GetComponentsInChildren<Collider>();

        foreach (Collider col in colliders)
        {
            if (col != null)
            {
                col.enabled = false;
            }
        }
    }

    // =========================================================
    // DESTROY
    // =========================================================

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(
            destroyDelay
        );

        Destroy(gameObject);
    }
}