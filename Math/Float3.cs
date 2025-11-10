using System.Runtime.InteropServices;

namespace RasterizerCube;

[StructLayout(LayoutKind.Sequential)]
public struct Float3
{
    public float X, Y, Z;

    public Float3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static Float3 operator +(Float3 a, Float3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Float3 operator -(Float3 a, Float3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Float3 operator *(Float3 a, float b) => new(a.X * b, a.Y * b, a.Z * b);
    public static Float3 operator /(Float3 a, float b) => new(a.X / b, a.Y / b, a.Z / b);

    public static Float3 Cross(Float3 a, Float3 b)
    {
        return new Float3(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X
        );
    }

    public static float Dot(Float3 a, Float3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    public float Length() => MathF.Sqrt(X * X + Y * Y + Z * Z);

    public Float3 Normalized()
    {
        float len = Length();
        return len > 0 ? this / len : new Float3(0, 0, 0);
    }

    public static Float3 Lerp(Float3 a, Float3 b, float t) => a + (b - a) * t;
}
