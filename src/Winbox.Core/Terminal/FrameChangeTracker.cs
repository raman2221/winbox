using System.Drawing;
using System.Security.Cryptography;

namespace Winbox.Core.Terminal;

/// <summary>
/// Remembers the last frame drawn so the viewer can skip redrawing an unchanged screen, which
/// keeps the terminal from flickering and saves the cost of re-encoding Sixel.
/// </summary>
public sealed class FrameChangeTracker
{
    private byte[] lastHash = [];
    private Size lastBox;

    /// <summary>True when the PNG or the box it is drawn into differs from the last accepted frame.</summary>
    public bool IsNew(byte[] png, Size box)
    {
        var hash = SHA256.HashData(png);
        if (box == lastBox && hash.AsSpan().SequenceEqual(lastHash)) return false;
        lastHash = hash;
        lastBox = box;
        return true;
    }
}
