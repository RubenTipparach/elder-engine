namespace RasterizerCube;

public static class TorchMesh
{
    public static List<Triangle> CreateTorch(Float3 position, Texture woodTexture, Texture fireTexture)
    {
        var triangles = new List<Triangle>();

        // Stick (tall thin octagonal prism for better roundness)
        float stickRadius = 0.12f;
        float stickHeight = 2.2f;
        int stickSides = 8;
        Float3 stickBase = position;
        Float3 stickTop = position + new Float3(0, stickHeight, 0);

        // Create octagonal stick
        for (int i = 0; i < stickSides; i++)
        {
            float angle1 = (float)(i * 2 * Math.PI / stickSides);
            float angle2 = (float)((i + 1) * 2 * Math.PI / stickSides);

            Float3 p1Bottom = stickBase + new Float3(MathF.Cos(angle1) * stickRadius, 0, MathF.Sin(angle1) * stickRadius);
            Float3 p2Bottom = stickBase + new Float3(MathF.Cos(angle2) * stickRadius, 0, MathF.Sin(angle2) * stickRadius);
            Float3 p1Top = stickTop + new Float3(MathF.Cos(angle1) * stickRadius, 0, MathF.Sin(angle1) * stickRadius);
            Float3 p2Top = stickTop + new Float3(MathF.Cos(angle2) * stickRadius, 0, MathF.Sin(angle2) * stickRadius);

            Float3 normal = new Float3(MathF.Cos(angle1 + MathF.PI / stickSides), 0, MathF.Sin(angle1 + MathF.PI / stickSides));

            // Side face
            AddQuad(triangles, p1Bottom, p1Top, p2Top, p2Bottom, normal, woodTexture);
        }

        // Fire - Create a more interesting flame shape using multiple layers
        float fireBaseSize = 0.25f;
        float fireMidSize = 0.22f;
        float fireTipSize = 0.12f;

        Float3 fireBase = stickTop + new Float3(0, 0.15f, 0);
        Float3 fireMid = stickTop + new Float3(0, 0.35f, 0);
        Float3 fireTip = stickTop + new Float3(0, 0.6f, 0);

        // Create tapered flame using octagon at three heights
        int flameSides = 6;
        for (int i = 0; i < flameSides; i++)
        {
            float angle1 = (float)(i * 2 * Math.PI / flameSides);
            float angle2 = (float)((i + 1) * 2 * Math.PI / flameSides);

            // Bottom to middle
            Float3 p1Base = fireBase + new Float3(MathF.Cos(angle1) * fireBaseSize, 0, MathF.Sin(angle1) * fireBaseSize);
            Float3 p2Base = fireBase + new Float3(MathF.Cos(angle2) * fireBaseSize, 0, MathF.Sin(angle2) * fireBaseSize);
            Float3 p1Mid = fireMid + new Float3(MathF.Cos(angle1) * fireMidSize, 0, MathF.Sin(angle1) * fireMidSize);
            Float3 p2Mid = fireMid + new Float3(MathF.Cos(angle2) * fireMidSize, 0, MathF.Sin(angle2) * fireMidSize);

            Float3 normal1 = new Float3(MathF.Cos(angle1 + MathF.PI / flameSides), 0, MathF.Sin(angle1 + MathF.PI / flameSides));
            AddQuad(triangles, p1Base, p1Mid, p2Mid, p2Base, normal1, fireTexture);

            // Middle to tip
            Float3 p1Tip = fireTip + new Float3(MathF.Cos(angle1) * fireTipSize, 0, MathF.Sin(angle1) * fireTipSize);
            Float3 p2Tip = fireTip + new Float3(MathF.Cos(angle2) * fireTipSize, 0, MathF.Sin(angle2) * fireTipSize);

            Float3 normal2 = new Float3(MathF.Cos(angle1 + MathF.PI / flameSides), 0.3f, MathF.Sin(angle1 + MathF.PI / flameSides)).Normalized();
            AddQuad(triangles, p1Mid, p1Tip, p2Tip, p2Mid, normal2, fireTexture);
        }

        return triangles;
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
}
