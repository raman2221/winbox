using System.Text;

namespace Winbox.Core.Rendering;

/// <summary>
/// Encodes a frame as a Sixel image for terminals like Windows Terminal. Colors snap to a fixed
/// 6x7x6 cube so frames encode quickly without a per-frame palette search.
/// </summary>
public static class SixelEncoder
{
    private const int RedLevels = 6, GreenLevels = 7, BlueLevels = 6;
    private const int PaletteSize = RedLevels * GreenLevels * BlueLevels;
    private const int BandHeight = 6;

    public static string Encode(PixelFrame frame)
    {
        var output = new StringBuilder("\u001bP0;1;0q");
        output.Append($"\"1;1;{frame.Width};{frame.Height}");
        AppendPalette(output);
        var indices = Quantize(frame);
        for (var top = 0; top < frame.Height; top += BandHeight)
        {
            AppendBand(output, new Band(indices, frame.Width, frame.Height, top));
        }
        return output.Append("\u001b\\").ToString();
    }

    public static int PaletteIndex(byte r, byte g, byte b) =>
        (Level(r, RedLevels) * GreenLevels + Level(g, GreenLevels)) * BlueLevels + Level(b, BlueLevels);

    private static int Level(byte value, int levels) => (value * (levels - 1) + 127) / 255;

    private static int Percent(int level, int levels) => level * 100 / (levels - 1);

    private static void AppendPalette(StringBuilder output)
    {
        for (var index = 0; index < PaletteSize; index++)
        {
            var b = index % BlueLevels;
            var g = index / BlueLevels % GreenLevels;
            var r = index / (BlueLevels * GreenLevels);
            output.Append($"#{index};2;{Percent(r, RedLevels)};{Percent(g, GreenLevels)};{Percent(b, BlueLevels)}");
        }
    }

    private static int[] Quantize(PixelFrame frame)
    {
        var indices = new int[frame.Width * frame.Height];
        for (var i = 0; i < indices.Length; i++)
        {
            indices[i] = PaletteIndex(frame.Rgb[i * 3], frame.Rgb[i * 3 + 1], frame.Rgb[i * 3 + 2]);
        }
        return indices;
    }

    private static void AppendBand(StringBuilder output, Band band)
    {
        var first = true;
        foreach (var (color, sixels) in band.SixelsByColor())
        {
            if (!first) output.Append('$');
            output.Append('#').Append(color);
            AppendRunLength(output, sixels);
            first = false;
        }
        output.Append('-');
    }

    private static void AppendRunLength(StringBuilder output, byte[] sixels)
    {
        for (var x = 0; x < sixels.Length;)
        {
            var run = 1;
            while (x + run < sixels.Length && sixels[x + run] == sixels[x]) run++;
            var symbol = (char)('?' + sixels[x]);
            if (run > 3) output.Append('!').Append(run).Append(symbol);
            else output.Append(symbol, run);
            x += run;
        }
    }

    /// <summary>Six pixel rows; each column becomes one sixel character per color.</summary>
    private readonly record struct Band(int[] Indices, int Width, int Height, int Top)
    {
        public Dictionary<int, byte[]> SixelsByColor()
        {
            var byColor = new Dictionary<int, byte[]>();
            var rows = Math.Min(BandHeight, Height - Top);
            for (var row = 0; row < rows; row++)
            {
                AddRow(byColor, row);
            }
            return byColor;
        }

        private void AddRow(Dictionary<int, byte[]> byColor, int row)
        {
            var offset = (Top + row) * Width;
            for (var x = 0; x < Width; x++)
            {
                var color = Indices[offset + x];
                if (!byColor.TryGetValue(color, out var sixels)) byColor[color] = sixels = new byte[Width];
                sixels[x] |= (byte)(1 << row);
            }
        }
    }
}
