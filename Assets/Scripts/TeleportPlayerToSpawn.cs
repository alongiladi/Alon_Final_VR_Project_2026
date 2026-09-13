using UnityEngine;

/// <summary>
/// Simple helper script that teleports the VR Player Rig (XR Origin) to the
/// spawn point's position as soon as the maze generates.
/// </summary>
public class TeleportPlayerToSpawn : MonoBehaviour
{
    private void Start()
    {
        // Attempt to find any GameObject with "XR" or "Player" in its name or tags
        GameObject playerRig = GameObject.FindWithTag("Player");

        if (playerRig == null)
        {
            // Fallback: Search for XR Origin by name or components
            var origins = GameObject.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var mono in origins)
            {
                if (mono.GetType().Name.Contains("XROrigin") || mono.GetType().Name.Contains("XRRig"))
                {
                    playerRig = mono.gameObject;
                    break;
                }
            }
        }

        if (playerRig != null)
        {
            // Teleport player rig, maintaining their height setup
            playerRig.transform.position = transform.position;
            playerRig.transform.rotation = transform.rotation;

            var sim = GameObject.FindAnyObjectByType<DungeonLaptopSimulator>();
            if (sim != null)
            {
                sim.SyncRotation(transform.eulerAngles.y);
            }

            Debug.Log($"[TeleportPlayerToSpawn] Teleported Player to Spawn Point at {transform.position} facing {transform.eulerAngles.y} degrees");
        }
        else
        {
            Debug.LogWarning("[TeleportPlayerToSpawn] Could not find XR Origin / Player in scene. Make sure your XR Origin is in the scene.");
        }
    }
}
