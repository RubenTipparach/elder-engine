namespace RasterizerCube;

public class PointLight
{
    public Float3 Position { get; set; }
    public Float3 Color { get; set; }
    public float Range { get; set; }
    public int Bands { get; set; }
    public float DitherRange { get; set; }

    // Flickering
    public bool FlickeringEnabled { get; set; }

    // Range Flickering
    public float RangeFlickerFrequency { get; set; }
    public float RangeFlickerAmplitude { get; set; }
    public float RangeFlickerNoiseIntensity { get; set; }
    private float rangeFlickerTime = 0f;
    private float rangeNoiseValue = 0f;
    private float rangeNoiseChangeTimer = 0f;
    private Random rangeFlickerRandom = new Random();
    private float baseRange; // Store the original range for flickering

    // Brightness Flickering
    public float BrightnessFlickerFrequency { get; set; }
    public float BrightnessFlickerAmplitude { get; set; }
    public float BrightnessFlickerNoiseIntensity { get; set; }
    private float brightnessFlickerTime = 0f;
    private float brightnessNoiseValue = 0f;
    private float brightnessNoiseChangeTimer = 0f;
    private Random brightnessFlickerRandom = new Random(42); // Different seed for variety

    public PointLight(Float3 position, Float3 color, float range, int bands = 8, float ditherRange = 0.5f,
                      bool flickeringEnabled = true,
                      float rangeFlickerFrequency = 4.0f, float rangeFlickerAmplitude = 0.2f, float rangeFlickerNoiseIntensity = 0.4f,
                      float brightnessFlickerFrequency = 8.0f, float brightnessFlickerAmplitude = 0.15f, float brightnessFlickerNoiseIntensity = 0.3f)
    {
        Position = position;
        Color = color;
        Range = range;
        baseRange = range; // Store original range
        Bands = bands;
        DitherRange = Math.Clamp(ditherRange, 0f, 1f);

        // Flickering
        FlickeringEnabled = flickeringEnabled;

        // Range flickering
        RangeFlickerFrequency = rangeFlickerFrequency;
        RangeFlickerAmplitude = rangeFlickerAmplitude;
        RangeFlickerNoiseIntensity = rangeFlickerNoiseIntensity;

        // Brightness flickering
        BrightnessFlickerFrequency = brightnessFlickerFrequency;
        BrightnessFlickerAmplitude = brightnessFlickerAmplitude;
        BrightnessFlickerNoiseIntensity = brightnessFlickerNoiseIntensity;
    }

    public void Update(float deltaTime)
    {
        if (FlickeringEnabled)
        {
            rangeFlickerTime += deltaTime;
            brightnessFlickerTime += deltaTime;

            // Update noise values at a slower rate (10 times per second) to avoid jitter at low frequencies
            const float noiseUpdateRate = 0.1f;

            rangeNoiseChangeTimer += deltaTime;
            if (rangeNoiseChangeTimer >= noiseUpdateRate)
            {
                rangeNoiseValue = ((float)rangeFlickerRandom.NextDouble() * 2f - 1f);
                rangeNoiseChangeTimer = 0f;
            }

            brightnessNoiseChangeTimer += deltaTime;
            if (brightnessNoiseChangeTimer >= noiseUpdateRate)
            {
                brightnessNoiseValue = ((float)brightnessFlickerRandom.NextDouble() * 2f - 1f);
                brightnessNoiseChangeTimer = 0f;
            }

            // Update range with flickering
            float rangeMultiplier = GetRangeFlickerMultiplier();
            Range = baseRange * rangeMultiplier;
        }
        else
        {
            Range = baseRange; // Reset to base range when not flickering
        }
    }

    public float GetRangeFlickerMultiplier()
    {
        if (!FlickeringEnabled)
            return 1.0f;

        // Sine wave base
        float sine = MathF.Sin(rangeFlickerTime * RangeFlickerFrequency * MathF.PI * 2f);

        // Use sampled noise value instead of generating new one every call
        float noise = rangeNoiseValue * RangeFlickerNoiseIntensity;

        // Combine sine and noise, then scale by amplitude
        float flicker = (sine + noise) * RangeFlickerAmplitude;

        // Return multiplier (1.0 +/- amplitude)
        return Math.Clamp(1.0f + flicker, 0.1f, 1.5f);
    }

