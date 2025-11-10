namespace RasterizerCube;

public class LightingConfig
{
    public Float3 PositionOffset { get; set; } = new Float3(0, 2.5f, 0);
    public Float3 LightColor { get; set; } = new Float3(1.0f, 0.85f, 0.6f);
    public float Range { get; set; } = 18f;
    public int Bands { get; set; } = 8;
    public float DitherRange { get; set; } = 0.5f;
    public float Ambient { get; set; } = 0.15f;
    public float Diffuse { get; set; } = 0.85f;
    public Float3 TorchPosition { get; set; } = new Float3(7, 4, 7);

    // Adaptive Lighting (Performance Optimization)
    public bool AdaptiveLightingEnabled { get; set; } = true;

    // Flickering
    public bool FlickeringEnabled { get; set; } = true;

    // Range Flickering
    public float RangeFlickerFrequency { get; set; } = 4.0f;
    public float RangeFlickerAmplitude { get; set; } = 0.2f;
    public float RangeFlickerNoiseIntensity { get; set; } = 0.4f;

    // Brightness Flickering
    public float BrightnessFlickerFrequency { get; set; } = 8.0f;
    public float BrightnessFlickerAmplitude { get; set; } = 0.15f;
    public float BrightnessFlickerNoiseIntensity { get; set; } = 0.3f;

    public static LightingConfig LoadFromFile(string filepath)
    {
        var config = new LightingConfig();

        if (!File.Exists(filepath))
        {
            Console.WriteLine($"Lighting config not found at {filepath}, using defaults");
            return config;
        }

        try
        {
            var yamlData = ParseYaml(File.ReadAllText(filepath));

            // Parse light section
            if (yamlData.TryGetValue("light", out var light))
            {
                if (light.TryGetValue("position_offset", out var posOffset))
                    config.PositionOffset = ParseFloat3Array(posOffset);
                if (light.TryGetValue("color", out var color))
                    config.LightColor = ParseFloat3Array(color);
                if (light.TryGetValue("range", out var range))
                    config.Range = float.Parse(range);
                if (light.TryGetValue("bands", out var bands))
                    config.Bands = int.Parse(bands);
                if (light.TryGetValue("dither_range", out var ditherRange))
                    config.DitherRange = float.Parse(ditherRange);
            }

            // Parse flickering section
            if (yamlData.TryGetValue("flickering", out var flickering))
            {
                if (flickering.TryGetValue("enabled", out var enabled))
                    config.FlickeringEnabled = bool.Parse(enabled);

                // Range flickering params
                if (flickering.TryGetValue("range_frequency", out var rangeFreq))
                    config.RangeFlickerFrequency = float.Parse(rangeFreq);
                if (flickering.TryGetValue("range_amplitude", out var rangeAmp))
                    config.RangeFlickerAmplitude = float.Parse(rangeAmp);
                if (flickering.TryGetValue("range_noise", out var rangeNoise))
                    config.RangeFlickerNoiseIntensity = float.Parse(rangeNoise);

                // Brightness flickering params
                if (flickering.TryGetValue("brightness_frequency", out var brightnessFreq))
                    config.BrightnessFlickerFrequency = float.Parse(brightnessFreq);
                if (flickering.TryGetValue("brightness_amplitude", out var brightnessAmp))
                    config.BrightnessFlickerAmplitude = float.Parse(brightnessAmp);
                if (flickering.TryGetValue("brightness_noise", out var brightnessNoise))
                    config.BrightnessFlickerNoiseIntensity = float.Parse(brightnessNoise);
            }

            // Parse lighting section
            if (yamlData.TryGetValue("lighting", out var lighting))
            {
                if (lighting.TryGetValue("ambient", out var ambient))
                    config.Ambient = float.Parse(ambient);
                if (lighting.TryGetValue("diffuse", out var diffuse))
                    config.Diffuse = float.Parse(diffuse);
            }

            // Parse performance section
            if (yamlData.TryGetValue("performance", out var performance))
            {
                if (performance.TryGetValue("adaptive_lighting", out var adaptiveLighting))
                    config.AdaptiveLightingEnabled = bool.Parse(adaptiveLighting);
            }

            // Parse torch section
            if (yamlData.TryGetValue("torch", out var torch))
            {
                if (torch.TryGetValue("position", out var position))
                    config.TorchPosition = ParseFloat3Array(position);
            }

            Console.WriteLine($"Loaded lighting config from {filepath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading lighting config: {ex.Message}");
        }

        return config;
    }

    private static Dictionary<string, Dictionary<string, string>> ParseYaml(string content)
    {
        var result = new Dictionary<string, Dictionary<string, string>>();
        Dictionary<string, string> currentSection = null;
        string currentSectionName = "";

        foreach (string line in content.Split('\n'))
        {
            string trimmed = line.Trim();

            // Skip comments and empty lines
            if (trimmed.StartsWith("#") || string.IsNullOrWhiteSpace(trimmed))
                continue;

            // Check if this is a top-level key (no indentation, ends with :)
            if (!line.StartsWith(" ") && trimmed.EndsWith(":"))
            {
                currentSectionName = trimmed.TrimEnd(':');
                currentSection = new Dictionary<string, string>();
                result[currentSectionName] = currentSection;
                continue;
            }

            // Parse indented key-value pairs
            if (currentSection != null && line.StartsWith("  ") && trimmed.Contains(":"))
            {
                int colonIndex = trimmed.IndexOf(':');
                if (colonIndex > 0)
                {
                    string key = trimmed.Substring(0, colonIndex).Trim();
                    string value = trimmed.Substring(colonIndex + 1).Trim();
                    currentSection[key] = value;
                }
            }
        }

        return result;
    }

    private static Float3 ParseFloat3Array(string value)
    {
        // Remove brackets if present
        value = value.Trim('[', ']', ' ');

        string[] parts = value.Split(',');
        if (parts.Length != 3)
            throw new FormatException($"Expected 3 values for Float3, got {parts.Length}");

        return new Float3(
            float.Parse(parts[0].Trim()),
            float.Parse(parts[1].Trim()),
            float.Parse(parts[2].Trim())
        );
    }

    public void PrintConfig()
    {
        Console.WriteLine("=== Lighting Configuration ===");
        Console.WriteLine($"Torch Position: {TorchPosition.X}, {TorchPosition.Y}, {TorchPosition.Z}");
        Console.WriteLine($"Light Offset: {PositionOffset.X}, {PositionOffset.Y}, {PositionOffset.Z}");
        Console.WriteLine($"Light Color: {LightColor.X}, {LightColor.Y}, {LightColor.Z}");
        Console.WriteLine($"Range: {Range}");
        Console.WriteLine($"Bands: {Bands}");
        Console.WriteLine($"Dither Range: {DitherRange}");
        Console.WriteLine($"Flickering: {FlickeringEnabled}");
        Console.WriteLine($"  Range: Freq={RangeFlickerFrequency}Hz, Amp={RangeFlickerAmplitude}, Noise={RangeFlickerNoiseIntensity}");
        Console.WriteLine($"  Brightness: Freq={BrightnessFlickerFrequency}Hz, Amp={BrightnessFlickerAmplitude}, Noise={BrightnessFlickerNoiseIntensity}");
        Console.WriteLine($"Ambient: {Ambient}");
        Console.WriteLine($"Diffuse: {Diffuse}");
        Console.WriteLine($"Adaptive Lighting: {AdaptiveLightingEnabled}");
        Console.WriteLine("==============================");
    }
}
