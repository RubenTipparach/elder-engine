namespace RasterizerCube;

public class Rasterizer
{
    private int width;
    private int height;
    private Float3[] colorBuffer;
    private float[] depthBuffer;
    private object[] pixelLocks; // Thread-safe locks for parallel rendering
    private List<PointLight> currentLights; // Current lights for rendering
    private Float3 currentCameraPos; // Current camera position for lighting calculations
    private Octree octree; // Spatial partitioning for efficient light queries
    private List<Triangle> currentTriangles; // Current triangles for octree queries
    private List<PointLight>[] lightsPerTriangle; // Pre-computed lights affecting each triangle
    public float Ambient { get; set; } = 0.15f;
    public float Diffuse { get; set; } = 0.85f;
    public bool AdaptiveLightingEnabled { get; set; } = true;
    private int renderedTriangleCount = 0; // Count of triangles that made it through culling
    public int RenderedTriangleCount => renderedTriangleCount;

    public Rasterizer(int width, int height)
    {
        this.width = width;
        this.height = height;
        colorBuffer = new Float3[width * height];
        depthBuffer = new float[width * height];
        pixelLocks = new object[width * height];
        for (int i = 0; i < pixelLocks.Length; i++)
        {
            pixelLocks[i] = new object();
        }
        currentLights = new List<PointLight>();
        currentTriangles = new List<Triangle>();
    }

    public void Clear()
    {
        Array.Fill(colorBuffer, new Float3(0.1f, 0.1f, 0.15f));
        Array.Fill(depthBuffer, float.MaxValue);
        renderedTriangleCount = 0; // Reset triangle counter
    }

    public Float3[] GetColorBuffer() => colorBuffer;

    // Render all triangles in parallel for better performance with multiple lights
    public void DrawTriangles(List<Triangle> triangles, Float3 cameraPos, Float3 cameraForward, Float3 cameraUp, List<PointLight> lights)
    {
        currentLights = lights ?? new List<PointLight>();
        currentCameraPos = cameraPos;
        currentTriangles = triangles;

        // Pre-compute which lights affect each triangle using octree
        lightsPerTriangle = new List<PointLight>[triangles.Count];

        if (triangles.Count > 0 && currentLights.Count > 0)
        {
            octree = new Octree(new AABB(new Float3(-100, -100, -100), new Float3(100, 100, 100)));
            octree.Build(triangles);
            octree.UpdateLights(currentLights);

            // Pre-compute light lists for all triangles (done once per frame, not per pixel!)
            for (int i = 0; i < triangles.Count; i++)
            {
                lightsPerTriangle[i] = octree.GetLightsForTriangle(i);
            }
        }
        else
        {
            octree = null;
            // If no octree, all triangles get all lights
            for (int i = 0; i < triangles.Count; i++)
            {
                lightsPerTriangle[i] = currentLights;
            }
        }

        Float3 cameraRight = Float3.Cross(cameraUp, cameraForward).Normalized();
        Float3 cameraUpNorm = Float3.Cross(cameraForward, cameraRight).Normalized();

        Parallel.For(0, triangles.Count, i =>
        {
            DrawTriangleInternal(triangles[i], i, cameraPos, cameraRight, cameraUpNorm, cameraForward);
        });
    }

    // Backwards compatibility for single light
    public void DrawTriangles(List<Triangle> triangles, Float3 cameraPos, Float3 cameraForward, Float3 cameraUp, PointLight light = null)
    {
        var lights = light != null ? new List<PointLight> { light } : new List<PointLight>();
        DrawTriangles(triangles, cameraPos, cameraForward, cameraUp, lights);
    }

    public void DrawTriangle(Triangle tri, Float3 cameraPos, Float3 cameraForward, Float3 cameraUp)
    {
        Float3 cameraRight = Float3.Cross(cameraUp, cameraForward).Normalized();
        Float3 cameraUpNorm = Float3.Cross(cameraForward, cameraRight).Normalized();
        DrawTriangleInternal(tri, -1, cameraPos, cameraRight, cameraUpNorm, cameraForward);
    }

