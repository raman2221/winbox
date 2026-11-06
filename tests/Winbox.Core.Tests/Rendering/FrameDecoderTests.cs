using System.Drawing;
using Winbox.Core.Rendering;

namespace Winbox.Core.Tests.Rendering;

public class FrameDecoderTests
{
    [Theory]
    [InlineData(800, 600, 1920, 1080)]
    [InlineData(640, 480, 640, 480)]
    [InlineData(1, 1, 5000, 5000)]
    public void FitInside_ImageSmallerThanBox_KeepsOriginalSize(int width, int height, int boxWidth, int boxHeight)
    {
        var fitted = FrameDecoder.FitInside(new Size(width, height), new Size(boxWidth, boxHeight));

        Assert.Equal(new Size(width, height), fitted);
    }

    [Theory]
    [InlineData(1920, 1080, 960, 960, 960, 540)]
    [InlineData(1000, 2000, 500, 500, 250, 500)]
    [InlineData(1920, 1080, 1000, 1000, 1000, 562)]
    [InlineData(2560, 1440, 1000, 2000, 1000, 562)]
    [InlineData(1366, 768, 1000, 700, 1000, 562)]
    [InlineData(1920, 1200, 1280, 720, 1152, 720)]
    public void FitInside_ImageLargerThanBox_ScalesDownToTheLimitingSide(
        int width, int height, int boxWidth, int boxHeight, int expectedWidth, int expectedHeight)
    {
        var fitted = FrameDecoder.FitInside(new Size(width, height), new Size(boxWidth, boxHeight));

        Assert.Equal(new Size(expectedWidth, expectedHeight), fitted);
    }

    [Theory]
    [InlineData(1920, 1080, 333, 777)]
    [InlineData(3840, 2160, 1203, 401)]
    [InlineData(1024, 768, 997, 641)]
    [InlineData(1366, 768, 1365, 767)]
    public void FitInside_AnyScaling_KeepsAspectRatioWithinOnePixel(int width, int height, int boxWidth, int boxHeight)
    {
        var fitted = FrameDecoder.FitInside(new Size(width, height), new Size(boxWidth, boxHeight));

        var heightForFittedWidth = (double)fitted.Width * height / width;
        Assert.InRange(fitted.Height, heightForFittedWidth - 1, heightForFittedWidth + 1);
        Assert.True(fitted.Width <= boxWidth && fitted.Height <= boxHeight);
    }

    [Fact]
    public void FitInside_TinyBox_NeverCollapsesBelowOnePixel()
    {
        var fitted = FrameDecoder.FitInside(new Size(1920, 10), new Size(100, 100));

        Assert.Equal(new Size(100, 1), fitted);
    }

    [Fact]
    public void DecodeToFit_LargePng_ReturnsFrameOfFittedSize()
    {
        var png = TestPng.Solid(400, 200, Color.Black);

        var frame = FrameDecoder.DecodeToFit(png, new Size(100, 100));

        Assert.Equal((100, 50), (frame.Width, frame.Height));
        Assert.Equal(100 * 50 * 3, frame.Rgb.Length);
    }

    [Fact]
    public void DecodeToFit_ColoredPng_StoresPixelsAsRgb()
    {
        var png = TestPng.Solid(40, 20, Color.FromArgb(200, 100, 50));

        var frame = FrameDecoder.DecodeToFit(png, new Size(20, 20));

        Assert.Equal(((byte)200, (byte)100, (byte)50), frame.PixelAt(10, 5));
    }
}
