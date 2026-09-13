using UnityEngine;

/// <summary>
/// Attached to the trigger volume behind the exit door.
/// Detects when the player walks through the open door and fires the victory celebration.
/// </summary>
public class WinTrigger : MonoBehaviour
{
    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        // Check if player or camera rig entered the doorway threshold
        if (other.CompareTag("Player") || other.name.Contains("XR") || other.name.Contains("Camera") || other.GetComponent<CharacterController>() != null)
        {
            hasTriggered = true;
            Debug.Log("[WinTrigger] Player stepped through the exit door!");

            if (VictoryManager.Instance != null)
            {
                VictoryManager.Instance.TriggerVictory();
            }
        }
    }
}