using Winbox.Core.Rendering;

namespace Winbox.Core.Tests.Rendering;

public class SixelEncoderTests
{
    private const int Black = 0, White = 251;

    [Fact]
    public void Encode_AnyFrame_StartsWithDeviceControlString()
    {
        var sixel = SixelEncoder.Encode(SolidFrame(4, 6, 0, 0, 0));

        Assert.StartsWith("\u001bP", sixel);
    }

    [Fact]
    public void Encode_AnyFrame_EndsWithStringTerminator()
    {
        var sixel = SixelEncoder.Encode(SolidFrame(4, 6, 0, 0, 0));

        Assert.EndsWith("\u001b\\", sixel);
    }

    [Fact]
    public void Encode_Frame_DeclaresItsSizeInRasterAttributes()
    {
        var sixel = SixelEncoder.Encode(SolidFrame(7, 13, 0, 0, 0));

        Assert.Contains("q\"1;1;7;13#", sixel);
    }

    [Fact]
    public void Encode_SolidColorRow_UsesRunLengthRepeat()
    {
        var sixel = SixelEncoder.Encode(SolidFrame(10, 6, 255, 255, 255));

        Assert.Contains($"#{White}!10~-", sixel);
    }

    [Fact]
    public void Encode_ShortRun_WritesSixelsLiterally()
    {
        var sixel = SixelEncoder.Encode(SolidFrame(3, 6, 0, 0, 0));

        Assert.Contains($"#{Black}~~~-", sixel);
        Assert.DoesNotContain('!', sixel);
    }

    [Fact]
    public void Encode_TwoColorsInOneBand_SeparatesThemWithCarriageReturn()
    {
        var frame = new PixelFrame(2, 1, [0, 0, 0, 255, 255, 255]);

        var sixel = SixelEncoder.Encode(frame);

        Assert.Contains($"#{Black}@?$#{White}?@-", sixel);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(6, 1)]
    [InlineData(7, 2)]
    [InlineData(12, 2)]
    [InlineData(13, 3)]
    public void Encode_FrameHeight_EmitsOneBandPerSixRows(int height, int expectedBands)
    {
        var sixel = SixelEncoder.Encode(SolidFrame(2, height, 9, 9, 9));

        Assert.Equal(expectedBands, sixel.Count(ch => ch == '-'));
    }

    [Theory]
    [InlineData(Black, "0;0;0")]
    [InlineData(White, "100;100;100")]
    public void Encode_AnyFrame_DefinesPaletteEndpoints(int index, string rgbPercent)
    {
        var sixel = SixelEncoder.Encode(SolidFrame(1, 1, 0, 0, 0));

        Assert.Contains($"#{index};2;{rgbPercent}#", sixel);
    }

    [Theory]
    [InlineData(0, 0, 0, Black)]
    [InlineData(255, 255, 255, White)]
    [InlineData(255, 0, 0, 210)]
    [InlineData(0, 0, 255, 5)]
    public void PaletteIndex_PrimaryColor_MapsToCubeCorner(byte r, byte g, byte b, int expected)
    {
        Assert.Equal(expected, SixelEncoder.PaletteIndex(r, g, b));
    }

    private static PixelFrame SolidFrame(int width, int height, byte r, byte g, byte b)
    {
        var rgb = new byte[width * height * 3];
        for (var i = 0; i < rgb.Length; i += 3) (rgb[i], rgb[i + 1], rgb[i + 2]) = (r, g, b);
        return new PixelFrame(width, height, rgb);
    }
}
