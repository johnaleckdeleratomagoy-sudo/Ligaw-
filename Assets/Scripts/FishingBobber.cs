using UnityEngine;
public class FishingBobber : MonoBehaviour 
{ [Header("Floating")][SerializeField] 
    private float bobHeight = 0.05f; [SerializeField]
    private float bobSpeed = 2f; [Header("Bite")][SerializeField] 
    private float biteDepth = 0.35f; [SerializeField]
    private float biteSpeed = 8f;
    private FishingManager fishingManager; 
    private Vector3 startPosition; 
    private bool isBiting; 
    public void Initialize(FishingManager manager, Vector3 position) { fishingManager = manager; transform.position = position; startPosition = position; }
    private void Update() { if (!isBiting) 
        { float y = Mathf.Sin(Time.time * bobSpeed) * bobHeight; transform.position = new Vector3(startPosition.x, startPosition.y + y, startPosition.z); } 
        else { float targetY = startPosition.y - biteDepth; Vector3 targetPosition = new Vector3(startPosition.x, targetY, startPosition.z); transform.position = Vector3.Lerp(transform.position, targetPosition, biteSpeed * Time.deltaTime); } } 
    public void SetBiting(bool biting) { isBiting = biting; } }