    private void DrawTriangleInternal(Triangle tri, int triangleIndex, Float3 cameraPos, Float3 cameraRight, Float3 cameraUp, Float3 cameraForward)
    {
        // Transform vertices to view space
        Vertex v0 = TransformVertex(tri.V0, cameraPos, cameraRight, cameraUp, cameraForward);
        Vertex v1 = TransformVertex(tri.V1, cameraPos, cameraRight, cameraUp, cameraForward);
        Vertex v2 = TransformVertex(tri.V2, cameraPos, cameraRight, cameraUp, cameraForward);

        const float nearClip = 0.1f;

        // Check which vertices are behind the near plane
        bool clip0 = v0.Position.Z < nearClip;
        bool clip1 = v1.Position.Z < nearClip;
        bool clip2 = v2.Position.Z < nearClip;
        int clipCount = (clip0 ? 1 : 0) + (clip1 ? 1 : 0) + (clip2 ? 1 : 0);

        // Handle clipping cases
        if (clipCount == 3)
        {
            // All vertices behind near plane - discard
            return;
        }

        // No frustum culling here - we'll clip in screen space instead
        // This handles partial visibility correctly
        if (clipCount == 0)
        {
            // No clipping needed - render normally
            RasterizeTriangle(tri.V0, tri.V1, tri.V2, v0, v1, v2, tri.Texture, triangleIndex);
        }
        else if (clipCount == 1)
        {
            // One vertex clipped - split into two triangles
            ClipOneVertex(tri.V0, tri.V1, tri.V2, v0, v1, v2, clip0, clip1, clip2, nearClip, tri.Texture, triangleIndex);
        }
        else if (clipCount == 2)
        {
            // Two vertices clipped - results in one triangle
            ClipTwoVertices(tri.V0, tri.V1, tri.V2, v0, v1, v2, clip0, clip1, clip2, nearClip, tri.Texture, triangleIndex);
        }
    }

    private void ClipOneVertex(Vertex w0, Vertex w1, Vertex w2, Vertex v0, Vertex v1, Vertex v2, bool clip0, bool clip1, bool clip2, float nearClip, Texture texture, int triangleIndex)
    {
        // Find which vertex is clipped
        int clipIndex = clip0 ? 0 : clip1 ? 1 : 2;
        int nextIndex = (clipIndex + 1) % 3;
        int prevIndex = (clipIndex + 2) % 3;

        Vertex vClip = clipIndex == 0 ? v0 : clipIndex == 1 ? v1 : v2;
        Vertex vNext = nextIndex == 0 ? v0 : nextIndex == 1 ? v1 : v2;
        Vertex vPrev = prevIndex == 0 ? v0 : prevIndex == 1 ? v1 : v2;

        Vertex wClip = clipIndex == 0 ? w0 : clipIndex == 1 ? w1 : w2;
        Vertex wNext = nextIndex == 0 ? w0 : nextIndex == 1 ? w1 : w2;
        Vertex wPrev = prevIndex == 0 ? w0 : prevIndex == 1 ? w1 : w2;

        // Calculate interpolation fractions where edges cross near plane
        float tNext = (nearClip - vClip.Position.Z) / (vNext.Position.Z - vClip.Position.Z);
        float tPrev = (nearClip - vClip.Position.Z) / (vPrev.Position.Z - vClip.Position.Z);

        // Create new vertices at clip plane (view space)
        Vertex clipNext = LerpVertex(vClip, vNext, tNext);
        Vertex clipPrev = LerpVertex(vClip, vPrev, tPrev);

        // Create new world vertices at clip plane
        Vertex wClipNext = LerpVertex(wClip, wNext, tNext);
        Vertex wClipPrev = LerpVertex(wClip, wPrev, tPrev);

        // Rasterize two new triangles
        RasterizeTriangle(wClipPrev, wClipNext, wNext, clipPrev, clipNext, vNext, texture, triangleIndex);
        RasterizeTriangle(wClipPrev, wNext, wPrev, clipPrev, vNext, vPrev, texture, triangleIndex);
    }

