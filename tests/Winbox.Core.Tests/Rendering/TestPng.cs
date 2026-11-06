using System.Drawing;
using System.Drawing.Imaging;

namespace Winbox.Core.Tests.Rendering;

/// <summary>Small PNG images for rendering tests.</summary>
internal static class TestPng
{
    public static byte[] Solid(int width, int height, Color color)
    {
        using var bitmap = new Bitmap(width, height);
        using (var graphics = Graphics.FromImage(bitmap)) graphics.Clear(color);
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }
}
