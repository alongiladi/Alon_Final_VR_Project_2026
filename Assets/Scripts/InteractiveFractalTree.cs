using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates a procedural 3D fractal tree with interactive runtime modification capabilities.
/// Supports modifying recursion depth, branch angles, branch lengths, and foliage dynamically.
/// </summary>
[ExecuteAlways]
public class InteractiveFractalTree : MonoBehaviour
{
    [Header("Fractal Recursion & Geometry")]
    [Range(1, 6)]
    [Tooltip("Number of recursive branching iterations")]
    public int recursionDepth = 4;

    [Range(0.5f, 3.0f)]
    [Tooltip("Base trunk/branch initial length")]
    public float baseBranchLength = 1.6f;

    [Range(0.4f, 0.9f)]
    [Tooltip("Branch length multiplier per recursion level")]
    public float branchLengthReduction = 0.72f;

    [Range(0.02f, 0.3f)]
    [Tooltip("Branch thickness radius")]
    public float baseRadius = 0.1f;

    [Range(15f, 65f)]
    [Tooltip("Split angle in degrees for branches")]
    public float branchAngle = 35f;

    [Header("Appearance & Customization")]
    public Material trunkMaterial;
    public Material leafMaterial;
    public bool spawnLeaves = true;
    public Color trunkColor = new Color(0.28f, 0.18f, 0.11f);
    public Color leafColor = new Color(0.18f, 0.85f, 0.35f, 0.9f);

    [Header("Interactive Animation")]
    public bool animateGrowth = true;
    public float swaySpeed = 1.5f;
    public float swayAmount = 2.0f;

    private GameObject treeContainer;
    private List<Transform> branchTransforms = new List<Transform>();
    private bool needsRebuild = true;
    private int lastDepth;
    private float lastAngle;
    private float lastLength;

    private void Start()
    {
        BuildFractalTree();
    }

    private void Update()
    {
        // Detect parameter changes and rebuild
        if (recursionDepth != lastDepth || Mathf.Abs(branchAngle - lastAngle) > 0.01f || Mathf.Abs(baseBranchLength - lastLength) > 0.01f)
        {
            BuildFractalTree();
        }

        // Gentle procedural wind sway
        if (animateGrowth && treeContainer != null)
        {
            float sway = Mathf.Sin(Time.time * swaySpeed) * swayAmount;
            treeContainer.transform.localRotation = Quaternion.Euler(sway, 0f, sway * 0.5f);
        }
    }

    /// <summary>
    /// Re-generates the procedural fractal tree mesh hierarchy.
    /// </summary>
    public void BuildFractalTree()
    {
        lastDepth = recursionDepth;
        lastAngle = branchAngle;
        lastLength = baseBranchLength;

        if (treeContainer != null)
        {
            if (Application.isPlaying)
                Destroy(treeContainer);
            else
                DestroyImmediate(treeContainer);
        }

        treeContainer = new GameObject("GeneratedFractalMesh");
        treeContainer.transform.SetParent(this.transform, false);
        branchTransforms.Clear();

        EnsureDefaultMaterials();

        // Recursively build tree starting at root
        GrowBranch(treeContainer.transform, Vector3.zero, Vector3.up, baseBranchLength, baseRadius, 1);
    }

    private void GrowBranch(Transform parent, Vector3 startPos, Vector3 direction, float length, float radius, int currentDepth)
    {
        if (currentDepth > recursionDepth) return;

        Vector3 endPos = startPos + direction.normalized * length;

        // Create cylinder primitive for branch
        GameObject branchObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        branchObj.name = $"Branch_D{currentDepth}";
        branchObj.transform.SetParent(parent, false);

        // Position cylinder midway between start and end
        Vector3 midPoint = (startPos + endPos) / 2f;
        branchObj.transform.localPosition = midPoint;
        branchObj.transform.up = direction.normalized;
        branchObj.transform.localScale = new Vector3(radius * 2f, length / 2f, radius * 2f);

        if (trunkMaterial != null)
        {
            var rend = branchObj.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial = trunkMaterial;
        }

        branchTransforms.Add(branchObj.transform);

        // Base case: add leaves at the branch tips
        if (currentDepth == recursionDepth && spawnLeaves)
        {
            GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaf.name = "FractalLeaf";
            leaf.transform.SetParent(branchObj.transform, false);
            leaf.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            leaf.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);

            if (leafMaterial != null)
            {
                var leafRend = leaf.GetComponent<Renderer>();
                if (leafRend != null) leafRend.sharedMaterial = leafMaterial;
            }
        }

        // Recursive branching step (3D multi-axis fork)
        float nextLength = length * branchLengthReduction;
        float nextRadius = radius * 0.75f;

        // Branch 1 (Pitch + Yaw)
        Quaternion rot1 = Quaternion.Euler(branchAngle, 0f, 0f);
        Vector3 dir1 = rot1 * direction;
        GrowBranch(parent, endPos, dir1, nextLength, nextRadius, currentDepth + 1);

        // Branch 2 (Pitch - Yaw 120 deg)
        Quaternion rot2 = Quaternion.Euler(-branchAngle * 0.7f, 120f, branchAngle * 0.5f);
        Vector3 dir2 = rot2 * direction;
        GrowBranch(parent, endPos, dir2, nextLength, nextRadius, currentDepth + 1);

        // Branch 3 (Pitch - Yaw 240 deg)
        Quaternion rot3 = Quaternion.Euler(-branchAngle * 0.7f, -120f, -branchAngle * 0.5f);
        Vector3 dir3 = rot3 * direction;
        GrowBranch(parent, endPos, dir3, nextLength, nextRadius, currentDepth + 1);
    }

    private void EnsureDefaultMaterials()
    {
        if (trunkMaterial == null)
        {
            Shader standardShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (standardShader != null)
            {
                trunkMaterial = new Material(standardShader);
                trunkMaterial.color = trunkColor;
            }
        }

        if (leafMaterial == null)
        {
            Shader standardShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (standardShader != null)
            {
                leafMaterial = new Material(standardShader);
                leafMaterial.color = leafColor;
            }
        }
    }

    /// <summary>
    /// Interactive methods to change the fractal tree properties dynamically at runtime
    /// </summary>
    public void IncreaseDepth()
    {
        if (recursionDepth < 6)
        {
            recursionDepth++;
            BuildFractalTree();
        }
    }

    public void DecreaseDepth()
    {
        if (recursionDepth > 1)
        {
            recursionDepth--;
            BuildFractalTree();
        }
    }

    public void SetBranchAngle(float newAngle)
    {
        branchAngle = Mathf.Clamp(newAngle, 10f, 75f);
        BuildFractalTree();
    }
}