    private void ClipTwoVertices(Vertex w0, Vertex w1, Vertex w2, Vertex v0, Vertex v1, Vertex v2, bool clip0, bool clip1, bool clip2, float nearClip, Texture texture, int triangleIndex)
    {
        // Find which vertex is NOT clipped
        int keepIndex = !clip0 ? 0 : !clip1 ? 1 : 2;
        int nextIndex = (keepIndex + 1) % 3;
        int prevIndex = (keepIndex + 2) % 3;

        Vertex vKeep = keepIndex == 0 ? v0 : keepIndex == 1 ? v1 : v2;
        Vertex vNext = nextIndex == 0 ? v0 : nextIndex == 1 ? v1 : v2;
        Vertex vPrev = prevIndex == 0 ? v0 : prevIndex == 1 ? v1 : v2;

        Vertex wKeep = keepIndex == 0 ? w0 : keepIndex == 1 ? w1 : w2;
        Vertex wNext = nextIndex == 0 ? w0 : nextIndex == 1 ? w1 : w2;
        Vertex wPrev = prevIndex == 0 ? w0 : prevIndex == 1 ? w1 : w2;

        // Calculate interpolation fractions
        float tNext = (nearClip - vKeep.Position.Z) / (vNext.Position.Z - vKeep.Position.Z);
        float tPrev = (nearClip - vKeep.Position.Z) / (vPrev.Position.Z - vKeep.Position.Z);

        // Create new vertices at clip plane (view space)
        Vertex clipNext = LerpVertex(vKeep, vNext, tNext);
        Vertex clipPrev = LerpVertex(vKeep, vPrev, tPrev);

        // Create new world vertices at clip plane
        Vertex wClipNext = LerpVertex(wKeep, wNext, tNext);
        Vertex wClipPrev = LerpVertex(wKeep, wPrev, tPrev);

        // Rasterize one new triangle
        RasterizeTriangle(wClipPrev, wKeep, wClipNext, clipPrev, vKeep, clipNext, texture, triangleIndex);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private Vertex LerpVertex(Vertex a, Vertex b, float t)
    {
        return new Vertex(
            a.Position + (b.Position - a.Position) * t,
            a.Normal + (b.Normal - a.Normal) * t,
            a.TexCoord + (b.TexCoord - a.TexCoord) * t
        );
    }

    private void RasterizeTriangle(Vertex worldV0, Vertex worldV1, Vertex worldV2, Vertex v0, Vertex v1, Vertex v2, Texture texture, int triangleIndex)
    {
        // Backface culling in view space
        // Calculate face normal from edges
        Float3 edge1 = v1.Position - v0.Position;
        Float3 edge2 = v2.Position - v0.Position;
        Float3 faceNormal = Float3.Cross(edge1, edge2).Normalized();

        // In view space, camera is at origin looking down -Z
        // View direction from triangle center to camera is -triCenter
        Float3 triCenter = (v0.Position + v1.Position + v2.Position) / 3.0f;
        Float3 viewDir = (new Float3(0, 0, 0) - triCenter).Normalized(); // From triangle to camera

        // If face normal points away from camera, cull it
        // Dot product < 0 means normal points opposite to view direction (backface)
        if (Float3.Dot(faceNormal, viewDir) < 0)
            return;

        // Triangle passed culling, increment counter
        System.Threading.Interlocked.Increment(ref renderedTriangleCount);

        // Project to screen space
        Float2 p0 = ProjectToScreen(v0.Position);
        Float2 p1 = ProjectToScreen(v1.Position);
        Float2 p2 = ProjectToScreen(v2.Position);

        // Screen-space frustum culling - early rejection if completely off-screen
        float minScreenX = MathF.Min(MathF.Min(p0.X, p1.X), p2.X);
        float maxScreenX = MathF.Max(MathF.Max(p0.X, p1.X), p2.X);
        float minScreenY = MathF.Min(MathF.Min(p0.Y, p1.Y), p2.Y);
        float maxScreenY = MathF.Max(MathF.Max(p0.Y, p1.Y), p2.Y);

        // Reject if completely outside screen bounds
        if (maxScreenX < 0 || minScreenX >= width || maxScreenY < 0 || minScreenY >= height)
        {
            return; // Triangle completely off-screen
        }

        // Calculate screen-space triangle area (in pixels)
        float screenArea = MathF.Abs(
            (p1.X - p0.X) * (p2.Y - p0.Y) -
            (p2.X - p0.X) * (p1.Y - p0.Y)
        ) * 0.5f;

        // Calculate screen coverage percentage
        float totalScreenPixels = width * height;
        float coveragePercent = screenArea / totalScreenPixels;

        // Determine lighting sample grid size based on screen coverage
        int lightingSampleSize = 1; // Default: per-pixel lighting

        if (AdaptiveLightingEnabled)
        {
            if (coveragePercent > 0.80f)
                lightingSampleSize = 32; // 32x32 groups for very large triangles
            else if (coveragePercent > 0.50f)
                lightingSampleSize = 24; // 24x24 groups
            else if (coveragePercent > 0.30f)
                lightingSampleSize = 16; // 16x16 groups
            else if (coveragePercent > 0.15f)
                lightingSampleSize = 8;  // 8x8 groups
            else if (coveragePercent > 0.05f)
                lightingSampleSize = 4;  // 4x4 groups for moderate triangles
        }

        // Calculate bounding box
        int minX = (int)Math.Max(0, Math.Min(Math.Min(p0.X, p1.X), p2.X));
        int maxX = (int)Math.Min(width - 1, Math.Max(Math.Max(p0.X, p1.X), p2.X));
        int minY = (int)Math.Max(0, Math.Min(Math.Min(p0.Y, p1.Y), p2.Y));
        int maxY = (int)Math.Min(height - 1, Math.Max(Math.Max(p0.Y, p1.Y), p2.Y));

        // Pre-compute inverse depths (optimization)
        float invZ0 = 1.0f / v0.Position.Z;
        float invZ1 = 1.0f / v1.Position.Z;
        float invZ2 = 1.0f / v2.Position.Z;

        // Pre-multiply texture coordinates by inverse depth
        Float2 texU = v0.TexCoord * invZ0;
        Float2 texV = v1.TexCoord * invZ1;
        Float2 texW = v2.TexCoord * invZ2;

        // Pre-multiply world positions by inverse depth for perspective-correct interpolation
        Float3 worldU = worldV0.Position * invZ0;
        Float3 worldV = worldV1.Position * invZ1;
        Float3 worldW = worldV2.Position * invZ2;

        // Pre-multiply normals by inverse depth for perspective-correct interpolation
        Float3 normalU = worldV0.Normal * invZ0;
        Float3 normalV = worldV1.Normal * invZ1;
        Float3 normalW = worldV2.Normal * invZ2;

        // Cache for lighting calculations (only used when lightingSampleSize > 1)
        Dictionary<(int, int), Float3> lightingCache = lightingSampleSize > 1
            ? new Dictionary<(int, int), Float3>()
            : null;

        // Rasterize
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Float2 p = new Float2(x + 0.5f, y + 0.5f);

                if (PointInTriangle(p0, p1, p2, p, out float w0, out float w1, out float w2))
                {
                    // Interpolate depth
                    float depth = 1.0f / (invZ0 * w0 + invZ1 * w1 + invZ2 * w2);

                    int index = y * width + x;

                    // Thread-safe depth test and pixel write
                    lock (pixelLocks[index])
                    {
                        if (depth > 0 && depth < depthBuffer[index])
                        {
                            depthBuffer[index] = depth;

                            // Perspective-correct texture coordinates
                            Float2 texCoord = (texU * w0 + texV * w1 + texW * w2) * depth;

                            // Perspective-correct world position
                            Float3 worldPos = (worldU * w0 + worldV * w1 + worldW * w2) * depth;

                            // Perspective-correct normal
                            Float3 normal = ((normalU * w0 + normalV * w1 + normalW * w2) * depth).Normalized();

                            // Sample texture
                            Float3 color = texture.Sample(texCoord);

                            // Apply lighting if available
                            if (currentLights.Count > 0)
                            {
                                Float3 finalLight;

                                if (lightingSampleSize > 1)
                                {
                                    // Use adaptive sampling - calculate lighting for pixel group
                                    int gridX = (x / lightingSampleSize) * lightingSampleSize + lightingSampleSize / 2;
                                    int gridY = (y / lightingSampleSize) * lightingSampleSize + lightingSampleSize / 2;
                                    var cacheKey = (gridX, gridY);

                                    if (!lightingCache.TryGetValue(cacheKey, out finalLight))
                                    {
                                        // Calculate lighting for the center of this grid cell
                                        Float2 gridP = new Float2(gridX + 0.5f, gridY + 0.5f);

                                        if (PointInTriangle(p0, p1, p2, gridP, out float gw0, out float gw1, out float gw2))
                                        {
                                            float gridDepth = 1.0f / (invZ0 * gw0 + invZ1 * gw1 + invZ2 * gw2);
                                            Float3 gridWorldPos = (worldU * gw0 + worldV * gw1 + worldW * gw2) * gridDepth;
                                            Float3 gridNormal = ((normalU * gw0 + normalV * gw1 + normalW * gw2) * gridDepth).Normalized();

                                            finalLight = CalculateLighting(gridWorldPos, gridNormal, gridX, gridY, triangleIndex);
                                            lightingCache[cacheKey] = finalLight;
                                        }
                                        else
                                        {
                                            // Grid center not in triangle, calculate for current pixel
                                            finalLight = CalculateLighting(worldPos, normal, x, y, triangleIndex);
                                        }
                                    }
                                }
                                else
                                {
                                    // Per-pixel lighting (default for small triangles)
                                    finalLight = CalculateLighting(worldPos, normal, x, y, triangleIndex);
                                }

                                color = new Float3(
                                    color.X * finalLight.X,
                                    color.Y * finalLight.Y,
                                    color.Z * finalLight.Z
                                );
                            }

                            colorBuffer[index] = color;
                        }
                    }
                }
            }
        }
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private Float3 CalculateLighting(Float3 worldPos, Float3 normal, int x, int y, int triangleIndex)
    {
        // Get pre-computed lights for this triangle (computed once per frame, not per pixel!)
        List<PointLight> affectingLights = (triangleIndex >= 0 && triangleIndex < lightsPerTriangle.Length)
            ? lightsPerTriangle[triangleIndex]
            : currentLights;

        // Accumulate light from all affecting lights
        Float3 totalLightContribution = new Float3(0, 0, 0);

        foreach (var light in affectingLights)
        {
            // Early distance rejection - avoid expensive calculations if out of range
            Float3 toLight = light.Position - worldPos;
            float distSq = toLight.X * toLight.X + toLight.Y * toLight.Y + toLight.Z * toLight.Z;
            float rangeSq = light.Range * light.Range;

            if (distSq > rangeSq)
                continue; // Light doesn't reach this pixel

            // Get dithered intensity using pre-calculated distance (avoids redundant sqrt)
            // Brightness flickering is already applied inside
            float intensity = light.GetDitheredIntensityFromDistSq(distSq, x, y);

            if (intensity > 0)
            {
                // Calculate diffuse lighting with normal
                Float3 lightDir = toLight.Normalized();
                float diffuseFactor = Math.Max(0f, normal.X * lightDir.X + normal.Y * lightDir.Y + normal.Z * lightDir.Z);

                // Diffuse lighting contribution from this light
                Float3 lightContribution = light.Color * (intensity * diffuseFactor * Diffuse);
                totalLightContribution = totalLightContribution + lightContribution;
            }
        }

        // Apply ambient + accumulated light
        return new Float3(
            Math.Clamp(Ambient + totalLightContribution.X, 0f, 1f),
            Math.Clamp(Ambient + totalLightContribution.Y, 0f, 1f),
            Math.Clamp(Ambient + totalLightContribution.Z, 0f, 1f)
        );
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private Vertex TransformVertex(Vertex v, Float3 camPos, Float3 camRight, Float3 camUp, Float3 camForward)
    {
        Float3 worldPos = v.Position;
        Float3 relativePos = worldPos - camPos;

        // Transform to view space
        Float3 viewPos = new Float3(
            Float3.Dot(relativePos, camRight),
            Float3.Dot(relativePos, camUp),
            Float3.Dot(relativePos, camForward)
        );

        return new Vertex(viewPos, v.Normal, v.TexCoord);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private Float2 ProjectToScreen(Float3 viewPos)
    {
        // Simple perspective projection
        float fov = 60f * MathF.PI / 180f;
        float aspectRatio = (float)width / height;

        if (viewPos.Z <= 0.1f) viewPos.Z = 0.1f;

        float projScale = 1f / MathF.Tan(fov / 2f);
        float x = viewPos.X * projScale / (aspectRatio * viewPos.Z);
        float y = viewPos.Y * projScale / viewPos.Z;

        // NDC to screen space
        float screenX = (x + 1f) * 0.5f * width;
        float screenY = (1f - y) * 0.5f * height;

        return new Float2(screenX, screenY);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private bool PointInTriangle(Float2 p0, Float2 p1, Float2 p2, Float2 p, out float w0, out float w1, out float w2)
    {
        Float2 v0 = p1 - p0;
        Float2 v1 = p2 - p0;
        Float2 v2 = p - p0;

        float d00 = v0.X * v0.X + v0.Y * v0.Y;
        float d01 = v0.X * v1.X + v0.Y * v1.Y;
        float d11 = v1.X * v1.X + v1.Y * v1.Y;
        float d20 = v2.X * v0.X + v2.Y * v0.Y;
        float d21 = v2.X * v1.X + v2.Y * v1.Y;

        float denom = d00 * d11 - d01 * d01;
        if (Math.Abs(denom) < 0.0001f)
        {
            w0 = w1 = w2 = 0;
            return false;
        }

        w1 = (d11 * d20 - d01 * d21) / denom;
        w2 = (d00 * d21 - d01 * d20) / denom;
        w0 = 1f - w1 - w2;

        return w0 >= 0 && w1 >= 0 && w2 >= 0;
    }
}
