using OpenCvSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Size = OpenCvSharp.Size;

namespace VideoDiff;

internal static class Program
{
    private static void Main(string[] args)
    {
        // using var controlFrame = (Image<Rgb24>)Image.Load("control.png");
        using var controlFrame = new Mat("control.png");
        
        using var inputVideo = new VideoCapture("sample.webm");
        var frameSize = new Size(inputVideo.FrameWidth, inputVideo.FrameHeight);
        var totalFrames = inputVideo.FrameCount;

        using var outputVideo = new VideoWriter("output.mp4", FourCC.Default, inputVideo.Fps, frameSize);
        
        using var frame = new Mat();

        var pixels = new int[frameSize.Height, frameSize.Width];

        var totalAvg = 0d;

        while (inputVideo.Read(frame))
        {
            // inMat.GetRectangularArray(out int[,] frame);
            
            // var frameStream = inMat.ToMemoryStream();
            // using var frame = (Image<Rgb24>)Image.Load(frameStream);
            // frameStream.Dispose();

            var size = frame.Size();

            // OpenCvSharp.MatType.CV_8UC3
            
            // using var outFrame = new Mat(frameSizeArr, inMat.Type());

            var frameAvg = 0d;
            
            for (var x = 0; x < size.Width; x++)
            {
                var colAvg = 0d;
                
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
            totalAvg += frameAvg / size.Width;
            
            outputVideo.Write(InputArray.Create(pixels, MatType.CV_8UC4));
            
            var pos = inputVideo.PosFrames;
            
            if (pos > 75)
                break;
            
            Console.WriteLine($"{pos} / {totalFrames}");
        }
        
        outputVideo.Release();
        
        Console.WriteLine($"Done! Average difference: {totalAvg / 75:F3} / 255");
    }
}