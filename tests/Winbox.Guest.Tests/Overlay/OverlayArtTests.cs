using System.Drawing;
using Winbox.Guest.Overlay;

namespace Winbox.Guest.Tests.Overlay;

public class OverlayArtTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(0.5)]
    [InlineData(1)]
    public void Halo_IsAHaloSizedSquare(double opacity)
    {
        using var image = OverlayArt.Halo(opacity);

        Assert.Equal(new Size(OverlayArt.HaloSize, OverlayArt.HaloSize), image.Size);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.5)]
    [InlineData(1)]
    public void Ripple_IsARippleSizedSquare(double progress)
    {
        using var image = OverlayArt.Ripple(progress);

        Assert.Equal(new Size(OverlayArt.RippleSize, OverlayArt.RippleSize), image.Size);
    }

    [Theory]
    [InlineData("Clicking", 0)]
    [InlineData("Clicking", 1)]
    [InlineData("Typing “a sentence long enough to fill the caption”", 0.5)]
    public void Caption_IsAPillWiderThanItIsTall(string text, double opacity)
    {
        using var image = OverlayArt.Caption(text, opacity);

        Assert.True(image.Width > image.Height, $"caption was {image.Width}x{image.Height}");
    }

    [Fact]
    public void Caption_LongerText_IsWider()
    {
        using var shortCaption = OverlayArt.Caption("Clicking", 1);
        using var longCaption = OverlayArt.Caption("Opening notepad C:\\notes\\today.txt", 1);

        Assert.True(longCaption.Width > shortCaption.Width, $"{longCaption.Width} vs {shortCaption.Width}");
    }

    [Fact]
    public void Halo_AtFullOpacity_DrawsVisiblePixels()
    {
        using var image = OverlayArt.Halo(1);

        Assert.True(MaxAlpha(image) > 0);
    }

    [Fact]
    public void Halo_AtZeroOpacity_IsFullyTransparent()
    {
        using var image = OverlayArt.Halo(0);

        Assert.Equal(0, MaxAlpha(image));
    }

    [Fact]
    public void Ripple_AtStart_DrawsVisiblePixels()
    {
        using var image = OverlayArt.Ripple(0);

        Assert.True(MaxAlpha(image) > 0);
    }

    [Fact]
    public void Ripple_AtFullProgress_HasFadedToTransparent()
    {
        using var image = OverlayArt.Ripple(1);

        Assert.Equal(0, MaxAlpha(image));
    }

    [Fact]
    public void Caption_AtFullOpacity_DrawsVisiblePixels()
    {
        using var image = OverlayArt.Caption("Clicking", 1);

        Assert.True(MaxAlpha(image) > 0);
    }

    [Fact]
    public void Caption_AtZeroOpacity_IsFullyTransparent()
    {
        using var image = OverlayArt.Caption("Clicking", 0);

        Assert.Equal(0, MaxAlpha(image));
    }

    private static int MaxAlpha(Bitmap image) =>
        (from y in Enumerable.Range(0, image.Height)
         from x in Enumerable.Range(0, image.Width)
         select image.GetPixel(x, y).A).Max();
}
