namespace Winbox.Core.Rendering;

/// <summary>An uncompressed image, three bytes (R, G, B) per pixel, row by row.</summary>
public sealed record PixelFrame(int Width, int Height, byte[] Rgb)
{
    public (byte R, byte G, byte B) PixelAt(int x, int y)
    {
        var offset = (y * Width + x) * 3;
        return (Rgb[offset], Rgb[offset + 1], Rgb[offset + 2]);
    }
}
