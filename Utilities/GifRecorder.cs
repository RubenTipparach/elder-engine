using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Gif;

namespace RasterizerCube;

/// <summary>
/// Simple GIF recorder for recording gameplay using ImageSharp library
/// </summary>
public class GifRecorder
{
    private readonly List<Image<Rgb24>> frames = [];
    private readonly int width;
    private readonly int height;
    private readonly int delay; // Delay in milliseconds

    public bool IsRecording { get; private set; }
    public int FrameCount => frames.Count;

    public GifRecorder(int width, int height, int fps = 60)
    {
        this.width = width;
        this.height = height;
        // Convert FPS to milliseconds per frame
        this.delay = 1000 / fps;
    }

    public void StartRecording()
    {
        // Clear any existing frames
        foreach (var frame in frames)
        {
            frame.Dispose();
        }
        frames.Clear();
        IsRecording = true;
    }

    public void StopRecording()
    {
        IsRecording = false;
    }

    public void AddFrame(byte[] rgbData)
    {
        if (!IsRecording) return;

        // Create an ImageSharp image from RGB data
        var image = Image.LoadPixelData<Rgb24>(rgbData, width, height);
        frames.Add(image);
    }

    public void SaveToFile(string filepath)
    {
        if (frames.Count == 0)
        {
            Console.WriteLine("No frames to save!");
            return;
        }

        try
        {
            // Create the first frame as the base
            using (var gif = frames[0].Clone(ctx => { }))
            {
                var gifMetadata = gif.Metadata.GetGifMetadata();
                gifMetadata.RepeatCount = 0; // 0 = infinite loop
                gifMetadata.ColorTableMode = GifColorTableMode.Global; // Use global color table

                // Get the frame metadata for the first frame
                var firstFrameMetadata = gif.Frames.RootFrame.Metadata.GetGifMetadata();
                firstFrameMetadata.FrameDelay = delay / 10; // Convert milliseconds to centiseconds
                firstFrameMetadata.DisposalMethod = GifDisposalMethod.RestoreToBackground; // Clear frame before next

                // Add remaining frames
                for (int i = 1; i < frames.Count; i++)
                {
                    var frame = frames[i];
                    var frameToAdd = frame.Clone(ctx => { });

                    var frameMetadata = frameToAdd.Frames.RootFrame.Metadata.GetGifMetadata();
                    frameMetadata.FrameDelay = delay / 10; // Convert milliseconds to centiseconds
                    frameMetadata.DisposalMethod = GifDisposalMethod.RestoreToBackground; // Clear frame before next

                    gif.Frames.AddFrame(frameToAdd.Frames.RootFrame);
                }

                // Save with GIF encoder using global color table
                var encoder = new SixLabors.ImageSharp.Formats.Gif.GifEncoder
                {
                    ColorTableMode = GifColorTableMode.Global,
                    Quantizer = new SixLabors.ImageSharp.Processing.Processors.Quantization.OctreeQuantizer(new SixLabors.ImageSharp.Processing.Processors.Quantization.QuantizerOptions
                    {
                        MaxColors = 256,
                        // Use Floyd-Steinberg dithering to approximate colors not in the palette
                        // This helps create smooth gradients in the lighting despite the 256-color limit
                        Dither = null
                    })
                };

                gif.SaveAsGif(filepath, encoder);
            }

            Console.WriteLine($"Saved GIF with {frames.Count} frames to {filepath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving GIF: {ex.Message}");
        }
        finally
        {
            // Clean up frames after saving
            foreach (var frame in frames)
            {
                frame.Dispose();
            }
            frames.Clear();
        }
    }
}
