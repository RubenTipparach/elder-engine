namespace RasterizerCube;

public static class TerrainGenerator
{
    public static List<Triangle> GenerateTerrain(Texture grassTexture, Texture dirtTexture, int gridWidth = 8, int gridDepth = 8, int maxLayers = 3)
    {
        var triangles = new List<Triangle>();
        var random = new Random(42); // Fixed seed for consistent terrain

        // Create height map with some randomness
        int[,] heightMap = new int[gridWidth, gridDepth];

        for (int z = 0; z < gridDepth; z++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                // Generate height with some noise
                float noiseValue = SimplexNoise(x * 0.3f, z * 0.3f);
                int height = (int)((noiseValue + 1f) * 0.5f * maxLayers); // Map from [-1,1] to [0, maxLayers]
                height = Math.Clamp(height, 1, maxLayers);
                heightMap[x, z] = height;
            }
        }

        // Generate cubes based on height map
        for (int z = 0; z < gridDepth; z++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                int height = heightMap[x, z];

                // Create vertical stack of cubes
                for (int y = 0; y < height; y++)
                {
                    Float3 position = new Float3(x * 2, y * 2, z * 2);
                    bool isTopLayer = (y == height - 1);

                    AddCubeAt(triangles, position, grassTexture, dirtTexture, isTopLayer,
                              x, y, z, gridWidth, gridDepth, heightMap);
                }
            }
        }

        return triangles;
    }

    private static void AddCubeAt(List<Triangle> triangles, Float3 position, Texture grassTexture,
                                  Texture dirtTexture, bool isTopLayer, int gridX, int gridY, int gridZ,
                                  int gridWidth, int gridDepth, int[,] heightMap)
    {
        Float3 center = position;
        float size = 1f;

        // Front face (-Z) - only if not blocked
        if (gridZ == 0 || heightMap[gridX, gridZ - 1] <= gridY)
        {
            AddQuad(triangles,
                new Float3(center.X - size, center.Y - size, center.Z - size),
                new Float3(center.X - size, center.Y + size, center.Z - size),
                new Float3(center.X + size, center.Y + size, center.Z - size),
                new Float3(center.X + size, center.Y - size, center.Z - size),
                new Float3(0, 0, -1), dirtTexture);
        }

        // Back face (+Z) - only if not blocked
        if (gridZ == gridDepth - 1 || heightMap[gridX, gridZ + 1] <= gridY)
        {
            AddQuad(triangles,
                new Float3(center.X + size, center.Y - size, center.Z + size),
                new Float3(center.X + size, center.Y + size, center.Z + size),
                new Float3(center.X - size, center.Y + size, center.Z + size),
                new Float3(center.X - size, center.Y - size, center.Z + size),
                new Float3(0, 0, 1), dirtTexture);
        }

        // Left face (-X) - only if not blocked
        if (gridX == 0 || heightMap[gridX - 1, gridZ] <= gridY)
        {
            AddQuad(triangles,
                new Float3(center.X - size, center.Y - size, center.Z - size),
                new Float3(center.X - size, center.Y - size, center.Z + size),
                new Float3(center.X - size, center.Y + size, center.Z + size),
                new Float3(center.X - size, center.Y + size, center.Z - size),
                new Float3(-1, 0, 0), dirtTexture);
        }

        // Right face (+X) - only if not blocked
        if (gridX == gridWidth - 1 || heightMap[gridX + 1, gridZ] <= gridY)
        {
            AddQuad(triangles,
                new Float3(center.X + size, center.Y - size, center.Z + size),
                new Float3(center.X + size, center.Y - size, center.Z - size),
                new Float3(center.X + size, center.Y + size, center.Z - size),
                new Float3(center.X + size, center.Y + size, center.Z + size),
                new Float3(1, 0, 0), dirtTexture);
        }

        // Bottom face (-Y) - only render bottom layer
        if (gridY == 0)
        {
            AddQuad(triangles,
                new Float3(center.X - size, center.Y - size, center.Z - size),
                new Float3(center.X + size, center.Y - size, center.Z - size),
                new Float3(center.X + size, center.Y - size, center.Z + size),
                new Float3(center.X - size, center.Y - size, center.Z + size),
                new Float3(0, -1, 0), dirtTexture);
        }

        // Top face (+Y) - grass on top layer, dirt otherwise
        if (isTopLayer)
        {
            AddQuad(triangles,
                new Float3(center.X - size, center.Y + size, center.Z + size),
                new Float3(center.X + size, center.Y + size, center.Z + size),
                new Float3(center.X + size, center.Y + size, center.Z - size),
                new Float3(center.X - size, center.Y + size, center.Z - size),
                new Float3(0, 1, 0), grassTexture);
        }
    }

    private static void AddQuad(List<Triangle> triangles, Float3 v0, Float3 v1, Float3 v2, Float3 v3,
                                Float3 normal, Texture texture)
    {
        // First triangle
        triangles.Add(new Triangle(
            new Vertex(v0, normal, new Float2(0, 1)),
            new Vertex(v1, normal, new Float2(0, 0)),
            new Vertex(v2, normal, new Float2(1, 0)),
            texture
        ));

        // Second triangle
        triangles.Add(new Triangle(
            new Vertex(v0, normal, new Float2(0, 1)),
            new Vertex(v2, normal, new Float2(1, 0)),
            new Vertex(v3, normal, new Float2(1, 1)),
            texture
        ));
    }

    // Simple 2D simplex-like noise function
    private static float SimplexNoise(float x, float y)
    {
        // Simple implementation - using sin/cos for pseudo-random terrain
        float n = MathF.Sin(x * 1.5f) * MathF.Cos(y * 1.5f);
        n += MathF.Sin(x * 0.7f + y * 0.9f) * 0.5f;
        n += MathF.Cos(x * 2.1f - y * 1.3f) * 0.3f;
        return Math.Clamp(n, -1f, 1f);
    }
}
