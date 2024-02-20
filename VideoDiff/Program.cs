using OpenCvSharp;
using Size = OpenCvSharp.Size;

namespace VideoDiff;

internal static class Program
{
    private static void Main(string[] args)
    {
        var controlFramePath = args.Length > 0 ? args[0] : "control.png";
        var samplePath = args.Length > 1 ? args[1] : "sample.mp4";
        var outputPath = args.Length > 2 ? args[2] : "output.mp4";

        
        using var controlFrame = new Mat(controlFramePath);
        
        using var inputVideo = new VideoCapture(samplePath);
        var frameSize = new Size(inputVideo.FrameWidth, inputVideo.FrameHeight);
        var totalFrames = inputVideo.FrameCount;

        using var outputVideo = new VideoWriter(outputPath, FourCC.Default, inputVideo.Fps, frameSize);
        
        using var frame = new Mat();

        var pixels = new int[frameSize.Height, frameSize.Width];
        var frameDiffs = new float[totalFrames];

        var totalAvg = 0d;

        while (inputVideo.Read(frame))
        {
            var size = frame.Size();

            var frameAvg = 0f;
            
            for (var x = 0; x < size.Width; x++)
            {
                var colAvg = 0f;
                
                for (var y = 0; y < size.Height; y++)
                {
                    var pixel1 = frame.Get<int>(y, x);
                    var pixel2 = controlFrame.Get<int>(y, x);
                    
                    var r1 = (pixel1 & 0xFF0000) >> 16;
                    var g1 = (pixel1 & 0x00FF00) >> 8;
                    var b1 = (pixel1 & 0x0000FF) >> 0;
                    
                    var r2 = (pixel2 & 0xFF0000) >> 16;
                    var g2 = (pixel2 & 0x00FF00) >> 8;
                    var b2 = (pixel2 & 0x0000FF) >> 0;
                    
                    var r = r1 > r2 ? r1 - r2 : r2 - r1;
                    var g = g1 > g2 ? g1 - g2 : g2 - g1;
                    var b = b1 > b2 ? b1 - b2 : b2 - b1;
                    
                    pixels[y, x] = (r << 16) | (g << 8) | b;

                    var avg = ((float)r + g + b) / 3;
                    
                    colAvg += avg;
                }

                frameAvg += colAvg / size.Height;
            }
            var frameDiff = frameAvg / size.Width;
            totalAvg += frameDiff;
            
            outputVideo.Write(InputArray.Create(pixels, MatType.CV_8UC4));
            
            var pos = inputVideo.PosFrames;
            frameDiffs[pos-1] = frameDiff;
            
            Console.WriteLine($"progress: {pos} / {totalFrames}");
        }
        
        outputVideo.Release();

        var diffs = File.CreateText("frame_diffs.txt");
        
        foreach (var diff in frameDiffs)
        {
            diffs.WriteLine(diff.ToString("F4"));
        }
        
        Console.WriteLine($"Done! Average difference: {totalAvg / 75:F4} / 255");
    }
}
