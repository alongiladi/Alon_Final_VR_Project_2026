using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Serializable data container for external JSON configuration.
/// Can be customized externally via StreamingAssets/dungeon_config.json without recompilation.
/// </summary>
[System.Serializable]
public class DungeonConfigData
{
    public string dungeonName = "Obsidian Fractal Labyrinth";
    public string version = "1.0.0";
    public int gridWidth = 4;
    public int gridDepth = 4;
    public float cellSize = 5.86f;
    public float ceilingHeight = 5.8f;
    public bool enableCeilings = true;
    public float playerMoveSpeed = 3.5f;
    
    public FractalTreeConfig fractalTree = new FractalTreeConfig();
    public LightingConfig lighting = new LightingConfig();
}

[System.Serializable]
public class FractalTreeConfig
{
    public int recursionDepth = 4;
    public float branchLength = 1.6f;
    public float branchLengthFactor = 0.72f;
    public float branchRadius = 0.12f;
    public float splitAngle = 32.0f;
    public bool leavesEnabled = true;
    public float growthSpeed = 2.5f;
}

[System.Serializable]
public class LightingConfig
{
    public float torchFlickerSpeed = 8.0f;
    public float minTorchIntensity = 1.8f;
    public float maxTorchIntensity = 3.2f;
}

/// <summary>
/// Loads and manages external JSON configuration from Application.streamingAssetsPath.
/// </summary>
public static class DungeonConfigLoader
{
    private const string ConfigFileName = "dungeon_config.json";

    public static DungeonConfigData LoadConfig()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, ConfigFileName);
        
        if (File.Exists(filePath))
        {
            try
            {
                string jsonContent = File.ReadAllText(filePath);
                DungeonConfigData config = JsonUtility.FromJson<DungeonConfigData>(jsonContent);
                if (config != null)
                {
                    Debug.Log($"[DungeonConfigLoader] Successfully loaded JSON configuration from: {filePath}");
                    return config;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DungeonConfigLoader] Error parsing JSON config at {filePath}: {ex.Message}. Using default values.");
            }
        }
        else
        {
            Debug.LogWarning($"[DungeonConfigLoader] JSON config not found at {filePath}. Creating and using default config.");
            DungeonConfigData defaultConfig = new DungeonConfigData();
            SaveConfig(defaultConfig);
            return defaultConfig;
        }

        return new DungeonConfigData();
    }

    public static void SaveConfig(DungeonConfigData config)
    {
        try
        {
            string dir = Application.streamingAssetsPath;
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string filePath = Path.Combine(dir, ConfigFileName);
            string jsonContent = JsonUtility.ToJson(config, true);
            File.WriteAllText(filePath, jsonContent);
            Debug.Log($"[DungeonConfigLoader] Saved JSON configuration to: {filePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DungeonConfigLoader] Failed to save JSON config: {ex.Message}");
        }
    }
}
