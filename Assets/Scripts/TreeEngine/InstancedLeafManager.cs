using UnityEngine;
using System.Collections.Generic;

public class InstancedLeafManager : MonoBehaviour
{
    [Header("Leaf Settings")]
    public Material leafMaterial;
    public Mesh leafMesh; // Simple quad mesh
    public int maxLeavesPerBatch = 1023; // Unity's limit for GPU instancing
    
    [Header("Leaf Generation")]
    public float leafSize = 0.5f;
    public int atlasVariations = 4; // Number of leaf textures in atlas
    public Vector2 leafSizeVariation = new Vector2(0.8f, 1.2f);
    
    private List<LeafBatch> leafBatches = new List<LeafBatch>();
    private List<LeafData> allLeaves = new List<LeafData>();
    
    // Property IDs for MaterialPropertyBlock
    private static readonly int InstanceDataID = Shader.PropertyToID("_InstanceData");
    private static readonly int InstanceRotationID = Shader.PropertyToID("_InstanceRotation");
    private static readonly int InstanceScaleID = Shader.PropertyToID("_InstanceScale");
    private static readonly int InstanceColorID = Shader.PropertyToID("_InstanceColor");

    [System.Serializable]
    public struct LeafData
    {
        public Vector3 position;
        public Quaternion rotation;
        public float scale;
        public Color color;
        public int atlasIndex;
    }

    private class LeafBatch
    {
        public Matrix4x4[] matrices;
        public Vector4[] instanceData;
        public Vector4[] instanceRotations;
        public float[] instanceScales;
        public Vector4[] instanceColors;
        public MaterialPropertyBlock propertyBlock;
        public int count;

        public LeafBatch(int capacity)
        {
            matrices = new Matrix4x4[capacity];
            instanceData = new Vector4[capacity];
            instanceRotations = new Vector4[capacity];
            instanceScales = new float[capacity];
            instanceColors = new Vector4[capacity];
            propertyBlock = new MaterialPropertyBlock();
            count = 0;
        }
    }

    void Start()
    {
        // Create a simple quad mesh if none provided
        if (leafMesh == null)
        {
            leafMesh = CreateLeafQuad();
        }
    }

    void Update()
    {
        RenderLeaves();
    }

    public void AddLeaf(Vector3 position, Vector3 normal = default)
    {
        if (normal == default) normal = Vector3.up;

        LeafData leaf = new LeafData
        {
            position = position,
            rotation = Quaternion.LookRotation(normal, Random.insideUnitSphere.normalized),
            scale = Random.Range(leafSizeVariation.x, leafSizeVariation.y) * leafSize,
            color = new Color(
                Random.Range(0.7f, 1.0f),
                Random.Range(0.8f, 1.0f),
                Random.Range(0.3f, 0.7f),
                1.0f
            ),
            atlasIndex = Random.Range(0, atlasVariations)
        };

        allLeaves.Add(leaf);
        RebuildBatches();
    }

    public void AddLeavesToBranch(Vector3 branchStart, Vector3 branchEnd, int leafCount)
    {
        Vector3 branchDir = (branchEnd - branchStart).normalized;
        float branchLength = Vector3.Distance(branchStart, branchEnd);

        for (int i = 0; i < leafCount; i++)
        {
            float t = Random.Range(0.2f, 1.0f); // Don't place leaves at very start of branch
            Vector3 branchPos = Vector3.Lerp(branchStart, branchEnd, t);
            
            // Offset slightly from branch
            Vector3 offset = Random.insideUnitSphere * 0.1f;
            Vector3 leafPos = branchPos + offset;
            
            // Random normal biased away from branch
            Vector3 normal = (Random.insideUnitSphere + branchDir.normalized).normalized;
            
            AddLeaf(leafPos, normal);
        }
    }

    public void ClearLeaves()
    {
        allLeaves.Clear();
        leafBatches.Clear();
    }

    private void RebuildBatches()
    {
        leafBatches.Clear();

        LeafBatch currentBatch = null;
        
        for (int i = 0; i < allLeaves.Count; i++)
        {
            if (currentBatch == null || currentBatch.count >= maxLeavesPerBatch)
            {
                currentBatch = new LeafBatch(maxLeavesPerBatch);
                leafBatches.Add(currentBatch);
            }

            LeafData leaf = allLeaves[i];
            int batchIndex = currentBatch.count;

            // Set matrix for rendering (even though we override in shader)
            currentBatch.matrices[batchIndex] = Matrix4x4.TRS(leaf.position, leaf.rotation, Vector3.one * leaf.scale);
            
            // Set instance data for shader
            currentBatch.instanceData[batchIndex] = new Vector4(leaf.position.x, leaf.position.y, leaf.position.z, leaf.atlasIndex);
            currentBatch.instanceRotations[batchIndex] = new Vector4(leaf.rotation.x, leaf.rotation.y, leaf.rotation.z, leaf.rotation.w);
            currentBatch.instanceScales[batchIndex] = leaf.scale;
            currentBatch.instanceColors[batchIndex] = leaf.color;

            currentBatch.count++;
        }
    }

    private void RenderLeaves()
    {
        if (leafMaterial == null || leafMesh == null) return;

        foreach (LeafBatch batch in leafBatches)
        {
            if (batch.count == 0) continue;

            // Set up property block with instance data
            batch.propertyBlock.SetVectorArray(InstanceDataID, batch.instanceData);
            batch.propertyBlock.SetVectorArray(InstanceRotationID, batch.instanceRotations);
            batch.propertyBlock.SetFloatArray(InstanceScaleID, batch.instanceScales);
            batch.propertyBlock.SetVectorArray(InstanceColorID, batch.instanceColors);

            // Render the batch
            Graphics.DrawMeshInstanced(
                leafMesh,
                0,
                leafMaterial,
                batch.matrices,
                batch.count,
                batch.propertyBlock,
                UnityEngine.Rendering.ShadowCastingMode.On,
                true
            );
        }
    }

    private Mesh CreateLeafQuad()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Leaf Quad";

        // Vertices for a quad
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3(0.5f, -0.5f, 0),
            new Vector3(0.5f, 0.5f, 0),
            new Vector3(-0.5f, 0.5f, 0)
        };

        // UVs
        Vector2[] uvs = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };

        // Triangles
        int[] triangles = new int[]
        {
            0, 1, 2,
            0, 2, 3,
            // Back face
            2, 1, 0,
            3, 2, 0
        };

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    // Helper method for tree generation systems
    public void GenerateLeavesFromBranches(Transform[] branches, int leavesPerBranch = 5)
    {
        ClearLeaves();
        
        foreach (Transform branch in branches)
        {
            if (branch.childCount > 0)
            {
                // If branch has children, use start and end points
                Vector3 start = branch.position;
                Vector3 end = branch.GetChild(0).position;
                AddLeavesToBranch(start, end, leavesPerBranch);
            }
            else
            {
                // Single point branch
                Vector3 pos = branch.position;
                for (int i = 0; i < leavesPerBranch; i++)
                {
                    Vector3 leafPos = pos + Random.insideUnitSphere * 0.2f;
                    AddLeaf(leafPos);
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw leaf positions for debugging
        Gizmos.color = Color.green;
        foreach (LeafData leaf in allLeaves)
        {
            Gizmos.DrawWireCube(leaf.position, Vector3.one * leaf.scale * 0.1f);
        }
    }
}