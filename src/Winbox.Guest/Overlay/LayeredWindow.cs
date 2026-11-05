using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Winbox.Guest.Overlay;

/// <summary>Per-pixel-alpha window painting via UpdateLayeredWindow, plus capture exclusion.</summary>
internal static class LayeredWindow
{
    public const int ExtendedStyles = 0x00080000 | 0x00000020 | 0x00000080 | 0x08000000 | 0x00000008;
    private const uint ExcludeFromCapture = 0x11;
    private const int UseAlpha = 2;

    /// <summary>Keeps the overlay visible to people but out of the agent's screenshots.</summary>
    public static void HideFromCapture(IntPtr window) => SetWindowDisplayAffinity(window, ExcludeFromCapture);

    public static void Present(IntPtr window, Bitmap bitmap, System.Drawing.Point topLeft)
    {
        var screenDc = GetDC(IntPtr.Zero);
        var memoryDc = CreateCompatibleDC(screenDc);
        var hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));
        var previous = SelectObject(memoryDc, hBitmap);
        try
        {
            Blit(window, screenDc, memoryDc, new Rect(topLeft.X, topLeft.Y, bitmap.Width, bitmap.Height));
        }
        finally
        {
            SelectObject(memoryDc, previous);
            DeleteObject(hBitmap);
            DeleteDC(memoryDc);
            ReleaseDC(IntPtr.Zero, screenDc);
        }
    }

    private static void Blit(IntPtr window, IntPtr screenDc, IntPtr memoryDc, Rect rect)
    {
        var target = new NativePoint { X = rect.X, Y = rect.Y };
        var size = new NativeSize { Width = rect.Width, Height = rect.Height };
        var source = new NativePoint();
        var blend = new BlendFunction { SourceConstantAlpha = 255, AlphaFormat = 1 };
        UpdateLayeredWindow(window, screenDc, ref target, ref size, memoryDc, ref source, 0, ref blend, UseAlpha);
    }

    private readonly record struct Rect(int X, int Y, int Width, int Height);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeSize { public int Width, Height; }

    [StructLayout(LayoutKind.Sequential)]
    private struct BlendFunction { public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat; }

    [DllImport("user32.dll")]
    private static extern bool SetWindowDisplayAffinity(IntPtr window, uint affinity);

    [DllImport("user32.dll")]
    private static extern bool UpdateLayeredWindow(IntPtr window, IntPtr screenDc, ref NativePoint target,
        ref NativeSize size, IntPtr sourceDc, ref NativePoint source, int colorKey, ref BlendFunction blend, int flags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr window);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr window, IntPtr dc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr dc);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr dc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr dc, IntPtr gdiObject);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr gdiObject);
}
