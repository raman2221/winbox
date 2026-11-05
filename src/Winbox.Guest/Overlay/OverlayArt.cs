using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace Winbox.Guest.Overlay;

/// <summary>Draws the overlay's three visuals: the cursor halo, the click ripple, and the caption pill.</summary>
internal static class OverlayArt
{
    public const int HaloSize = 56, RippleSize = 96;
    private static readonly Color Accent = Color.FromArgb(139, 92, 246);
    private static readonly Font CaptionFont = new("Segoe UI Semibold", 11f);
    private const int CaptionPadX = 18, CaptionPadY = 9, DotSize = 8, DotGap = 10;

    public static Bitmap Halo(double opacity)
    {
        var image = NewCanvas(HaloSize, HaloSize, out var graphics);
        using (graphics)
        using (var fill = new SolidBrush(WithAlpha(Accent, 50 * opacity)))
        using (var ring = new Pen(WithAlpha(Accent, 220 * opacity), 3f))
        using (var contrast = new Pen(WithAlpha(Color.White, 170 * opacity), 1.5f))
        {
            graphics.FillEllipse(fill, 6, 6, HaloSize - 12, HaloSize - 12);
            graphics.DrawEllipse(ring, 6, 6, HaloSize - 12, HaloSize - 12);
            // The default Windows wallpaper is blue, where violet alone disappears.
            graphics.DrawEllipse(contrast, 9, 9, HaloSize - 18, HaloSize - 18);
        }
        return image;
    }

    /// <summary>An expanding ring that fades as <paramref name="progress"/> goes from 0 to 1.</summary>
    public static Bitmap Ripple(double progress)
    {
        var image = NewCanvas(RippleSize, RippleSize, out var graphics);
        var diameter = (float)(16 + (RippleSize - 20) * progress);
        var inset = (RippleSize - diameter) / 2;
        using (graphics)
        using (var ring = new Pen(WithAlpha(Accent, 230 * (1 - progress)), 4f))
        {
            graphics.DrawEllipse(ring, inset, inset, diameter, diameter);
        }
        return image;
    }

    public static Bitmap Caption(string text, double opacity)
    {
        var textSize = MeasureText(text);
        var width = CaptionPadX * 2 + DotSize + DotGap + textSize.Width;
        var height = CaptionPadY * 2 + textSize.Height;
        var image = NewCanvas(width, height, out var graphics);
        using (graphics)
        {
            DrawPill(graphics, new Rectangle(0, 0, width - 1, height - 1), opacity);
            DrawCaptionContent(graphics, text, height, opacity);
        }
        return image;
    }

    private static void DrawPill(Graphics graphics, Rectangle bounds, double opacity)
    {
        using var path = RoundedRectangle(bounds, bounds.Height / 2);
        using var fill = new SolidBrush(Color.FromArgb((int)(225 * opacity), 22, 22, 30));
        using var border = new Pen(WithAlpha(Accent, 160 * opacity), 1.5f);
        graphics.FillPath(fill, path);
        graphics.DrawPath(border, path);
    }

    private static void DrawCaptionContent(Graphics graphics, string text, int height, double opacity)
    {
        using var dot = new SolidBrush(WithAlpha(Accent, 255 * opacity));
        using var ink = new SolidBrush(Color.FromArgb((int)(255 * opacity), 245, 245, 250));
        graphics.FillEllipse(dot, CaptionPadX, (height - DotSize) / 2f, DotSize, DotSize);
        graphics.DrawString(text, CaptionFont, ink, CaptionPadX + DotSize + DotGap, CaptionPadY);
    }

    private static Size MeasureText(string text)
    {
        using var probe = new Bitmap(1, 1);
        using var graphics = Graphics.FromImage(probe);
        var size = graphics.MeasureString(text, CaptionFont);
        return new Size((int)Math.Ceiling(size.Width), (int)Math.Ceiling(size.Height));
    }

    private static Bitmap NewCanvas(int width, int height, out Graphics graphics)
    {
        var image = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        graphics = Graphics.FromImage(image);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        // ClearType assumes an opaque background; grayscale antialiasing stays clean on transparent pixels.
        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        return image;
    }

    private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 90, 180);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 180);
        path.CloseFigure();
        return path;
    }

    private static Color WithAlpha(Color color, double alpha) =>
        Color.FromArgb((int)Math.Max(0, Math.Min(255, alpha)), color);
}
