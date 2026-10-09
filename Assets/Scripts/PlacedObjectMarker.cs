using System.Collections.Generic;
using UnityEngine;

// Added to every object the player places in the world so the
// save system can find it again and respawn it after loading.
public class PlacedObjectMarker : MonoBehaviour
{
    public static readonly List<PlacedObjectMarker> All =
        new List<PlacedObjectMarker>();

    public int itemID;

    private void OnEnable()
    {
        if (!All.Contains(this))
            All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }
}
