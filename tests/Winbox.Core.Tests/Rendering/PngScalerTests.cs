using System.Drawing;
using System.Drawing.Imaging;
using Winbox.Core.Rendering;

namespace Winbox.Core.Tests.Rendering;

public class PngScalerTests
{
    [Fact]
    public void Resize_Png_ReturnsPngOfTheRequestedSize()
    {
        var png = TestPng.Solid(400, 200, Color.Black);

        var resized = PngScaler.Resize(png, new Size(150, 75));

        using var image = Decode(resized);
        Assert.Equal(ImageFormat.Png, image.RawFormat);
        Assert.Equal(new Size(150, 75), image.Size);
    }

    [Fact]
    public void Resize_SolidColor_KeepsTheColorRightUpToTheEdges()
    {
        var color = Color.FromArgb(200, 100, 50);
        var png = TestPng.Solid(400, 200, color);

        using var image = Decode(PngScaler.Resize(png, new Size(150, 75)));

        Assert.Equal(color.ToArgb(), image.GetPixel(0, 0).ToArgb());
        Assert.Equal(color.ToArgb(), image.GetPixel(149, 74).ToArgb());
    }

    private static Bitmap Decode(byte[] png) => new(new MemoryStream(png));
}
