using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeDrawer : MonoBehaviour
{
    private Queue<BranchData> branchesToGrow = new Queue<BranchData>();
    private float maxTreeHeight = 0f;

    [Header("Growth Settings")]
    public float baseLength = 2f;
    public float lengthVariance = 0.5f;
    public float baseAngle = 25f;
    public float angleVariance = 10f;

    [Header("Prefabs & Materials")]
    public GameObject branchPrefab;
    public GameObject leafPrefab; 
    public Material[] barkMaterials;

    [Header("Leaf Settings")]
    public float leafHeightThreshold = 0.4f;
    public float leafGrowTime = 1f;
    public Vector2 leafScaleRange = new Vector2(0.8f, 1.2f);

    [Header("Randomization")]
    [Range(0f, 1f)] public float branchProbability = 1f;


    [Header("Branch Thickness Control")]
    public float baseTrunkThickness = 0.3f;
    public float thinnessMultiplier = 1.0f;

    public Vector2 branchWidthRange = new Vector2(0.05f, 0.15f);
    public float barkColorVariance = 0.1f;

    [Header("Growth Timing")]
    public float totalGrowthTime = 15f;
    public int parallelBranches = 2;
    public float branchOverlapDelay = 0.2f;

    private Stack<TurtleState> stateStack = new Stack<TurtleState>();
    private List<Vector3> branchEndpoints = new List<Vector3>();
    private Vector3 treeBasePosition;
    private bool isFirstBranch = true;

    private struct BranchInfo
    {
        public Vector3 start;
        public Vector3 end;
        public Vector3 direction;
        public float thickness;
    }

    private class BranchData
    {
        public Vector3 start;
        public Vector3 control;
        public Vector3 end;
        public GameObject prefab;
        public BranchInfo branchInfo;
    }

    private struct TurtleState
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    public void DrawTree(string sequence)
    {
        if (GetComponent<LSystemGenerator>().preset == TreePreset.BonsaiLike)
        {
            ApplyBonsaiStyle();
        }

        // Clear existing data
        branchesToGrow.Clear();
        branchEndpoints.Clear();
        isFirstBranch = true;

        // Safe cleanup of existing leaves
        List<Transform> toDestroy = new List<Transform>();
        foreach (Transform child in GetComponentsInChildren<Transform>())
        {
            if (child != null && child != transform && child.name.StartsWith("Leaf"))
            {
                toDestroy.Add(child);
            }
        }
        foreach (var leaf in toDestroy)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(leaf.gameObject);
            else
                Destroy(leaf.gameObject);
#else
            Destroy(leaf.gameObject);
#endif
        }

        Vector3 position = transform.position;
        Quaternion rotation = Quaternion.identity;
        maxTreeHeight = position.y;
        treeBasePosition = transform.position;

        // Parse L-System sequence
        foreach (char c in sequence)
        {
            if (c == 'F')
            {
                if (Random.value > branchProbability) continue;

                float length = baseLength + Random.Range(-lengthVariance, lengthVariance);
                Vector3 direction = rotation * Vector3.up;

                Vector3 start = position;
                Vector3 end = start + direction * length;
                position = end;

                if (end.y > maxTreeHeight) maxTreeHeight = end.y;

                // Bezier curve
                Vector3 mid = (start + end) / 2f;
                Vector3 curveOffset = rotation * new Vector3(
                    Random.Range(-0.4f, 0.4f),
                    Random.Range(-0.2f, 0.4f),
                    Random.Range(-0.4f, 0.4f)
                );
                Vector3 control = mid + curveOffset;

                GameObject branch = new GameObject("Branch");
                branch.transform.parent = transform;
                AssignRandomMaterial(branch, barkMaterials, barkColorVariance);

                float thickness = isFirstBranch ? baseTrunkThickness : CalculateBranchThickness(end);
                isFirstBranch = false;

                BranchInfo branchInfo = new BranchInfo
                {
                    start = start,
                    end = end,
                    direction = direction,
                    thickness = thickness
                };

                branchesToGrow.Enqueue(new BranchData
                {
                    start = start,
                    control = control,
                    end = end,
                    prefab = branch,
                    branchInfo = branchInfo
                });
            }
            else if (c == '+') rotation *= Quaternion.Euler(0, 0, RandomizedAngle());
            else if (c == '-') rotation *= Quaternion.Euler(0, 0, -RandomizedAngle());
            else if (c == '&') rotation *= Quaternion.Euler(RandomizedAngle(), 0, 0);
            else if (c == '^') rotation *= Quaternion.Euler(-RandomizedAngle(), 0, 0);
            else if (c == '\\') rotation *= Quaternion.Euler(0, RandomizedAngle(), 0);
            else if (c == '/') rotation *= Quaternion.Euler(0, -RandomizedAngle(), 0);
            else if (c == '|') rotation *= Quaternion.Euler(0, 180, 0);
            else if (c == '[')
                stateStack.Push(new TurtleState { position = position, rotation = rotation });
            else if (c == ']')
            {
                if (stateStack.Count > 0)
                {
                    if (position.y >= maxTreeHeight * leafHeightThreshold)
                        branchEndpoints.Add(position);

                    TurtleState state = stateStack.Pop();
                    position = state.position;
                    rotation = state.rotation;
                }
            }
        }

        if (position.y >= maxTreeHeight * leafHeightThreshold)
            branchEndpoints.Add(position);

        StartCoroutine(GrowTreeWithLeaves());
    }

    private float CalculateBranchThickness(Vector3 point)
    {
        // vertical distance ratio (0 = base, 1 = top)
        float verticalRatio = Mathf.InverseLerp(0f, maxTreeHeight, point.y);

        // horizontal spread ratio (0 = center trunk, 1 = far outward)
        float horizontalDistance = Vector3.Distance(
            new Vector3(point.x, 0, point.z),
            new Vector3(treeBasePosition.x, 0, treeBasePosition.z)
        );
        float maxHorizontalDistance = 4f;
        float horizontalRatio = Mathf.Clamp01(horizontalDistance / maxHorizontalDistance);

        // combine vertical + horizontal to create taper
        float taperFactor = Mathf.Clamp01((verticalRatio + horizontalRatio) / 2f);
        taperFactor = Mathf.Pow(taperFactor, thinnessMultiplier); // exaggeration control

        // smoothly interpolate between thick trunk and thin branches
        return Mathf.Lerp(baseTrunkThickness, branchWidthRange.x, taperFactor);
    }

    private IEnumerator GrowTreeWithLeaves()
    {
        float timePerBranch = totalGrowthTime / Mathf.Max(branchesToGrow.Count, 1);
        List<Coroutine> activeCoroutines = new List<Coroutine>();

        while (branchesToGrow.Count > 0)
        {
            for (int i = 0; i < parallelBranches && branchesToGrow.Count > 0; i++)
            {
                BranchData branch = branchesToGrow.Dequeue();
                float randomDuration = timePerBranch * Random.Range(0.7f, 1.3f);
                Coroutine grow = StartCoroutine(AnimateBranchGrowth(
                    branch.prefab,
                    branch.start,
                    branch.control,
                    branch.end,
                    randomDuration,
                    branch.branchInfo
                ));
                activeCoroutines.Add(grow);
                yield return new WaitForSeconds(branchOverlapDelay * Random.Range(0.8f, 1.2f));
            }
        }

        foreach (var co in activeCoroutines)
            if (co != null) yield return co;

        CreateLeafPrefabs();
    }

    private void CreateLeafPrefabs()
    {
        if (leafPrefab == null)
        {
            Debug.LogWarning("Leaf Prefab not assigned!");
            return;
        }

        foreach (Vector3 endpoint in branchEndpoints)
        {
            GameObject leaf = Instantiate(leafPrefab, endpoint, Quaternion.identity, transform);

            leaf.transform.rotation = Quaternion.Euler(
                Random.Range(-20f, 20f),
                Random.Range(0f, 360f),
                Random.Range(-20f, 20f)
            );

            float finalScale = Random.Range(leafScaleRange.x, leafScaleRange.y);
            leaf.transform.localScale = Vector3.zero;

            StartCoroutine(GrowLeaf(leaf.transform, finalScale));
        }
    }

    private IEnumerator GrowLeaf(Transform leaf, float targetScale)
    {
        float elapsed = 0f;
        while (elapsed < leafGrowTime)
        {
            float t = elapsed / leafGrowTime;
            float scale = Mathf.Lerp(0f, targetScale, t);
            leaf.localScale = Vector3.one * scale;
            elapsed += Time.deltaTime;
            yield return null;
        }


        // Random rotation
        leaf.transform.rotation = Quaternion.Euler(
            Random.Range(-30f, 30f),
            Random.Range(0f, 360f),
            Random.Range(-30f, 30f)
        );
        
        leaf.localScale = Vector3.one * targetScale;
    }

    private IEnumerator AnimateBranchGrowth(GameObject branch, Vector3 start, Vector3 control, Vector3 end, float duration, BranchInfo branchInfo)
    {
        MeshFilter mf = branch.GetComponent<MeshFilter>();
        if (!mf) mf = branch.AddComponent<MeshFilter>();

        MeshRenderer mr = branch.GetComponent<MeshRenderer>();
        if (!mr) mr = branch.AddComponent<MeshRenderer>();

        AssignRandomMaterial(branch, barkMaterials, barkColorVariance);

        Mesh branchMesh = GenerateBezierBranchMesh(start, control, end, branchInfo.thickness, maxTreeHeight);
        mf.mesh = branchMesh;

        yield return new WaitForSeconds(Random.Range(0.1f, 0.3f));
    }

    private float RandomizedAngle()
    {
        return baseAngle + Random.Range(-angleVariance, angleVariance);
    }

    private Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        return uu * p0 + 2 * u * t * p1 + tt * p2;
    }

    private Mesh GenerateBezierBranchMesh(Vector3 start, Vector3 control, Vector3 end, float baseWidth, float maxTreeHeight, int curveSegments = 12, int radialSegments = 8)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();

        for (int i = 0; i <= curveSegments; i++)
        {
            float t = (float)i / curveSegments;
            Vector3 center = CalculateQuadraticBezierPoint(t, start, control, end);
            Vector3 forward = (CalculateQuadraticBezierPoint(Mathf.Clamp01(t + 0.01f), start, control, end) - center).normalized;
            if (forward == Vector3.zero) forward = Vector3.up;

            Quaternion rot = Quaternion.LookRotation(forward);

            float heightRatio = Mathf.InverseLerp(0f, maxTreeHeight, center.y);
            float taper = Mathf.Lerp(1f, 0.2f, heightRatio);

            for (int j = 0; j < radialSegments; j++)
            {
                float angle = (j / (float)radialSegments) * Mathf.PI * 2f;
                Vector3 localPos = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * baseWidth * 0.5f * taper;

                Vector3 vertex = center + rot * localPos;
                vertices.Add(vertex);
                normals.Add((rot * localPos).normalized);
            }
        }

        for (int i = 0; i < curveSegments; i++)
        {
            for (int j = 0; j < radialSegments; j++)
            {
                int current = i * radialSegments + j;
                int next = current + radialSegments;
                int nextJ = (j + 1) % radialSegments;

                triangles.Add(current);
                triangles.Add(next);
                triangles.Add(i * radialSegments + nextJ);

                triangles.Add(next);
                triangles.Add(next + nextJ - j);
                triangles.Add(i * radialSegments + nextJ);
            }
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetNormals(normals);
        return mesh;
    }

    private void AssignRandomMaterial(GameObject obj, Material[] materials, float tintVariance)
    {
        if (materials.Length == 0) return;

        Material mat = materials[Random.Range(0, materials.Length)];
        Material instanceMat = new Material(mat);

        if (instanceMat.HasProperty("_Color"))
        {
            Color baseColor = instanceMat.color;
            float h, s, v;
            Color.RGBToHSV(baseColor, out h, out s, out v);

            h = Mathf.Clamp01(h + Random.Range(-0.02f, 0.02f));
            s = Mathf.Clamp01(s + Random.Range(-0.05f, 0.1f));
            v = Mathf.Clamp01(v - Random.Range(0.1f, 0.25f));

            Color randomTint = Color.HSVToRGB(h, s, v);
            instanceMat.color = randomTint;
        }

        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material = instanceMat;
    }

    public void ApplyBonsaiStyle()
    {
        baseLength = 0.6f;
        lengthVariance = 0.2f;
        baseAngle = 35f;
        angleVariance = 20f;
        branchProbability = 0.95f;
        branchWidthRange = new Vector2(0.02f, 0.07f);
        barkColorVariance = 0.2f;

        leafHeightThreshold = 0.25f;
        totalGrowthTime = 8f;
        parallelBranches = 3;
        branchOverlapDelay = 0.1f;
    }
}
