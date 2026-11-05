using System.Drawing;

namespace Winbox.Core.Terminal;

/// <summary>Sizes the viewer's image area and status line from the terminal's dimensions.</summary>
public static class ViewerGeometry
{
    private const int SixelBandHeight = 6;

    /// <summary>
    /// The pixel box the guest image must fit in: every row but the last (kept for the status line).
    /// The height is rounded down to whole sixel bands, since a partial final band still occupies a
    /// full band and could push the image into the status row and scroll the screen.
    /// </summary>
    public static Size ImageBox(Size window, Size cell)
    {
        var height = Math.Max(0, window.Height - 1) * cell.Height;
        var bandedHeight = height / SixelBandHeight * SixelBandHeight;
        return new Size(Math.Max(1, window.Width * cell.Width), Math.Max(SixelBandHeight, bandedHeight));
    }

    /// <summary>
    /// The status text, cut to leave the last column empty so writing it never triggers a wrap.
    /// </summary>
    public static string StatusLine(Size guest, int columns)
    {
        var text = $"winbox · {guest.Width}x{guest.Height} · Ctrl+Q quit · click/type to control";
        var room = Math.Max(0, columns - 1);
        return text.Length <= room ? text : text[..room];
    }
}
