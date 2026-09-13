using UnityEngine;

/// <summary>
/// Represents a single cell in the 3D maze grid.
/// Keeps track of its grid coordinates, visited state, relative path distance,
/// and whether each of its four walls is currently active.
/// </summary>
[System.Serializable]
public class MazeCell
{
    // Grid coordinates
    public int x;
    public int z;

    // BFS/DFS tracking states
    public bool isVisited = false;
    public int distance = 0; // Distance from the start cell (0, 0)

    // Wall states (true = wall is intact, false = wall is carved/removed)
    public bool northWall = true; // +Z direction
    public bool southWall = true; // -Z direction
    public bool eastWall = true;  // +X direction
    public bool westWall = true;  // -X direction

    /// <summary>
    /// Constructor to initialize a grid cell.
    /// </summary>
    public MazeCell(int x, int z)
    {
        this.x = x;
        this.z = z;
    }
}
