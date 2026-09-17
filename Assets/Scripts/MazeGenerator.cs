using System.Collections.Generic;
using System.IO;
using System.Linq; // LINQ support with .Max()
using UnityEngine;

///  procedural enerating  dungeon  sequence based on
/// - JSON configuration (dungeon_config.json)
/// - Obsidian.md Logic graph (dungeon_logic.md)
/// - interactive fractal trees in sanctuary chambers
/// - LINQ Queries (.Max(), etc.)
///
public class MazeGenerator : MonoBehaviour
{
    [Header("Maze Dimensions")]
    [Tooltip("Number of grid columns")]
    public int width = 3;
    [Tooltip("Number of grid rows")]
    public int depth = 3;
    [Tooltip("Size of each grid cell in Unity world units")]
    public float cellSize = 5.86f;

    [Header("Modular Dungeon Architecture")]
    public GameObject[] wallPrefabs;
    public GameObject[] floorPrefabs;
    public GameObject ceilingPrefab;
    public bool spawnCeilings = true;
    public GameObject[] pillarPrefabs;

    [Header("Lighting & Atmosphere")]
    public GameObject wallTorchPrefab;

    [Header("Decorative Props & Decals")]
    public GameObject[] deadEndProps;
    public GameObject[] floorDecals;

    [Header("Gameplay Elements")]
    public GameObject playerSpawnPrefab;
    public GameObject starterTorchPrefab;
    public GameObject starterStandPrefab;
    public GameObject keyPrefab;
    public GameObject keyTablePrefab;
    public GameObject doorPrefab;

    [Header("Fractal Tree Integration")]
    public GameObject fractalTreePrefab;
    public bool spawnFractalTree = true;

    [Header("Spawn Heights / Offsets")]
    public float wallHeightOffset = 0f;
    public float ceilingHeight = 5.8f;
    public float keyHeightOffset = 1.48f;

    // External Config & Obsidian Graph
    public DungeonConfigData loadedConfig { get; private set; }
    public ObsidianDungeonGraph logicGraph { get; private set; }

    // 2D grid array representing the maze structure
    private MazeCell[,] grid;

    private void Awake()
    {
        LoadExternalConfigAndGraph();
    }

    private void Start()
    {
        GenerateMaze();
    }

    
    /// Loads external JSON config and obsdian graphfile.
    /// LINQ .Max() for compute layout parameters dynamically.
    
    public void LoadExternalConfigAndGraph()
    {
        //  Load JSON config
        loadedConfig = DungeonConfigLoader.LoadConfig();
        if (loadedConfig != null)
        {
            cellSize = loadedConfig.cellSize;
            ceilingHeight = loadedConfig.ceilingHeight;
            spawnCeilings = loadedConfig.enableCeilings;
        }

        // Load obsdian graph
        logicGraph = new ObsidianDungeonGraph();
        string graphPath = Path.Combine(Application.streamingAssetsPath, "dungeon_logic.md");
        if (File.Exists(graphPath))
        {
            logicGraph.LoadFromMarkdown(graphPath);

            // 3. Use LINQ .Max() to determine dimensions based on Graph data
            if (logicGraph.nodes.Count > 0)
            {
                int maxObsidianX = logicGraph.GetMaxGridX();
                int maxObsidianZ = logicGraph.GetMaxGridZ();
                int maxDifficulty = logicGraph.GetMaxDifficulty();
                float maxSpan = logicGraph.GetMaxTotalSpan();

                width = Mathf.Max(width, maxObsidianX + 1);
                depth = Mathf.Max(depth, maxObsidianZ + 1);

                Debug.Log($"[MazeGenerator] [LINQ .Max()] configured dungeon bounds from graph: MaxX={maxObsidianX}, MaxZ={maxObsidianZ}, MaxDifficulty={maxDifficulty}, MaxSpan={maxSpan:F2}");
            }
        }
    }

    /// executes full procedural maze layout.

    public void GenerateMaze()
    {
        InitializeGrid();
        CarveLinearCompactPath();
        InstantiateDungeonMaze();
    }

