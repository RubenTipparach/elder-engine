using Raylib_cs;
using RasterizerCube;

namespace RasterizerCube;

class Program
{
    static void Main()
    {
        const int windowWidth = 1280;
        const int windowHeight = 720;

        // Load engine configuration
        string configPath = Path.Combine(AppContext.BaseDirectory, "config.yaml");
        var config = EngineConfig.LoadFromFile(configPath);
        config.PrintConfig();

        // Get render resolution from config
        int renderWidth = config.RenderWidth;
        int renderHeight = config.RenderHeight;

        Raylib.InitWindow(windowWidth, windowHeight, "Software Rasterizer - Cube with Grass & Dirt");
        Raylib.SetTargetFPS(60);

        // Create rasterizer at configured resolution
        var rasterizer = new Rasterizer(renderWidth, renderHeight);

        // Create GIF recorder for recording at 30 fps
        var gifEncoder = new GifRecorder(renderWidth, renderHeight, 30);
        string recordingMessage = "";
        float recordingMessageTime = 0f;
        int frameCounter = 0; // For skipping frames

        // Apply lighting config to rasterizer
        rasterizer.Ambient = config.Ambient;
        rasterizer.Diffuse = config.Diffuse;
        rasterizer.AdaptiveLightingEnabled = config.AdaptiveLightingEnabled;

        // Get the directory where the executable is located
        string exeDir = AppContext.BaseDirectory;
        string grassPath = Path.Combine(exeDir, "assets", "grass.png");
        string dirtPath = Path.Combine(exeDir, "assets", "dirt.png");

        // Create textures from files
        var grassTexture = Texture.LoadFromFile(grassPath);
        var dirtTexture = Texture.LoadFromFile(dirtPath);

        // Create procedural textures for torch
        var woodTexture = Texture.CreateSolid(new Float3(0.4f, 0.25f, 0.15f)); // Brown wood
        var fireTexture = Texture.CreateSolid(new Float3(1.0f, 0.6f, 0.2f)); // Orange fire

        // Generate terrain with 8x8 grid and 3 layers
        var terrainTriangles = TerrainGenerator.GenerateTerrain(grassTexture, dirtTexture, 16, 16, 4);

        // Create multiple torch positions
        List<Float3> torchPositions = new List<Float3>
        {
            new Float3(8, 2.5f, 8),    // Center torch (main light)
            new Float3(20, 3f, 20),    // Corner torch
            new Float3(12, 2.5f, 12),   // Corner torch
            new Float3(10, 2.5f, 12)    // Corner torch
        };

        // Create multiple point lights for each torch
        List<PointLight> pointLights = new List<PointLight>();
        foreach (var torchPos in torchPositions)
        {
            Float3 lightPosition = torchPos + config.PositionOffset;
            var light = new PointLight(
                lightPosition,
                config.LightColor,
                config.Range,
                config.Bands,
                config.DitherRange,
                config.FlickeringEnabled,
                config.RangeFlickerFrequency,
                config.RangeFlickerAmplitude,
                config.RangeFlickerNoiseIntensity,
                config.BrightnessFlickerFrequency,
                config.BrightnessFlickerAmplitude,
                config.BrightnessFlickerNoiseIntensity
            );
            pointLights.Add(light);
        }

        // Main light is the first one (moveable)
        var mainLight = pointLights[0];

        // Camera setup - position to see the terrain
        // Terrain center is at (7, 0, 7) with 8x8 grid
        Float3 cameraPos = new Float3(7, 8, -10);
        float cameraRotationY = 0f;
        float cameraRotationX = 0.3f;

        Console.WriteLine($"Terrain triangles: {terrainTriangles.Count}");

        // Create raylib image and texture for displaying the result
        unsafe
        {
            Image image = Raylib.GenImageColor(renderWidth, renderHeight, Color.Black);
            Raylib_cs.Texture2D displayTexture = Raylib.LoadTextureFromImage(image);
            Raylib.UnloadImage(image);

            float moveSpeed = 0.1f;

            while (!Raylib.WindowShouldClose())
            {
                // Update all lights flickering
                float deltaTime = Raylib.GetFrameTime();
                foreach (var light in pointLights)
                {
                    light.Update(deltaTime);
                }

                // Increment frame counter
                frameCounter++;

                // Update recording message timer
                if (recordingMessageTime > 0f)
                    recordingMessageTime -= deltaTime;

                // Handle GIF recording start (Ctrl+8)
                if (Raylib.IsKeyDown(KeyboardKey.LeftControl) && Raylib.IsKeyPressed(KeyboardKey.Eight))
                {
                    if (!gifEncoder.IsRecording)
                    {
                        gifEncoder.StartRecording();
                        recordingMessage = "GIF Recording Started";
                        recordingMessageTime = 2.0f;
                        Console.WriteLine("Started GIF recording");
                    }
                }

                // Handle GIF recording stop (Ctrl+9)
                if (Raylib.IsKeyDown(KeyboardKey.LeftControl) && Raylib.IsKeyPressed(KeyboardKey.Nine))
                {
                    if (gifEncoder.IsRecording)
                    {
                        gifEncoder.StopRecording();
                        recordingMessage = "GIF Recording Stopped";
                        recordingMessageTime = 2.0f;
                        Console.WriteLine($"Stopped GIF recording ({gifEncoder.FrameCount} frames)");

                        // Determine save path based on config
                        string gifPath;
                        if (config.UseCustomGifPath && !string.IsNullOrWhiteSpace(config.CustomGifPath))
                        {
                            gifPath = config.CustomGifPath;
                        }
                        else
                        {
                            // Default to desktop with timestamp
                            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                            gifPath = Path.Combine(desktopPath, $"render_{timestamp}.gif");
                        }

                        Console.WriteLine($"Saving GIF to {gifPath}...");
                        gifEncoder.SaveToFile(gifPath);
                    }
                }

                // Update camera rotation (Arrow keys)
                if (Raylib.IsKeyDown(KeyboardKey.Left))
                    cameraRotationY -= 0.02f;
                if (Raylib.IsKeyDown(KeyboardKey.Right))
                    cameraRotationY += 0.02f;
                if (Raylib.IsKeyDown(KeyboardKey.Up))
                    cameraRotationX -= 0.02f;
                if (Raylib.IsKeyDown(KeyboardKey.Down))
                    cameraRotationX += 0.02f;

                // Calculate camera forward and up vectors
                Float3 cameraForward = new Float3(
                    MathF.Sin(cameraRotationY) * MathF.Cos(cameraRotationX),
                    -MathF.Sin(cameraRotationX),
                    MathF.Cos(cameraRotationY) * MathF.Cos(cameraRotationX)
                ).Normalized();

                Float3 cameraUp = new Float3(0, 1, 0);
                Float3 cameraRight = Float3.Cross(cameraUp, cameraForward).Normalized();

                // Update camera position (WASD keys)
                if (Raylib.IsKeyDown(KeyboardKey.W))
                    cameraPos = cameraPos + cameraForward * moveSpeed;
                if (Raylib.IsKeyDown(KeyboardKey.S))
                    cameraPos = cameraPos - cameraForward * moveSpeed;
                if (Raylib.IsKeyDown(KeyboardKey.A))
                    cameraPos = cameraPos - cameraRight * moveSpeed;
                if (Raylib.IsKeyDown(KeyboardKey.D))
                    cameraPos = cameraPos + cameraRight * moveSpeed;
                if (Raylib.IsKeyDown(KeyboardKey.Space))
                    cameraPos = cameraPos + new Float3(0, moveSpeed, 0);
                if (Raylib.IsKeyDown(KeyboardKey.LeftShift))
                    cameraPos = cameraPos - new Float3(0, moveSpeed, 0);

                // Update main light position (IJKL keys)
                float lightMoveSpeed = 0.1f;
                if (Raylib.IsKeyDown(KeyboardKey.I))
                    mainLight.Position = mainLight.Position + new Float3(0, 0, lightMoveSpeed);
                if (Raylib.IsKeyDown(KeyboardKey.K))
                    mainLight.Position = mainLight.Position - new Float3(0, 0, lightMoveSpeed);
                if (Raylib.IsKeyDown(KeyboardKey.J))
                    mainLight.Position = mainLight.Position - new Float3(lightMoveSpeed, 0, 0);
                if (Raylib.IsKeyDown(KeyboardKey.L))
                    mainLight.Position = mainLight.Position + new Float3(lightMoveSpeed, 0, 0);
                if (Raylib.IsKeyDown(KeyboardKey.U))
                    mainLight.Position = mainLight.Position + new Float3(0, lightMoveSpeed, 0);
                if (Raylib.IsKeyDown(KeyboardKey.O))
                    mainLight.Position = mainLight.Position - new Float3(0, lightMoveSpeed, 0);

                // Clear the rasterizer
                rasterizer.Clear();

                // Rebuild scene with torches
                var sceneTriangles = new List<Triangle>(terrainTriangles);

                // Update main torch position to follow light (subtract offset to get torch base)
                torchPositions[0] = mainLight.Position - config.PositionOffset;

                // Update all light positions to match torches
                for (int i = 0; i < torchPositions.Count; i++)
                {
                    pointLights[i].Position = torchPositions[i] + config.PositionOffset;
                    var torchTriangles = TorchMesh.CreateTorch(torchPositions[i], woodTexture, fireTexture);
                    sceneTriangles.AddRange(torchTriangles);
                }

                // Render all triangles in parallel with multiple point lights
                rasterizer.DrawTriangles(sceneTriangles, cameraPos, cameraForward, cameraUp, pointLights);

                // Convert buffer to raylib texture
                Float3[] colorBuffer = rasterizer.GetColorBuffer();
                Color[] pixels = new Color[renderWidth * renderHeight];

                for (int i = 0; i < pixels.Length; i++)
                {
                    Float3 color = colorBuffer[i];
                    pixels[i] = new Color(
                        (int)(Math.Clamp(color.X, 0f, 1f) * 255),
                        (int)(Math.Clamp(color.Y, 0f, 1f) * 255),
                        (int)(Math.Clamp(color.Z, 0f, 1f) * 255),
                        255
                    );
                }

                // Capture frame if recording (30fps = every other frame at 60fps)
                if (gifEncoder.IsRecording && frameCounter % 2 == 0)
                {
                    byte[] rgbData = new byte[renderWidth * renderHeight * 3];
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        rgbData[i * 3] = pixels[i].R;
                        rgbData[i * 3 + 1] = pixels[i].G;
                        rgbData[i * 3 + 2] = pixels[i].B;
                    }

                    gifEncoder.AddFrame(rgbData);
                }

                // Update texture
                fixed (Color* pixelPtr = pixels)
                {
                    Raylib.UpdateTexture(displayTexture, pixelPtr);
                }

                // Draw - scale up to window size
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Black);

                // Draw texture scaled to fill window
                Rectangle srcRect = new Rectangle(0, 0, renderWidth, renderHeight);
                Rectangle dstRect = new Rectangle(0, 0, windowWidth, windowHeight);
                Raylib.DrawTexturePro(displayTexture, srcRect, dstRect, new System.Numerics.Vector2(0, 0), 0f, Color.White);

                Raylib.DrawFPS(10, 10);
                Raylib.DrawText($"Triangles: {rasterizer.RenderedTriangleCount}/{sceneTriangles.Count}", 10, 30, 20, Color.White);
                Raylib.DrawText($"Camera: ({cameraPos.X:F1}, {cameraPos.Y:F1}, {cameraPos.Z:F1})", 10, 50, 20, Color.White);
                Raylib.DrawText($"Main Light: ({mainLight.Position.X:F1}, {mainLight.Position.Y:F1}, {mainLight.Position.Z:F1}) | Lights: {pointLights.Count}", 10, 70, 20, Color.White);

                // Show recording message if active
                if (recordingMessageTime > 0f)
                {
                    Color messageColor = gifEncoder.IsRecording ? Color.Red : Color.Green;
                    Raylib.DrawText(recordingMessage, 10, 110, 24, messageColor);
                }

                // Show recording indicator
                if (gifEncoder.IsRecording)
                {
                    Raylib.DrawText($"● REC ({gifEncoder.FrameCount} frames)", windowWidth - 200, 10, 20, Color.Red);
                }

                Raylib.EndDrawing();
            }

            Raylib.UnloadTexture(displayTexture);
        }

        Raylib.CloseWindow();
    }
}
