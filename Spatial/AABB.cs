namespace RasterizerCube;

public struct AABB
{
    public Float3 Min;
    public Float3 Max;

    public AABB(Float3 min, Float3 max)
    {
        Min = min;
        Max = max;
    }

    public Float3 Center => (Min + Max) * 0.5f;
    public Float3 Size => Max - Min;

    public bool Intersects(AABB other)
    {
        return Min.X <= other.Max.X && Max.X >= other.Min.X &&
               Min.Y <= other.Max.Y && Max.Y >= other.Min.Y &&
               Min.Z <= other.Max.Z && Max.Z >= other.Min.Z;
    }

    public bool Contains(Float3 point)
    {
        return point.X >= Min.X && point.X <= Max.X &&
               point.Y >= Min.Y && point.Y <= Max.Y &&
               point.Z >= Min.Z && point.Z <= Max.Z;
    }

    public static AABB FromTriangle(Triangle triangle)
    {
        Float3 min = new Float3(
            MathF.Min(MathF.Min(triangle.V0.Position.X, triangle.V1.Position.X), triangle.V2.Position.X),
            MathF.Min(MathF.Min(triangle.V0.Position.Y, triangle.V1.Position.Y), triangle.V2.Position.Y),
            MathF.Min(MathF.Min(triangle.V0.Position.Z, triangle.V1.Position.Z), triangle.V2.Position.Z)
        );

        Float3 max = new Float3(
            MathF.Max(MathF.Max(triangle.V0.Position.X, triangle.V1.Position.X), triangle.V2.Position.X),
            MathF.Max(MathF.Max(triangle.V0.Position.Y, triangle.V1.Position.Y), triangle.V2.Position.Y),
            MathF.Max(MathF.Max(triangle.V0.Position.Z, triangle.V1.Position.Z), triangle.V2.Position.Z)
        );

        return new AABB(min, max);
    }

    public static AABB FromLight(PointLight light)
    {
        Float3 min = light.Position - new Float3(light.Range, light.Range, light.Range);
        Float3 max = light.Position + new Float3(light.Range, light.Range, light.Range);
        return new AABB(min, max);
    }

    public static AABB Combine(AABB a, AABB b)
    {
        return new AABB(
            new Float3(
                MathF.Min(a.Min.X, b.Min.X),
                MathF.Min(a.Min.Y, b.Min.Y),
                MathF.Min(a.Min.Z, b.Min.Z)
            ),
            new Float3(
                MathF.Max(a.Max.X, b.Max.X),
                MathF.Max(a.Max.Y, b.Max.Y),
                MathF.Max(a.Max.Z, b.Max.Z)
            )
        );
    }
}
