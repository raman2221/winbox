using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace Winbox.Guest;

internal sealed class ScreenCapture
{
    public Size Size => Bounds.Size;

    private static Rectangle Bounds => Screen.PrimaryScreen.Bounds;

    /// <summary>
    /// Encodes in memory and writes once: GDI+ saves in many small writes, each a round trip over the mapped share.
    /// </summary>
    public Size SaveAsPng(string path)
    {
        var bounds = Bounds;
        using var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
        }
        using var png = new MemoryStream();
        bitmap.Save(png, ImageFormat.Png);
        File.WriteAllBytes(path, png.ToArray());
        return bounds.Size;
    }
}
