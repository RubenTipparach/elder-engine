using Raylib_cs;

namespace RasterizerCube;

public class Texture
{
    public int Width { get; }
    public int Height { get; }
    public Float3[] Pixels { get; }

    public Texture(int width, int height)
    {
        Width = width;
        Height = height;
        Pixels = new Float3[width * height];
    }

    public static unsafe Texture LoadFromFile(string filepath)
    {
        Image img = Raylib.LoadImage(filepath);

        if (img.Width == 0 || img.Height == 0)
        {
            Console.WriteLine($"Failed to load texture: {filepath}");
            // Return a fallback 2x2 checkerboard texture
            var fallback = new Texture(2, 2);
            fallback.Pixels[0] = new Float3(1, 0, 1);
            fallback.Pixels[1] = new Float3(0, 0, 0);
            fallback.Pixels[2] = new Float3(0, 0, 0);
            fallback.Pixels[3] = new Float3(1, 0, 1);
            return fallback;
        }

        // Convert to RGBA format if needed
        Raylib.ImageFormat(ref img, PixelFormat.UncompressedR8G8B8A8);

        var texture = new Texture(img.Width, img.Height);
        Color* pixels = (Color*)img.Data;

        for (int i = 0; i < img.Width * img.Height; i++)
        {
            texture.Pixels[i] = new Float3(
                pixels[i].R / 255f,
                pixels[i].G / 255f,
                pixels[i].B / 255f
            );
        }

        Raylib.UnloadImage(img);
        Console.WriteLine($"Loaded texture: {filepath} ({texture.Width}x{texture.Height})");
        return texture;
    }

    public Float3 Sample(Float2 uv)
    {
        uv.X = Math.Clamp(uv.X, 0f, 1f);
        uv.Y = Math.Clamp(uv.Y, 0f, 1f);

        int x = (int)(uv.X * (Width - 1));
        int y = (int)(uv.Y * (Height - 1));

        return Pixels[y * Width + x];
    }

    public static Texture CreateSolid(Float3 color)
    {
        var tex = new Texture(1, 1);
        tex.Pixels[0] = color;
        return tex;
    }

    public static Texture CreateGrassTexture()
    {
        var tex = new Texture(16, 16);
        var grassColor = new Float3(0.3f, 0.8f, 0.2f);
        var grassDark = new Float3(0.2f, 0.6f, 0.15f);

        for (int y = 0; y < tex.Height; y++)
        {
            for (int x = 0; x < tex.Width; x++)
            {
                bool isDark = (x + y) % 3 == 0;
                tex.Pixels[y * tex.Width + x] = isDark ? grassDark : grassColor;
            }
        }
        return tex;
    }

    public static Texture CreateDirtTexture()
    {
        var tex = new Texture(16, 16);
        var dirtColor = new Float3(0.4f, 0.3f, 0.2f);
        var dirtDark = new Float3(0.3f, 0.2f, 0.15f);

        for (int y = 0; y < tex.Height; y++)
        {
            for (int x = 0; x < tex.Width; x++)
            {
                bool isDark = (x * 3 + y * 7) % 5 < 2;
                tex.Pixels[y * tex.Width + x] = isDark ? dirtDark : dirtColor;
            }
        }
        return tex;
    }
}
