using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Handles the VR key-socket interaction.
/// Detects when a GameObject with the correct key tag enters the trigger area,
/// disables the key, snaps it visually to the socket, plays audio feedback,
/// and opens the door to allow escape.
/// </summary>
public class DoorKeySocket : MonoBehaviour
{
    [Header("Socket Configuration")]
    [Tooltip("The tag required on the colliding object to be recognized as the Key")]
    public string keyTag = "Key";

    [Tooltip("The transform representing the exact slot where the key should snap into")]
    public Transform snapAnchor;

    [Header("Door Configuration")]
    [Tooltip("The door GameObject (e.g. door panel, metal gate) that should be opened.")]
    public GameObject doorObject;

    [Tooltip("Whether the door should smoothly swing open instead of immediately disappearing.")]
    public bool animateDoorOpen = true;

    [Tooltip("Target rotation angle for swinging open (degrees around Y axis).")]
    public float openAngle = -95f;

    [Tooltip("Speed of door opening animation.")]
    public float openSpeed = 2.0f;

    [Header("Feedback Effects")]
    [Tooltip("Audio clip to play when the door is successfully unlocked")]
    public AudioClip unlockSound;
    
    [Tooltip("Audio source to play the unlock sound")]
    public AudioSource audioSource;

    [Tooltip("Optional particle system played when unlocked (e.g. magic sparkles or dust).")]
    public ParticleSystem unlockParticles;

    [Header("Events")]
    [Tooltip("Unity Event triggered when the door is successfully unlocked.")]
    public UnityEvent OnDoorUnlocked;

    private bool isUnlocked = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isUnlocked) return;

        // Check if the colliding object has the correct tag
        if (other.CompareTag(keyTag))
        {
            GameObject keyInstance = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;
            UnlockDoor(keyInstance);
        }
    }

    /// <summary>
    /// Snaps the key into place, plays audio feedback, animates door opening, and fires custom events.
    /// </summary>
    public void UnlockDoor(GameObject keyInstance)
    {
        if (isUnlocked) return;
        isUnlocked = true;

        if (keyInstance != null)
        {
            // 1. Disable Physics on the key so it doesn't fall or bounce out
            Rigidbody rb = keyInstance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // 2. Disable XR Interaction so the player is forced to release it
            var grabInteractable = keyInstance.GetComponent("XRGrabInteractable");
            if (grabInteractable != null)
            {
                (grabInteractable as MonoBehaviour).enabled = false;
            }

            // 3. Physically snap the key to the socket's visual snap anchor
            if (snapAnchor != null)
            {
                keyInstance.transform.SetParent(snapAnchor);
                keyInstance.transform.localPosition = Vector3.zero;
                keyInstance.transform.localRotation = Quaternion.identity;
            }
        }

        // 4. Play audio feedback
        if (audioSource != null && unlockSound != null)
        {
            audioSource.PlayOneShot(unlockSound);
        }

        // Play particle effect
        if (unlockParticles != null)
        {
            unlockParticles.Play();
        }

        // 5. Open the door
        if (doorObject != null)
        {
            if (animateDoorOpen)
            {
                StartCoroutine(AnimateDoorOpening());
            }
            else
            {
                doorObject.SetActive(false);
            }
            Debug.Log("[DoorKeySocket] Maze Door Unlocked and Opened!");
        }

        // 6. Invoke customizable Unity events
        OnDoorUnlocked?.Invoke();
    }

    private IEnumerator AnimateDoorOpening()
    {
        if (doorObject == null) yield break;

        Quaternion initialRot = doorObject.transform.localRotation;
        Quaternion targetRot = initialRot * Quaternion.Euler(0, openAngle, 0);

        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * openSpeed;
            doorObject.transform.localRotation = Quaternion.Slerp(initialRot, targetRot, Mathf.SmoothStep(0, 1, elapsed));
            yield return null;
        }

        doorObject.transform.localRotation = targetRot;
    }
}
