using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Handles the victory sequence when the player escapes through the final door:
/// - Celebratory visual and particle fireworks
/// - Animated victory banner ("YOU ESCAPED!")
/// - "Start Over" (Restart) and "Exit Game" interactive buttons
/// - Works seamlessly with VR Ray Interactors and Laptop mouse clicks.
/// </summary>
public class VictoryManager : MonoBehaviour
{
    public static VictoryManager Instance { get; private set; }

    [Header("UI References")]
    public Canvas victoryCanvas;
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subtitleText;
    public Button restartButton;
    public Button exitButton;

    [Header("Celebration Effects")]
    public ParticleSystem celebrationParticles;
    public AudioSource audioSource;
    public AudioClip victorySound;
    public Light celebrationLight;

    private bool hasWon = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (victoryCanvas != null)
        {
            victoryCanvas.gameObject.SetActive(false);
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(ExitGame);
        }
    }

    /// <summary>
    /// Triggers the full victory sequence.
    /// </summary>
    public void TriggerVictory()
    {
        if (hasWon) return;
        hasWon = true;

        Debug.Log("[VictoryManager] Player escaped! Triggering celebration...");
        StartCoroutine(VictorySequenceRoutine());
    }

    private IEnumerator VictorySequenceRoutine()
    {
        // 1. Play celebration particles
        if (celebrationParticles != null)
        {
            celebrationParticles.gameObject.SetActive(true);
            celebrationParticles.Play();
        }

        // 2. Play victory fanfare / sound
        if (audioSource != null && victorySound != null)
        {
            audioSource.PlayOneShot(victorySound);
        }

        // 3. Turn on celebratory ambient light
        if (celebrationLight != null)
        {
            celebrationLight.enabled = true;
        }

        // 4. Show & Animate Victory UI Canvas as full screen overlay
        if (victoryCanvas != null)
        {
            victoryCanvas.gameObject.SetActive(true);

            // If running on flat screen / Laptop simulator: switch to ScreenSpaceOverlay for crisp full-screen centered overlay!
            bool isVrHeadset = UnityEngine.XR.XRSettings.isDeviceActive;

            if (!isVrHeadset)
            {
                victoryCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var rect = victoryCanvas.GetComponent<RectTransform>();
                rect.localScale = Vector3.one;
            }
            else
            {
                // In VR Headset: Keep WorldSpace and anchor 1.8m directly in front of camera
                victoryCanvas.renderMode = RenderMode.WorldSpace;
                Camera cam = Camera.main;
                if (cam != null)
                {
                    Vector3 forwardPos = cam.transform.position + (cam.transform.forward * 1.8f);
                    victoryCanvas.transform.position = forwardPos;
                    victoryCanvas.transform.rotation = Quaternion.LookRotation(forwardPos - cam.transform.position);
                    victoryCanvas.transform.localScale = Vector3.one * 0.0035f;
                }
            }

            // Unlock mouse cursor for laptop simulator
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Smooth fade-in animation
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                float elapsed = 0f;
                float duration = 0.6f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                    yield return null;
                }

                canvasGroup.alpha = 1f;
            }
        }
    }

    /// <summary>
    /// Restarts the dungeon game.
    /// </summary>
    public void RestartGame()
    {
        Debug.Log("[VictoryManager] Restarting game...");
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }

    /// <summary>
    /// Exits the game or stops play mode in editor.
    /// </summary>
    public void ExitGame()
    {
        Debug.Log("[VictoryManager] Exiting game...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}