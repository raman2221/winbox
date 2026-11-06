using System;
using System.Threading;

namespace Winbox.Guest.Overlay;

/// <summary>
/// Shows a caption and keeps it on screen until disposed. The overlay fades a caption a few seconds after it
/// is announced, and some actions, such as typing a long text, take longer than that.
/// </summary>
internal sealed class CaptionKeepAlive : IDisposable
{
    // Well inside the overlay's linger time, so the caption never starts to fade while its action runs.
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(1);

    private readonly Timer _refresh;

    public CaptionKeepAlive(IActivityFeedback feedback, string caption)
    {
        feedback.Announce(caption);
        _refresh = new Timer(_ => feedback.Announce(caption), null, RefreshInterval, RefreshInterval);
    }

    public void Dispose()
    {
        // Wait out a refresh already under way, or it could land after the next command's caption and replace it.
        using var stopped = new ManualResetEvent(false);
        if (_refresh.Dispose(stopped)) stopped.WaitOne();
    }
}
