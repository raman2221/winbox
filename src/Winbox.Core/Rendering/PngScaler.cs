using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Winbox.Core.Rendering;

/// <summary>Resizes a PNG and encodes the result as PNG again.</summary>
public static class PngScaler
{
    public static byte[] Resize(byte[] png, Size size)
    {
        using var input = new MemoryStream(png);
        using var source = new Bitmap(input);
        using var scaled = new Bitmap(size.Width, size.Height, PixelFormat.Format24bppRgb);
        using (var graphics = Graphics.FromImage(scaled))
        {
            // Bicubic keeps small UI text legible for the model reading the screenshot.
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            DrawWithoutDarkEdges(graphics, source, size);
        }
        using var output = new MemoryStream();
        scaled.Save(output, ImageFormat.Png);
        return output.ToArray();
    }

    // Without mirrored edge sampling, bicubic blends transparent black into the outermost pixels.
    private static void DrawWithoutDarkEdges(Graphics graphics, Bitmap source, Size size)
    {
        using var edges = new ImageAttributes();
        edges.SetWrapMode(WrapMode.TileFlipXY);
        var target = new Rectangle(0, 0, size.Width, size.Height);
        graphics.DrawImage(source, target, 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, edges);
    }
}
