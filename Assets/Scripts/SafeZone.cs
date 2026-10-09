using UnityEngine;

public class SafeZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Example safe zone logic
            // enemyAI.canAttack = false;
            // playerHealth.isInvulnerable = true;
            Debug.Log("Player entered the safe zone.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Re-enable danger
            // enemyAI.canAttack = true;
            // playerHealth.isInvulnerable = false;
            Debug.Log("Player left the safe zone.");
        }
    }
}