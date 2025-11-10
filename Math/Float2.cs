using System.Runtime.InteropServices;

namespace RasterizerCube;

[StructLayout(LayoutKind.Sequential)]
public struct Float2
{
    public float X, Y;

    public Float2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public static Float2 operator +(Float2 a, Float2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Float2 operator -(Float2 a, Float2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Float2 operator *(Float2 a, float b) => new(a.X * b, a.Y * b);
    public static Float2 operator /(Float2 a, float b) => new(a.X / b, a.Y / b);

    public static Float2 Lerp(Float2 a, Float2 b, float t) => a + (b - a) * t;
}
