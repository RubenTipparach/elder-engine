namespace RasterizerCube;

public class OctreeNode
{
    public AABB Bounds;
    public OctreeNode[] Children; // 8 children (null if leaf)
    public List<int> TriangleIndices; // Indices into triangle list
    public List<int> LightIndices; // Indices into light list
    public int Depth;

    private const int MaxTrianglesPerNode = 8;
    private const int MaxDepth = 6;

    public OctreeNode(AABB bounds, int depth = 0)
    {
        Bounds = bounds;
        Depth = depth;
        Children = null;
        TriangleIndices = new List<int>();
        LightIndices = new List<int>();
    }

    public bool IsLeaf => Children == null;

    public void Insert(int triangleIndex, AABB triangleBounds, List<Triangle> triangles)
    {
        // If this is a leaf and we haven't exceeded capacity or depth, add here
        if (IsLeaf)
        {
            TriangleIndices.Add(triangleIndex);

            // Subdivide if we have too many triangles and haven't reached max depth
            if (TriangleIndices.Count > MaxTrianglesPerNode && Depth < MaxDepth)
            {
                Subdivide(triangles);
            }
            return;
        }

        // Insert into children
        for (int i = 0; i < 8; i++)
        {
            if (Children[i].Bounds.Intersects(triangleBounds))
            {
                Children[i].Insert(triangleIndex, triangleBounds, triangles);
            }
        }
    }

    public void InsertLight(int lightIndex, AABB lightBounds)
    {
        // Lights are inserted into all nodes they intersect with
        if (!Bounds.Intersects(lightBounds))
            return;

        LightIndices.Add(lightIndex);

        // Also insert into children if they exist
        if (!IsLeaf)
        {
            for (int i = 0; i < 8; i++)
            {
                Children[i].InsertLight(lightIndex, lightBounds);
            }
        }
    }

    public void ClearLights()
    {
        LightIndices.Clear();
        if (!IsLeaf)
        {
            for (int i = 0; i < 8; i++)
            {
                Children[i].ClearLights();
            }
        }
    }

    private void Subdivide(List<Triangle> triangles)
    {
        Float3 center = Bounds.Center;
        Float3 halfSize = Bounds.Size * 0.5f;

        Children = new OctreeNode[8];

        // Create 8 child nodes
        for (int i = 0; i < 8; i++)
        {
            Float3 offset = new Float3(
                (i & 1) == 0 ? -halfSize.X * 0.5f : halfSize.X * 0.5f,
                (i & 2) == 0 ? -halfSize.Y * 0.5f : halfSize.Y * 0.5f,
                (i & 4) == 0 ? -halfSize.Z * 0.5f : halfSize.Z * 0.5f
            );

            Float3 childCenter = center + offset;
            Float3 childMin = childCenter - halfSize * 0.5f;
            Float3 childMax = childCenter + halfSize * 0.5f;

            Children[i] = new OctreeNode(new AABB(childMin, childMax), Depth + 1);
        }

        // Redistribute triangles to children
        foreach (int triIndex in TriangleIndices)
        {
            AABB triBounds = AABB.FromTriangle(triangles[triIndex]);
            for (int i = 0; i < 8; i++)
            {
                if (Children[i].Bounds.Intersects(triBounds))
                {
                    Children[i].TriangleIndices.Add(triIndex);
                }
            }
        }

        // Clear parent's triangle list as they're now in children
        TriangleIndices.Clear();
    }

    public void GetLightsForTriangle(int triangleIndex, Triangle triangle, HashSet<int> resultLightIndices)
    {
        // Add lights from this node
        foreach (int lightIndex in LightIndices)
        {
            resultLightIndices.Add(lightIndex);
        }

        // Search children
        if (!IsLeaf)
        {
            AABB triBounds = AABB.FromTriangle(triangle);
            for (int i = 0; i < 8; i++)
            {
                if (Children[i].Bounds.Intersects(triBounds))
                {
                    Children[i].GetLightsForTriangle(triangleIndex, triangle, resultLightIndices);
                }
            }
        }
    }
}

public class Octree
{
    public OctreeNode Root;
    private List<Triangle> triangles;
    private List<PointLight> lights;

    public Octree(AABB worldBounds)
    {
        Root = new OctreeNode(worldBounds);
        triangles = new List<Triangle>();
        lights = new List<PointLight>();
    }

    public void Build(List<Triangle> tris)
    {
        triangles = tris;
        Root = new OctreeNode(CalculateWorldBounds(tris));

        for (int i = 0; i < triangles.Count; i++)
        {
            AABB triBounds = AABB.FromTriangle(triangles[i]);
            Root.Insert(i, triBounds, triangles);
        }
    }

    public void UpdateLights(List<PointLight> newLights)
    {
        lights = newLights;

        // Clear existing light references
        Root.ClearLights();

        // Insert all lights
        for (int i = 0; i < lights.Count; i++)
        {
            AABB lightBounds = AABB.FromLight(lights[i]);
            Root.InsertLight(i, lightBounds);
        }
    }

    public List<PointLight> GetLightsForTriangle(int triangleIndex)
    {
        var lightIndices = new HashSet<int>();
        Root.GetLightsForTriangle(triangleIndex, triangles[triangleIndex], lightIndices);

        var result = new List<PointLight>();
        foreach (int index in lightIndices)
        {
            result.Add(lights[index]);
        }
        return result;
    }

    private AABB CalculateWorldBounds(List<Triangle> tris)
    {
        if (tris.Count == 0)
            return new AABB(new Float3(-10, -10, -10), new Float3(10, 10, 10));

        AABB bounds = AABB.FromTriangle(tris[0]);
        for (int i = 1; i < tris.Count; i++)
        {
            bounds = AABB.Combine(bounds, AABB.FromTriangle(tris[i]));
        }

        // Expand bounds slightly to ensure everything fits
        Float3 padding = bounds.Size * 0.1f;
        bounds.Min = bounds.Min - padding;
        bounds.Max = bounds.Max + padding;

        return bounds;
    }
}
