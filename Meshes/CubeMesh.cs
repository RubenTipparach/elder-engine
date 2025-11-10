namespace RasterizerCube;

public static class CubeMesh
{
    public static List<Triangle> CreateCube(Texture grassTexture, Texture dirtTexture)
    {
        var triangles = new List<Triangle>();

        // Front face (-Z) - facing toward -Z (camera looking from -Z sees this)
        AddQuad(triangles,
            new Float3(-1, -1, -1), new Float3(-1, 1, -1), new Float3(1, 1, -1), new Float3(1, -1, -1),
            new Float3(0, 0, -1), dirtTexture);

        // Back face (+Z) - facing toward +Z
        AddQuad(triangles,
            new Float3(1, -1, 1), new Float3(1, 1, 1), new Float3(-1, 1, 1), new Float3(-1, -1, 1),
            new Float3(0, 0, 1), dirtTexture);

        // Left face (-X) - facing toward -X
        AddQuad(triangles,
            new Float3(-1, -1, -1), new Float3(-1, -1, 1), new Float3(-1, 1, 1), new Float3(-1, 1, -1),
            new Float3(-1, 0, 0), dirtTexture);

        // Right face (+X) - facing toward +X
        AddQuad(triangles,
            new Float3(1, -1, 1), new Float3(1, -1, -1), new Float3(1, 1, -1), new Float3(1, 1, 1),
            new Float3(1, 0, 0), dirtTexture);

        // Bottom face (-Y) - facing toward -Y
        AddQuad(triangles,
            new Float3(-1, -1, -1), new Float3(1, -1, -1), new Float3(1, -1, 1), new Float3(-1, -1, 1),
            new Float3(0, -1, 0), dirtTexture);

        // Top face (+Y) - GRASS - facing toward +Y
        AddQuad(triangles,
            new Float3(-1, 1, 1), new Float3(1, 1, 1), new Float3(1, 1, -1), new Float3(-1, 1, -1),
            new Float3(0, 1, 0), grassTexture);

        return triangles;
    }

    private static void AddQuad(List<Triangle> triangles, Float3 v0, Float3 v1, Float3 v2, Float3 v3, Float3 normal, Texture texture)
    {
        // First triangle
        triangles.Add(new Triangle(
            new Vertex(v0, normal, new Float2(0, 1)),
            new Vertex(v1, normal, new Float2(1, 1)),
            new Vertex(v2, normal, new Float2(1, 0)),
            texture
        ));

        // Second triangle
        triangles.Add(new Triangle(
            new Vertex(v0, normal, new Float2(0, 1)),
            new Vertex(v2, normal, new Float2(1, 0)),
            new Vertex(v3, normal, new Float2(0, 0)),
            texture
        ));
    }
}
