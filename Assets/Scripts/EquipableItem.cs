using UnityEngine;

public class EquipableItem : MonoBehaviour
{
    [Header("Hand Alignment Offsets")]
    [Tooltip("Local position offset relative to hand HoldPoint")]
    public Vector3 positionOffset = Vector3.zero;

    [Tooltip("Local rotation offset relative to hand HoldPoint")]
    public Vector3 rotationOffset = Vector3.zero;

    [Tooltip("Local scale override (default 1,1,1)")]
    public Vector3 scaleOffset = Vector3.one;
}