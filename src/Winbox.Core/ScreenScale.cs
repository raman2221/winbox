using System.Drawing;
using Winbox.Protocol;
using GuestPoint = Winbox.Protocol.Point;

namespace Winbox.Core;

/// <summary>
/// The size an agent sees a guest screenshot at. Claude shrinks any image whose long edge passes 1568 pixels or
/// whose area passes about 1.15 megapixels, then answers in the shrunk image's pixels, so winbox shrinks the image
/// itself and maps the agent's coordinates back to guest pixels.
/// </summary>
public sealed record ScreenScale(Size Guest, Size Scaled)
{
    public const int MaxLongEdge = 1568;
    public const int MaxPixels = 1_150_000;

    public static ScreenScale For(int width, int height)
    {
        var guest = new Size(width, height);
        return new ScreenScale(guest, new Size(Shrink(width, guest), Shrink(height, guest)));
    }

    public bool IsShrunk => Scaled != Guest;

    public GuestPoint ToGuest(GuestPoint scaled) =>
        new(Unscale(scaled.X, Guest.Width, Scaled.Width), Unscale(scaled.Y, Guest.Height, Scaled.Height));

    /// <summary>Maps the points of a pointer action; other commands have none and pass through.</summary>
    public Command ToGuest(Command scaled) => scaled switch
    {
        ClickCommand c => c with { At = ToGuest(c.At) },
        MoveCommand m => m with { To = ToGuest(m.To) },
        DragCommand d => d with { From = ToGuest(d.From), To = ToGuest(d.To) },
        ScrollCommand s => s with { At = ToGuest(s.At) },
        _ => scaled,
    };

    /// <summary>
    /// One side of the screen, shrunk by whichever limit is stricter. Rounding down keeps the image inside both,
    /// and integer maths for the long edge makes it land exactly on its limit rather than a pixel short.
    /// </summary>
    private static int Shrink(int length, Size screen)
    {
        var byLongEdge = (long)length * MaxLongEdge / Math.Max(screen.Width, screen.Height);
        var byArea = (long)(length * Math.Sqrt(MaxPixels / ((double)screen.Width * screen.Height)));
        return (int)Math.Max(1, Math.Min(length, Math.Min(byLongEdge, byArea)));
    }

    private static int Unscale(int value, int guestLength, int scaledLength) =>
        (int)Math.Round((double)value * guestLength / scaledLength, MidpointRounding.AwayFromZero);
}
