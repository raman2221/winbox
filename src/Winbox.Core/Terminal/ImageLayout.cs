using System.Drawing;
using GuestPoint = Winbox.Protocol.Point;

namespace Winbox.Core.Terminal;

/// <summary>
/// Where the guest screen sits in the terminal: drawn from the top-left cell, scaled down to
/// <paramref name="Displayed"/> pixels, with each cell covering <paramref name="Cell"/> pixels.
/// </summary>
public sealed record ImageLayout(Size Cell, Size Displayed, Size Guest)
{
    /// <summary>Maps a 1-based terminal cell to the guest pixel under its center; null outside the image.</summary>
    public GuestPoint? GuestPointAt(int column, int row)
    {
        if (column < 1 || row < 1) return null;
        var x = (column - 1) * Cell.Width + Cell.Width / 2;
        var y = (row - 1) * Cell.Height + Cell.Height / 2;
        if (x >= Displayed.Width || y >= Displayed.Height) return null;
        return new GuestPoint(Scale(x, Displayed.Width, Guest.Width), Scale(y, Displayed.Height, Guest.Height));
    }

    // Sample the middle of the displayed pixel so the mapping stays centered at any scale.
    private static int Scale(int pixel, int displayed, int guest) =>
        Math.Min(guest - 1, (int)((pixel + 0.5) * guest / displayed));
}
