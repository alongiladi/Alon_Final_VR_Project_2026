using UnityEngine;

/// <summary>
/// Adds realistic flickering light to dungeon torches and candles.
/// </summary>
public class TorchFlicker : MonoBehaviour
{
    public Light targetLight;
    public float minIntensity = 3.2f;
    public float maxIntensity = 4.6f;
    public float flickerSpeed = 3.0f;

    [Header("Solar Orb Pulsing")]
    public Transform sunOrbTransform;
    public float minScale = 0.95f;
    public float maxScale = 1.06f;

    private float baseIntensity;
    private float noiseOffset;
    private Vector3 initialOrbScale = Vector3.one;

    private void Awake()
    {
        if (targetLight == null)
            targetLight = GetComponent<Light>();

        if (targetLight != null)
            baseIntensity = targetLight.intensity;

        if (sunOrbTransform != null)
            initialOrbScale = sunOrbTransform.localScale;

        noiseOffset = Random.Range(0f, 100f);
    }

    private void Update()
    {
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, noiseOffset);

        if (targetLight != null)
        {
            targetLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
        }

        if (sunOrbTransform != null)
        {
            float scaleFactor = Mathf.Lerp(minScale, maxScale, noise);
            sunOrbTransform.localScale = initialOrbScale * scaleFactor;
        }
    }
}