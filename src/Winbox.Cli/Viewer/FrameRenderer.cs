using System.Drawing;
using Winbox.Core;
using Winbox.Core.Rendering;
using Winbox.Core.Terminal;

namespace Winbox.Cli.Viewer;

/// <summary>
/// Polls guest screenshots at up to ten frames a second and draws the ones that changed. A frame
/// the guest can't deliver is skipped; only repeated liveness failures end the viewer.
/// </summary>
internal sealed class FrameRenderer(GuestClient guest, TerminalScreen screen, ViewerState state)
{
    private const int MissesBeforeGivingUp = 3;
    private const string RetryingNotice = "guest not responding, retrying";

    private static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan CellReportWait = TimeSpan.FromMilliseconds(300);

    // Spaces the liveness retries out to about the heartbeat period, so three misses span real time.
    private static readonly TimeSpan MissBackoff = TimeSpan.FromSeconds(1);

    private readonly FrameChangeTracker changes = new();
    private readonly OutageTolerance outages = new(MissesBeforeGivingUp);
    private Size lastBox;
    private Size guestSize;
    private StatusText lastStatus;

    public async Task RunAsync(CancellationToken cancel)
    {
        await state.WaitForCellReportAsync(CellReportWait, cancel);
        guestSize = ScreenSizeFromHeartbeat();
        while (true)
        {
            var nextFrame = Task.Delay(FrameInterval, cancel);
            await TryRenderAsync(cancel);
            RefreshStatus();
            await nextFrame;
        }
    }

    private async Task TryRenderAsync(CancellationToken cancel)
    {
        try
        {
            await RenderOnceAsync(cancel);
            outages.RecordSuccess();
        }
        catch (GuestUnavailableException miss)
        {
            await WaitOutMissAsync(miss, cancel);
        }
        catch (WinboxException failure)
        {
            // e.g. the guest can't capture the screen while a UAC prompt or the lock screen is up.
            state.ShowNotice(failure.Message);
        }
    }

    private async Task WaitOutMissAsync(GuestUnavailableException miss, CancellationToken cancel)
    {
        outages.RecordMiss(miss);
        state.ShowNotice(RetryingNotice);
        RefreshStatus();
        await Task.Delay(MissBackoff, cancel);
    }

    private async Task RenderOnceAsync(CancellationToken cancel)
    {
        var shot = await guest.ScreenshotAsync(cancel);
        var viewport = new Viewport(CurrentWindow(), state.Cell);
        if (!changes.IsNew(shot.Png, viewport.ImageBox)) return;
        Draw(shot, viewport);
    }

    private void Draw(Screenshot shot, Viewport viewport)
    {
        // A resize or new cell size can leave pieces of the old, larger frame outside the new one.
        if (viewport.ImageBox != lastBox) screen.ClearBeforeNextFrame();
        lastBox = viewport.ImageBox;
        var frame = FrameDecoder.DecodeToFit(shot.Png, viewport.ImageBox);
        guestSize = new Size(shot.Width, shot.Height);
        lastStatus = StatusFor(viewport.Window);
        screen.DrawFrame(SixelEncoder.Encode(frame), lastStatus);
        state.Layout = new ImageLayout(viewport.Cell, new Size(frame.Width, frame.Height), guestSize);
    }

    // The status line changes without the picture changing (a notice appears or expires), so it is
    // redrawn on its own, and only when its text or row actually differs.
    private void RefreshStatus()
    {
        var status = StatusFor(CurrentWindow());
        if (status == lastStatus) return;
        screen.DrawStatus(status);
        lastStatus = status;
    }

    private StatusText StatusFor(Size window) =>
        new(ViewerGeometry.StatusLine(guestSize, state.CurrentNotice(), window.Width), window.Height);

    private Size ScreenSizeFromHeartbeat() =>
        guest.ReadStatus() is { } status ? new Size(status.ScreenWidth, status.ScreenHeight) : Size.Empty;

    private static Size CurrentWindow() => new(Console.WindowWidth, Console.WindowHeight);

    /// <summary>The terminal's size in cells and the pixel size of one cell.</summary>
    private readonly record struct Viewport(Size Window, Size Cell)
    {
        public Size ImageBox => ViewerGeometry.ImageBox(Window, Cell);
    }
}
