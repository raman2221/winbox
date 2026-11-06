using System.Drawing;
using Winbox.Core.Terminal;

namespace Winbox.Cli.Viewer;

/// <summary>
/// What the renderer and the input side share: the cell size, where the image was last drawn,
/// and any notice waiting to be shown on the status line.
/// </summary>
internal sealed class ViewerState
{
    // The size Windows Terminal reports for its virtual Sixel cell; used when the terminal stays silent.
    private static readonly Size FallbackCell = new(10, 20);
    private static readonly TimeSpan NoticeDuration = TimeSpan.FromSeconds(4);

    private readonly TaskCompletionSource cellReported = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly StatusNotice notice = new(NoticeDuration);
    private readonly Lock gate = new();
    private Size cell = FallbackCell;
    private ImageLayout? layout;

    public Size Cell
    {
        get { lock (gate) return cell; }
    }

    /// <summary>Null until the first frame is on screen, so early clicks are ignored rather than misplaced.</summary>
    public ImageLayout? Layout
    {
        get { lock (gate) return layout; }
        set { lock (gate) layout = value; }
    }

    public void ShowNotice(string message) => notice.Post(message, DateTimeOffset.UtcNow);

    public string? CurrentNotice() => notice.CurrentAt(DateTimeOffset.UtcNow);

    public void ReportCell(Size reported)
    {
        if (reported.Width <= 0 || reported.Height <= 0) return;
        lock (gate) cell = reported;
        cellReported.TrySetResult();
    }

    /// <summary>Waits until the terminal reports its cell size or the time limit passes, whichever is first.</summary>
    public Task WaitForCellReportAsync(TimeSpan limit, CancellationToken cancel) =>
        Task.WhenAny(cellReported.Task, Task.Delay(limit, cancel));
}
