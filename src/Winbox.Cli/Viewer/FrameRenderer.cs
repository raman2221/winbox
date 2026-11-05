using System.Drawing;
using Winbox.Core;
using Winbox.Core.Rendering;
using Winbox.Core.Terminal;

namespace Winbox.Cli.Viewer;

/// <summary>Polls guest screenshots at up to ten frames a second and draws the ones that changed.</summary>
internal sealed class FrameRenderer(GuestClient guest, TerminalScreen screen, ViewerState state)
{
    private static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan CellReportWait = TimeSpan.FromMilliseconds(300);

    private readonly FrameChangeTracker changes = new();
    private Size lastBox;

    public async Task RunAsync(CancellationToken cancel)
    {
        await state.WaitForCellReportAsync(CellReportWait, cancel);
        while (true)
        {
            var nextFrame = Task.Delay(FrameInterval, cancel);
            await RenderOnceAsync(cancel);
            await nextFrame;
        }
    }

    private async Task RenderOnceAsync(CancellationToken cancel)
    {
        var shot = await guest.ScreenshotAsync(cancel);
        var viewport = new Viewport(new Size(Console.WindowWidth, Console.WindowHeight), state.Cell);
        if (!changes.IsNew(shot.Png, viewport.ImageBox)) return;
        Draw(shot, viewport);
    }

    private void Draw(Screenshot shot, Viewport viewport)
    {
        // A resize or new cell size can leave pieces of the old, larger frame outside the new one.
        if (viewport.ImageBox != lastBox) screen.ClearBeforeNextFrame();
        lastBox = viewport.ImageBox;
        var frame = FrameDecoder.DecodeToFit(shot.Png, viewport.ImageBox);
        var guestSize = new Size(shot.Width, shot.Height);
        var statusLine = ViewerGeometry.StatusLine(guestSize, viewport.Window.Width);
        screen.DrawFrame(SixelEncoder.Encode(frame), new StatusText(statusLine, viewport.Window.Height));
        state.Layout = new ImageLayout(viewport.Cell, new Size(frame.Width, frame.Height), guestSize);
    }

    /// <summary>The terminal's size in cells and the pixel size of one cell.</summary>
    private readonly record struct Viewport(Size Window, Size Cell)
    {
        public Size ImageBox => ViewerGeometry.ImageBox(Window, Cell);
    }
}
