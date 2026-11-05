using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Winbox.Core.Rendering;

/// <summary>Decodes a PNG and scales it down to fit a box, keeping the aspect ratio.</summary>
public static class FrameDecoder
{
    public static PixelFrame DecodeToFit(byte[] png, Size box)
    {
        using var source = new Bitmap(new MemoryStream(png));
        var size = FitInside(source.Size, box);
        using var scaled = new Bitmap(size.Width, size.Height, PixelFormat.Format24bppRgb);
        using (var graphics = Graphics.FromImage(scaled))
        {
            graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
            graphics.DrawImage(source, 0, 0, size.Width, size.Height);
        }
        return ToFrame(scaled);
    }

    public static Size FitInside(Size image, Size box)
    {
        var scale = Math.Min(1.0, Math.Min((double)box.Width / image.Width, (double)box.Height / image.Height));
        return new Size(Math.Max(1, (int)(image.Width * scale)), Math.Max(1, (int)(image.Height * scale)));
    }

    private static PixelFrame ToFrame(Bitmap bitmap)
    {
        var rect = new Rectangle(Point.Empty, bitmap.Size);
        var data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        try
        {
            return new PixelFrame(bitmap.Width, bitmap.Height, CopyAsRgb(data));
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    // GDI stores 24-bit pixels as B, G, R with padded rows; flip to tightly packed R, G, B.
    private static byte[] CopyAsRgb(BitmapData data)
    {
        var row = new byte[data.Stride];
        var rgb = new byte[data.Width * data.Height * 3];
        for (var y = 0; y < data.Height; y++)
        {
            Marshal.Copy(data.Scan0 + y * data.Stride, row, 0, data.Stride);
            for (var x = 0; x < data.Width; x++)
            {
                var target = (y * data.Width + x) * 3;
                rgb[target] = row[x * 3 + 2];
                rgb[target + 1] = row[x * 3 + 1];
                rgb[target + 2] = row[x * 3];
            }
        }
        return rgb;
    }
}
