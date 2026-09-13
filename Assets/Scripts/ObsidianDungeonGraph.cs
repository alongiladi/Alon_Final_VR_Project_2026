using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; // Demonstrating LINQ support
using UnityEngine;

/// <summary>
/// Node representation in the Obsidian-designed dungeon logic graph.
/// </summary>
[System.Serializable]
public class DungeonGraphNode
{
    public string id;
    public string nodeType; // e.g. Spawn, KeyAltar, FractalGarden, ExitGate
    public int gridX;
    public int gridZ;
    public int difficulty;
    public string extraFeature;
    public List<string> connectedNodes = new List<string>();

    public DungeonGraphNode(string id)
    {
        this.id = id;
    }
}

/// <summary>
/// Parses dungeon logic graphs designed in Obsidian.md markdown files
/// and applies procedural generation using graph traversal and LINQ queries.
/// </summary>
public class ObsidianDungeonGraph
{
    public Dictionary<string, DungeonGraphNode> nodes = new Dictionary<string, DungeonGraphNode>();

    /// <summary>
    /// Loads and parses an Obsidian markdown graph file.
    /// </summary>
    public bool LoadFromMarkdown(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"[ObsidianDungeonGraph] Graph markdown file not found: {filePath}");
            return false;
        }

        nodes.Clear();
        string[] lines = File.ReadAllLines(filePath);

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

            // Parse connections: - [[NodeA]] -> [[NodeB]]
            if (line.Contains("->"))
            {
                ParseEdgeLine(line);
            }
            // Parse node attributes: - [[NodeA]]: Type=Spawn, GridX=0, GridZ=0, Difficulty=1
            else if (line.Contains(":") && line.Contains("[["))
            {
                ParseNodeAttributes(line);
            }
        }

        Debug.Log($"[ObsidianDungeonGraph] Successfully loaded graph with {nodes.Count} nodes from {Path.GetFileName(filePath)}");
        return true;
    }

    private void ParseEdgeLine(string line)
    {
        string[] parts = line.Replace("-", "").Split(new[] { "->" }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2)
        {
            string fromId = ExtractNodeId(parts[0]);
            string toId = ExtractNodeId(parts[1]);

            var fromNode = GetOrCreateNode(fromId);
            var toNode = GetOrCreateNode(toId);

            if (!fromNode.connectedNodes.Contains(toId))
            {
                fromNode.connectedNodes.Add(toId);
            }
        }
    }

    private void ParseNodeAttributes(string line)
    {
        int colonIdx = line.IndexOf(':');
        if (colonIdx < 0) return;

        string nodePart = line.Substring(0, colonIdx);
        string attrPart = line.Substring(colonIdx + 1);

        string nodeId = ExtractNodeId(nodePart);
        var node = GetOrCreateNode(nodeId);

        string[] keyValues = attrPart.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var kv in keyValues)
        {
            string[] pair = kv.Split('=');
            if (pair.Length == 2)
            {
                string key = pair[0].Trim();
                string val = pair[1].Trim();

                if (key.Equals("Type", StringComparison.OrdinalIgnoreCase)) node.nodeType = val;
                else if (key.Equals("GridX", StringComparison.OrdinalIgnoreCase)) int.TryParse(val, out node.gridX);
                else if (key.Equals("GridZ", StringComparison.OrdinalIgnoreCase)) int.TryParse(val, out node.gridZ);
                else if (key.Equals("Difficulty", StringComparison.OrdinalIgnoreCase)) int.TryParse(val, out node.difficulty);
                else if (key.Equals("Feature", StringComparison.OrdinalIgnoreCase) || key.Equals("Puzzle", StringComparison.OrdinalIgnoreCase)) node.extraFeature = val;
            }
        }
    }

    private string ExtractNodeId(string raw)
    {
        string clean = raw.Trim().Replace("[[", "").Replace("]]", "").Trim();
        return clean;
    }

    public DungeonGraphNode GetOrCreateNode(string id)
    {
        if (!nodes.ContainsKey(id))
        {
            nodes[id] = new DungeonGraphNode(id);
        }
        return nodes[id];
    }

    /// <summary>
    /// Demonstrates LINQ .Max() usage to find the maximum grid span and difficulty in the dungeon graph.
    /// </summary>
    public int GetMaxDifficulty()
    {
        if (nodes.Count == 0) return 0;
        return nodes.Values.Select(n => n.difficulty).Max();
    }

    public int GetMaxGridX()
    {
        if (nodes.Count == 0) return 0;
        return nodes.Values.Select(n => n.gridX).Max();
    }

    public int GetMaxGridZ()
    {
        if (nodes.Count == 0) return 0;
        return nodes.Values.Select(n => n.gridZ).Max();
    }

    public float GetMaxTotalSpan()
    {
        if (nodes.Count == 0) return 0f;
        // LINQ .Max() over calculated metric
        return nodes.Values.Max(n => Mathf.Sqrt(n.gridX * n.gridX + n.gridZ * n.gridZ));
    }
}
