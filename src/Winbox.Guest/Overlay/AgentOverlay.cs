using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace Winbox.Guest.Overlay;

/// <summary>
/// Shows people watching the sandbox what the agent is doing: a halo on its cursor, a ripple on each click,
/// and a caption. Runs its own UI thread so slow commands never stall the animation.
/// </summary>
internal sealed class AgentOverlay : IActivityFeedback
{
    private const int FrameMs = 33, CaptionBottomMargin = 72;
    private readonly OverlayScene _scene = new();

    private AgentOverlay() { }

    public static AgentOverlay Start()
    {
        var overlay = new AgentOverlay();
        var thread = new Thread(overlay.RunUiThread) { IsBackground = true, Name = "overlay" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return overlay;
    }

    public void Announce(string caption) => _scene.Announce(caption);

    public void ShowClick(Protocol.Point at) => _scene.Click(at);

    private void RunUiThread()
    {
        var sprites = new Sprites();
        var timer = new System.Windows.Forms.Timer { Interval = FrameMs };
        timer.Tick += (_, _) => RenderFrameSafely(sprites);
        timer.Start();
        Application.Run(sprites.Context);
    }

    /// <summary>
    /// Skips a frame on failure (e.g. GetCursorPos fails while a UAC prompt owns the secure desktop) instead of
    /// letting WinForms show an unhandled-exception dialog in front of the people watching.
    /// </summary>
    private void RenderFrameSafely(Sprites sprites)
    {
        try
        {
            RenderFrame(sprites, DateTime.UtcNow);
        }
        catch (Exception)
        {
        }
    }

    private void RenderFrame(Sprites sprites, DateTime now)
    {
        RenderHalo(sprites.Halo, _scene.HaloOpacity(now));
        RenderRipple(sprites.Ripple, _scene.Ripple(now));
        RenderCaption(sprites.Caption, _scene.Caption(now));
    }

    private static void RenderHalo(SpriteWindow window, double opacity)
    {
        if (opacity <= 0) { window.Blank(); return; }
        var cursor = Native.CursorPosition();
        using var image = OverlayArt.Halo(opacity);
        window.Present(image, CenteredOn(cursor.X, cursor.Y, OverlayArt.HaloSize));
    }

    private static void RenderRipple(SpriteWindow window, (Protocol.Point At, double Progress)? ripple)
    {
        if (ripple is not { } active) { window.Blank(); return; }
        using var image = OverlayArt.Ripple(active.Progress);
        window.Present(image, CenteredOn(active.At.X, active.At.Y, OverlayArt.RippleSize));
    }

    private static void RenderCaption(SpriteWindow window, (string Text, double Opacity) caption)
    {
        if (caption.Opacity <= 0 || caption.Text.Length == 0) { window.Blank(); return; }
        using var image = OverlayArt.Caption(caption.Text, caption.Opacity);
        var screen = Screen.PrimaryScreen.Bounds;
        var topLeft = new System.Drawing.Point(
            screen.X + (screen.Width - image.Width) / 2, screen.Bottom - CaptionBottomMargin - image.Height);
        window.Present(image, topLeft);
    }

    private static System.Drawing.Point CenteredOn(int x, int y, int size) => new(x - size / 2, y - size / 2);

    /// <summary>The three overlay windows, created and shown on the UI thread.</summary>
    private sealed class Sprites
    {
        public SpriteWindow Halo { get; } = Shown(new SpriteWindow());
        public SpriteWindow Ripple { get; } = Shown(new SpriteWindow());
        public SpriteWindow Caption { get; } = Shown(new SpriteWindow());
        public ApplicationContext Context { get; } = new();

        private static SpriteWindow Shown(SpriteWindow window)
        {
            window.Show();
            window.Blank();
            return window;
        }
    }
}
