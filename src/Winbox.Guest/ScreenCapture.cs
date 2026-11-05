using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace Winbox.Guest;

internal sealed class ScreenCapture
{
    public Size Size => Bounds.Size;

    private static Rectangle Bounds => Screen.PrimaryScreen.Bounds;

    public Size SaveAsPng(string path)
    {
        var bounds = Bounds;
        using var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
        }
        bitmap.Save(path, ImageFormat.Png);
        return bounds.Size;
    }
}