    private void InitializeGrid()
    {
        grid = new MazeCell[width, depth];
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                grid[x, z] = new MazeCell(x, z);
            }
        }
    }

    /// <summary>
    /// Carves a clean path connecting the rooms defined in the Obsidian logic graph:
    /// Start Cell (0,0) -> Key Chamber (0,1) -> Fractal Sanctuary (1,1) -> Exit Sanctuary (2,1)
    /// </summary>
    private void CarveLinearCompactPath()
    {
        // Start: Cell (0,0) - Entrance
        grid[0, 0].isVisited = true;
        grid[0, 0].distance = 0;

        // Key Chamber: Cell (0,1)
        grid[0, 1].isVisited = true;
        grid[0, 1].distance = 1;
        RemoveWallsBetween(grid[0, 0], grid[0, 1]);

        // Fractal Sanctuary: Cell (1,1)
        grid[1, 1].isVisited = true;
        grid[1, 1].distance = 2;
        RemoveWallsBetween(grid[0, 1], grid[1, 1]);

        // Exit Sanctuary: Cell (2,1)
        grid[2, 1].isVisited = true;
        grid[2, 1].distance = 3;
        RemoveWallsBetween(grid[1, 1], grid[2, 1]);
    }

    private void RemoveWallsBetween(MazeCell a, MazeCell b)
    {
        int dx = b.x - a.x;
        int dz = b.z - a.z;

        if (dx == 1) { a.eastWall = false; b.westWall = false; }
        else if (dx == -1) { a.westWall = false; b.eastWall = false; }
        else if (dz == 1) { a.northWall = false; b.southWall = false; }
        else if (dz == -1) { a.southWall = false; b.northWall = false; }
    }

    private void InstantiateDungeonMaze()
    {
        Transform mazeParent = this.transform;

        Transform floorContainer = new GameObject("Floors").transform;
        floorContainer.SetParent(mazeParent);

        Transform wallContainer = new GameObject("Walls").transform;
        wallContainer.SetParent(mazeParent);

        Transform ceilingContainer = new GameObject("Ceilings").transform;
        ceilingContainer.SetParent(mazeParent);

        Transform pillarContainer = new GameObject("Pillars").transform;
        pillarContainer.SetParent(mazeParent);

        Transform torchContainer = new GameObject("Torches").transform;
        torchContainer.SetParent(mazeParent);

        Transform propsContainer = new GameObject("Props").transform;
        propsContainer.SetParent(mazeParent);

        MazeCell startCell = grid[0, 0];
        MazeCell keyCell = grid[0, 1];        // Room 2: Key Chamber
        MazeCell fractalCell = grid[1, 1];    // Room 3: Fractal Sanctuary
        MazeCell exitCell = grid[2, 1];       // Room 4: Exit Sanctuary

        // 1. Spawn Floors, Ceilings, and Walls
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                MazeCell cell = grid[x, z];
                if (!cell.isVisited) continue;

                Vector3 cellPosition = new Vector3(x * cellSize, 0, z * cellSize);

                // Floor Tile
                if (floorPrefabs != null && floorPrefabs.Length > 0)
                {
                    GameObject floorPrefab = floorPrefabs[Random.Range(0, floorPrefabs.Length)];
                    if (floorPrefab != null)
                    {
                        GameObject floor = Instantiate(floorPrefab, cellPosition, Quaternion.identity, floorContainer);
                        floor.name = $"Floor_{x}_{z}";
                    }
                }

                // Ceiling Tile
                if (spawnCeilings && ceilingPrefab != null)
                {
                    Vector3 ceilPos = cellPosition + new Vector3(0, ceilingHeight, 0);
                    GameObject ceil = Instantiate(ceilingPrefab, ceilPos, Quaternion.identity, ceilingContainer);
                    ceil.name = $"Ceiling_{x}_{z}";
                }

                // Floor decals
                if (floorDecals != null && floorDecals.Length > 0 && Random.value < 0.4f)
                {
                    GameObject decalPrefab = floorDecals[Random.Range(0, floorDecals.Length)];
                    if (decalPrefab != null)
                    {
                        Vector3 decalPos = cellPosition + new Vector3(Random.Range(-1.2f, 1.2f), 0.02f, Random.Range(-1.2f, 1.2f));
                        Quaternion decalRot = Quaternion.Euler(0, Random.Range(0, 360), 0);
                        Instantiate(decalPrefab, decalPos, decalRot, propsContainer);
                    }
                }

                // Walls & Mounted Torches
                SpawnCellWallsAndTorches(cell, cellPosition, wallContainer, torchContainer);
            }
        }

        // 2. Spawn Corner Pillars
        SpawnIntersectionPillars(pillarContainer);

        // 3. Room 1: Player Spawn & Starter Torch
        if (playerSpawnPrefab != null)
        {
            Vector3 startPos = new Vector3(startCell.x * cellSize, 0f, startCell.z * cellSize);
            Instantiate(playerSpawnPrefab, startPos, Quaternion.Euler(0, 0, 0), mazeParent);

            if (wallTorchPrefab != null)
            {
                Vector3 wallTorchPos = startPos + new Vector3(0, 2.8f, -cellSize / 2f);
                Instantiate(wallTorchPrefab, wallTorchPos, Quaternion.identity, torchContainer);
            }

            Vector3 standPos = startPos + new Vector3(0f, 0f, 1.3f);
            if (starterStandPrefab != null)
            {
                Instantiate(starterStandPrefab, standPos, Quaternion.identity, propsContainer);
            }

            if (starterTorchPrefab != null)
            {
                Vector3 torchPos = standPos + new Vector3(0f, 0.95f, 0f);
                GameObject torchObj = Instantiate(starterTorchPrefab, torchPos, Quaternion.identity, mazeParent);
                torchObj.name = "StarterHandTorch";
            }
        }

        // 4. Room 2: Key Chamber
        Vector3 keyCenter = new Vector3(keyCell.x * cellSize, 0f, keyCell.z * cellSize);
        if (keyTablePrefab != null)
        {
            Instantiate(keyTablePrefab, keyCenter, Quaternion.identity, propsContainer);
        }
        if (keyPrefab != null)
        {
            Vector3 keyPos = keyCenter + new Vector3(0f, keyHeightOffset, 0f);
            Instantiate(keyPrefab, keyPos, Quaternion.identity, mazeParent);
        }
        if (wallTorchPrefab != null)
        {
            Vector3 ktPos = keyCenter + new Vector3(-cellSize / 2f, 2.8f, 0);
            Instantiate(wallTorchPrefab, ktPos, Quaternion.Euler(0, 90, 0), torchContainer);
        }

        // 5. Room 3: Fractal Tree Sanctuary (Procedural Fractal Tree with runtime customization)
        Vector3 fractalPos = new Vector3(fractalCell.x * cellSize, 0f, fractalCell.z * cellSize);
        if (spawnFractalTree)
        {
            GameObject treeObj;
            if (fractalTreePrefab != null)
            {
                treeObj = Instantiate(fractalTreePrefab, fractalPos, Quaternion.identity, propsContainer);
            }
            else
            {
                treeObj = new GameObject("ProceduralFractalTree");
                treeObj.transform.position = fractalPos;
                treeObj.transform.SetParent(propsContainer);
                var treeScript = treeObj.AddComponent<InteractiveFractalTree>();

                if (loadedConfig != null && loadedConfig.fractalTree != null)
                {
                    treeScript.recursionDepth = loadedConfig.fractalTree.recursionDepth;
                    treeScript.baseBranchLength = loadedConfig.fractalTree.branchLength;
                    treeScript.branchLengthReduction = loadedConfig.fractalTree.branchLengthFactor;
                    treeScript.baseRadius = loadedConfig.fractalTree.branchRadius;
                    treeScript.branchAngle = loadedConfig.fractalTree.splitAngle;
                    treeScript.spawnLeaves = loadedConfig.fractalTree.leavesEnabled;
                }
            }
            treeObj.name = "Interactive_Fractal_Tree";
            Debug.Log($"[MazeGenerator] Spawned Interactive Fractal Tree at Sanctuary ({fractalCell.x}, {fractalCell.z})");
        }

        // 6. Room 4: Exit Sanctuary
        if (doorPrefab != null)
        {
            Vector3 exitPos = new Vector3(exitCell.x * cellSize, 0f, exitCell.z * cellSize);
            Vector3 doorPos = exitPos + new Vector3(cellSize / 2f, 0f, 0f);
            Quaternion doorRot = Quaternion.Euler(0, -90, 0);

            Instantiate(doorPrefab, doorPos, doorRot, mazeParent);

            if (wallTorchPrefab != null)
            {
                Vector3 wt = exitPos + new Vector3(0, 2.8f, cellSize / 2f);
                Instantiate(wallTorchPrefab, wt, Quaternion.Euler(0, 180, 0), torchContainer);
            }
        }

        // 7. Safety Boundaries
        CreateSafetyBoundaries(mazeParent);
    }

    private void SpawnCellWallsAndTorches(MazeCell cell, Vector3 cellPos, Transform wallParent, Transform torchParent)
    {
        if (wallPrefabs == null || wallPrefabs.Length == 0) return;

        float halfSize = cellSize / 2f;

        // North Wall (+Z)
        if (cell.northWall)
        {
            Vector3 wallPos = cellPos + new Vector3(0, wallHeightOffset, halfSize);
            Quaternion wallRot = Quaternion.Euler(0, 90, 0);
            GameObject wall = Instantiate(GetRandomWallPrefab(), wallPos, wallRot, wallParent);
            wall.name = $"Wall_N_{cell.x}_{cell.z}";
        }

        // South Wall (-Z)
        if (cell.southWall)
        {
            Vector3 wallPos = cellPos + new Vector3(0, wallHeightOffset, -halfSize);
            Quaternion wallRot = Quaternion.Euler(0, 90, 0);
            GameObject wall = Instantiate(GetRandomWallPrefab(), wallPos, wallRot, wallParent);
            wall.name = $"Wall_S_{cell.x}_{cell.z}";
        }

        // East Wall (+X)
        if (cell.eastWall && !(cell.x == 2 && cell.z == 1))
        {
            Vector3 wallPos = cellPos + new Vector3(halfSize, wallHeightOffset, 0);
            Quaternion wallRot = Quaternion.identity;
            GameObject wall = Instantiate(GetRandomWallPrefab(), wallPos, wallRot, wallParent);
            wall.name = $"Wall_E_{cell.x}_{cell.z}";
        }

        // West Wall (-X)
        if (cell.westWall)
        {
            Vector3 wallPos = cellPos + new Vector3(-halfSize, wallHeightOffset, 0);
            Quaternion wallRot = Quaternion.identity;
            GameObject wall = Instantiate(GetRandomWallPrefab(), wallPos, wallRot, wallParent);
            wall.name = $"Wall_W_{cell.x}_{cell.z}";
        }
    }

    private void SpawnIntersectionPillars(Transform pillarParent)
    {
        if (pillarPrefabs == null || pillarPrefabs.Length == 0) return;

        float half = cellSize / 2f;
        for (int x = 0; x <= width; x++)
        {
            for (int z = 0; z <= depth; z++)
            {
                bool nearActiveCell = false;
                if (x > 0 && z > 0 && grid[x - 1, z - 1].isVisited) nearActiveCell = true;
                if (x < width && z < depth && grid[x, z].isVisited) nearActiveCell = true;
                if (x > 0 && z < depth && grid[x - 1, z].isVisited) nearActiveCell = true;
                if (x < width && z > 0 && grid[x, z - 1].isVisited) nearActiveCell = true;

                if (!nearActiveCell) continue;

                Vector3 pillarPos = new Vector3((x * cellSize) - half, 0f, (z * cellSize) - half);
                GameObject pillarPrefab = pillarPrefabs[Random.Range(0, pillarPrefabs.Length)];
                if (pillarPrefab != null)
                {
                    Instantiate(pillarPrefab, pillarPos, Quaternion.identity, pillarParent);
                }
            }
        }
    }

    private GameObject GetRandomWallPrefab()
    {
        return wallPrefabs[Random.Range(0, wallPrefabs.Length)];
    }

    private void CreateSafetyBoundaries(Transform parent)
    {
        GameObject boundariesGo = new GameObject("SafetyBoundaries");
        boundariesGo.transform.SetParent(parent);

        float totalWidth = width * cellSize;
        float totalDepth = depth * cellSize;
        Vector3 mazeCenter = new Vector3((width - 1) * cellSize / 2f, 0f, (depth - 1) * cellSize / 2f);

        GameObject catchFloor = new GameObject("CatchFloorCollider");
        catchFloor.transform.SetParent(boundariesGo.transform);
        catchFloor.transform.position = new Vector3(mazeCenter.x, -0.6f, mazeCenter.z);
        BoxCollider catchCol = catchFloor.AddComponent<BoxCollider>();
        catchCol.size = new Vector3(totalWidth + 40f, 1f, totalDepth + 40f);
    }
}
