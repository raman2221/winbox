using System;
using Winbox.Protocol;

namespace Winbox.Guest.Overlay;

/// <summary>
/// Thread-safe record of recent agent activity, and the fade math for each visual.
/// Written by the command thread, read by the overlay's UI thread.
/// </summary>
internal sealed class OverlayScene
{
    private static readonly TimeSpan HaloLinger = TimeSpan.FromSeconds(2.5), CaptionLinger = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan FadeOut = TimeSpan.FromMilliseconds(450), RippleLength = TimeSpan.FromMilliseconds(500);

    private readonly object _gate = new();
    private DateTime _lastActivity = DateTime.MinValue, _captionSince = DateTime.MinValue, _rippleSince = DateTime.MinValue;
    private string _caption = "";
    private Point _rippleAt;

    public void Announce(string caption)
    {
        lock (_gate) { _caption = caption; _captionSince = _lastActivity = DateTime.UtcNow; }
    }

    public void Click(Point at)
    {
        lock (_gate) { _rippleAt = at; _rippleSince = _lastActivity = DateTime.UtcNow; }
    }

    public double HaloOpacity(DateTime now)
    {
        lock (_gate) return Linger(now - _lastActivity, HaloLinger);
    }

    public (string Text, double Opacity) Caption(DateTime now)
    {
        lock (_gate) return (_caption, Linger(now - _captionSince, CaptionLinger));
    }

    /// <summary>Progress 0..1 of the latest ripple, or null once it has finished.</summary>
    public (Point At, double Progress)? Ripple(DateTime now)
    {
        lock (_gate)
        {
            var progress = (now - _rippleSince).TotalMilliseconds / RippleLength.TotalMilliseconds;
            return progress is >= 0 and < 1 ? (_rippleAt, progress) : null;
        }
    }

    /// <summary>Fully visible while recent, then fades linearly to zero.</summary>
    private static double Linger(TimeSpan age, TimeSpan visibleFor)
    {
        if (age < visibleFor) return 1;
        var fade = 1 - (age - visibleFor).TotalMilliseconds / FadeOut.TotalMilliseconds;
        return Math.Max(0, fade);
    }
}