    public float GetBrightnessFlickerMultiplier()
    {
        if (!FlickeringEnabled)
            return 1.0f;

        // Sine wave base
        float sine = MathF.Sin(brightnessFlickerTime * BrightnessFlickerFrequency * MathF.PI * 2f);

        // Use sampled noise value instead of generating new one every call
        float noise = brightnessNoiseValue * BrightnessFlickerNoiseIntensity;

        // Combine sine and noise, then scale by amplitude
        float flicker = (sine + noise) * BrightnessFlickerAmplitude;

        // Return multiplier (1.0 +/- amplitude)
        return Math.Clamp(1.0f + flicker, 0.1f, 1.5f);
    }

    // Calculate light intensity at a world position with banding
    public float GetIntensity(Float3 worldPos)
    {
        Float3 toLight = Position - worldPos;
        float distance = MathF.Sqrt(toLight.X * toLight.X + toLight.Y * toLight.Y + toLight.Z * toLight.Z);

        if (distance > Range)
            return 0f;

        // Calculate raw intensity (inverse square falloff, clamped)
        float rawIntensity = 1f - (distance / Range);
        rawIntensity = Math.Clamp(rawIntensity, 0f, 1f);

        // Apply banding - quantize to discrete levels
        float bandedIntensity = MathF.Floor(rawIntensity * Bands) / Bands;

        return bandedIntensity;
    }

    // Get light direction from world position
    public Float3 GetDirection(Float3 worldPos)
    {
        Float3 toLight = Position - worldPos;
        return toLight.Normalized();
    }

    // Get raw (unquantized) intensity for dithering
    public float GetRawIntensity(Float3 worldPos)
    {
        Float3 toLight = Position - worldPos;
        float distance = MathF.Sqrt(toLight.X * toLight.X + toLight.Y * toLight.Y + toLight.Z * toLight.Z);

        if (distance > Range)
            return 0f;

        float rawIntensity = 1f - (distance / Range);
        rawIntensity = Math.Clamp(rawIntensity, 0f, 1f);

        // Apply brightness flickering
        rawIntensity *= GetBrightnessFlickerMultiplier();

        return Math.Clamp(rawIntensity, 0f, 1f);
    }

    // Get dithered intensity - dithers at the START of each band
    // Pattern: 1.0 1.0 1.0 ... (dither: 1.0/0.9) 0.9 0.9 0.9 ... (dither: 0.9/0.8) 0.8 0.8 0.8
    public float GetDitheredIntensity(Float3 worldPos, int pixelX, int pixelY)
    {
        float rawIntensity = GetRawIntensity(worldPos);

        if (rawIntensity <= 0f)
            return 0f;

        // Calculate which band we're in and the fractional position
        float bandValue = rawIntensity * Bands;
        int currentBand = (int)MathF.Floor(bandValue);
        float fractionalPart = bandValue - currentBand;

        // Bayer matrix for dithering pattern
        int[,] bayerMatrix = new int[4, 4] {
            { 0, 8, 2, 10 },
            { 12, 4, 14, 6 },
            { 3, 11, 1, 9 },
            { 15, 7, 13, 5 }
        };

        int bayerValue = bayerMatrix[pixelY % 4, pixelX % 4];
        float bayerThreshold = bayerValue / 16.0f;

        // DitherRange controls the size of the transition zone at the START of each band
        // Check if we're at the START of the band (transitioning from previous brighter band to current)
        bool atBandStart = fractionalPart < DitherRange;

        if (atBandStart)
        {
            // At the START of currentBand - dither between previous brighter band and current band
            // transitionProgress: 0.0 at start (show more brighter), 1.0 at end (show more current)
            float transitionProgress = fractionalPart / DitherRange;

            // Previous band is brighter (higher value when divided), current band is darker
            int brighterBand = currentBand - 1;
            int darkerBand = currentBand ;

            // Use Bayer threshold to decide which band to show for this pixel
            int finalBand = (transitionProgress > bayerThreshold) ? darkerBand : brighterBand;
            finalBand = Math.Clamp(finalBand, 0, Bands);

            return finalBand / (float)Bands;
        }
        else
        {
            // Not in transition zone - use solid band color
            return currentBand / (float)Bands;
        }
    }
}
