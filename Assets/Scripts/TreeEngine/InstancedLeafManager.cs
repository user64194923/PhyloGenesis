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

    public void AddLeavesAtPoint(Vector3 position, Vector3 direction, int leafCount, float spread = 0.3f)
    {
        Debug.Log($"Adding {leafCount} leaves at position {position} with spread {spread}");
        
        // Create a very tight cluster directly at the branch endpoint
        for (int i = 0; i < leafCount; i++)
        {
            // Create leaves in a small cluster around the exact endpoint
            Vector3 randomOffset = Random.insideUnitSphere * spread * 0.5f; // Much smaller spread
            Vector3 leafPos = position + randomOffset;
            
            // Make sure leaves don't go below the endpoint
            if (leafPos.y < position.y - spread * 0.2f)
                leafPos.y = position.y - spread * 0.2f;
            
            // Normal should point outward from the endpoint
            Vector3 normal = randomOffset.normalized;
            if (normal.magnitude < 0.1f) 
                normal = Random.insideUnitSphere.normalized;
            
            Debug.Log($"  Leaf {i} at: {leafPos} (offset: {randomOffset})");

            leafPos = position;
            normal = new Vector3(0f,0f,0f);

            AddLeaf(leafPos, normal);
        }
        
        Debug.Log($"Total leaves after adding: {allLeaves.Count}");
    }

    public void AddLeavesToBranch(Vector3 branchStart, Vector3 branchEnd, int leafCount, float branchThickness = 0.05f)
    {
        Vector3 branchDir = (branchEnd - branchStart).normalized;
        float branchLength = Vector3.Distance(branchStart, branchEnd);

        // Create perpendicular vectors to the branch direction for radial positioning
        Vector3 perpendicular1 = Vector3.Cross(branchDir, Vector3.up).normalized;
        if (perpendicular1.magnitude < 0.1f) // If branch is vertical, use forward
            perpendicular1 = Vector3.Cross(branchDir, Vector3.forward).normalized;
        Vector3 perpendicular2 = Vector3.Cross(branchDir, perpendicular1).normalized;

        for (int i = 0; i < leafCount; i++)
        {
            float t = Random.Range(0.3f, 1.0f); // Don't place leaves at very start of branch
            Vector3 branchPos = Vector3.Lerp(branchStart, branchEnd, t);
            
            // Position leaves around the branch circumference, not randomly in space
            float angle = Random.Range(0f, 2f * Mathf.PI);
            float radiusMultiplier = Random.Range(0.8f, 2.5f); // Stay close to branch surface
            float radius = branchThickness * radiusMultiplier;
            
            Vector3 radialOffset = (perpendicular1 * Mathf.Cos(angle) + perpendicular2 * Mathf.Sin(angle)) * radius;
            Vector3 leafPos = branchPos + radialOffset;
            
            // Slight random offset along branch direction
            leafPos += branchDir * Random.Range(-0.05f, 0.05f);
            
            // Normal should point outward from branch
            Vector3 normal = radialOffset.normalized;
            // Add some randomness but keep it generally pointing outward
            normal = Vector3.Slerp(normal, Random.insideUnitSphere.normalized, 0.3f).normalized;
            
            AddLeaf(leafPos, normal);
        }
    }

    public void AddLeafClusters(Vector3 branchStart, Vector3 branchEnd, int clusterCount, int leavesPerCluster, float branchThickness = 0.05f)
    {
        Vector3 branchDir = (branchEnd - branchStart).normalized;
        
        for (int cluster = 0; cluster < clusterCount; cluster++)
        {
            // Position clusters along the branch
            float t = Random.Range(0.4f, 1.0f);
            Vector3 clusterCenter = Vector3.Lerp(branchStart, branchEnd, t);
            
            // Offset cluster slightly from branch center
            Vector3 perpendicular = Vector3.Cross(branchDir, Vector3.up).normalized;
            if (perpendicular.magnitude < 0.1f)
                perpendicular = Vector3.Cross(branchDir, Vector3.forward).normalized;
            
            float angle = Random.Range(0f, 2f * Mathf.PI);
            Vector3 clusterOffset = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, branchDir) * perpendicular * branchThickness * Random.Range(0.5f, 2f);
            clusterCenter += clusterOffset;
            
            // Add leaves in this cluster
            for (int leaf = 0; leaf < leavesPerCluster; leaf++)
            {
                Vector3 leafOffset = Random.insideUnitSphere * 0.15f; // Small tight cluster
                Vector3 leafPos = clusterCenter + leafOffset;
                
                Vector3 normal = (leafPos - (clusterCenter - clusterOffset)).normalized;
                normal = Vector3.Slerp(normal, Random.insideUnitSphere.normalized, 0.2f).normalized;
                
                AddLeaf(leafPos, normal);
            }
        }
    }

    public void ClearLeaves()
    {
        allLeaves.Clear();
        leafBatches.Clear();
        Debug.Log("Cleared all leaves");
    }

    public int GetBatchCount()
    {
        return leafBatches.Count;
    }

    public int GetLeafCount()
    {
        return allLeaves.Count;
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
        
        Debug.Log($"Rebuilt {leafBatches.Count} batches with {allLeaves.Count} total leaves");
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
        
        // Draw a larger gizmo at each leaf position to make them more visible
        Gizmos.color = Color.yellow;
        foreach (LeafData leaf in allLeaves)
        {
            Gizmos.DrawSphere(leaf.position, 0.05f);
        }
    }
}