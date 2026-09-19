using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; // Demonstrating LINQ support
using UnityEngine;


/// Node representation in the Obsidian dungeon logic graph.

[System.Serializable]
public class DungeonGraphNode
{
    public string id;
    public string nodeType; // spawn, keyAltar, fractalGarden, exitGate
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


//creating a dictionary for all nodes in graph

public class ObsidianDungeonGraph
{
    public Dictionary<string, DungeonGraphNode> nodes = new Dictionary<string, DungeonGraphNode>();

  
    ///func to loads+  parse  Obsidian.md (if exists),also clean up file string content
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

    //recognizes all symbols (arrows for neighbors, brackets for node ids, colons for attributes etc) 

            if (line.Contains("->"))
            {
                ParseEdgeLine(line);
            }
         
            else if (line.Contains(":") && line.Contains("[["))
            {
                ParseNodeAttributes(line);
            }
        }

        Debug.Log($"[ObsidianDungeonGraph] Successfully loaded graph with {nodes.Count} nodes from {Path.GetFileName(filePath)}");
        return true;
    }


//identifying connections between nodes + creating them in graph dictionary
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
//parsing all attributes for each node
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

//in case we come across a node id that we don't have yet in the dict, we create a new node with that id s the code won't crash
    public DungeonGraphNode GetOrCreateNode(string id)
    {
        if (!nodes.ContainsKey(id))
        {
            nodes[id] = new DungeonGraphNode(id);
        }
        return nodes[id];
    }

 
    /// LINQ queries -max difficult, max gridX, max gridZ, max straight line distance in the maze etc
    
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
