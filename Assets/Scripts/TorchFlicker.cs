using UnityEngine;

/// <summary>
/// Adds realistic flickering light to dungeon torches and candles.
/// </summary>
public class TorchFlicker : MonoBehaviour
{
    public Light targetLight;
    public float minIntensity = 1.8f;
    public float maxIntensity = 3.0f;
    public float flickerSpeed = 8f;
    
    private float baseIntensity;
    private float noiseOffset;

    private void Awake()
    {
        if (targetLight == null)
            targetLight = GetComponent<Light>();

        if (targetLight != null)
            baseIntensity = targetLight.intensity;

        noiseOffset = Random.Range(0f, 100f);
    }

    private void Update()
    {
        if (targetLight == null) return;

        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, noiseOffset);
        targetLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
    }
}