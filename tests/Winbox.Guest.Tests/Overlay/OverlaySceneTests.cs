using Winbox.Guest.Overlay;
using Winbox.Protocol;

namespace Winbox.Guest.Tests.Overlay;

/// <summary>
/// The scene stamps each write with its own DateTime.UtcNow, so every test brackets the write between two clock
/// readings and asserts only what holds wherever the stamp falls inside that window.
/// </summary>
public class OverlaySceneTests
{
    // Mirrors OverlayScene's private timings.
    private static readonly TimeSpan HaloLinger = TimeSpan.FromSeconds(2.5), CaptionLinger = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan FadeOut = TimeSpan.FromMilliseconds(450);
    private static readonly TimeSpan RippleLength = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan OneMs = TimeSpan.FromMilliseconds(1);
    private const int HammerIterations = 50_000;

    private readonly OverlayScene _scene = new();

    [Fact]
    public void HaloOpacity_WithNoActivity_IsZero()
    {
        var opacity = _scene.HaloOpacity(DateTime.UtcNow);

        Assert.Equal(0, opacity);
    }

    [Fact]
    public void HaloOpacity_RightAfterAnnounce_IsFull()
    {
        var write = WriteWindow.Around(() => _scene.Announce("Clicking"));

        var opacity = _scene.HaloOpacity(write.Latest);

        Assert.Equal(1, opacity);
    }

    [Fact]
    public void HaloOpacity_RightAfterClick_IsFull()
    {
        var write = WriteWindow.Around(() => _scene.Click(new Point(5, 5)));

        var opacity = _scene.HaloOpacity(write.Latest);

        Assert.Equal(1, opacity);
    }

    [Fact]
    public void HaloOpacity_JustBeforeLingerEnds_IsStillFull()
    {
        var write = WriteWindow.Around(() => _scene.Announce("Clicking"));

        var opacity = _scene.HaloOpacity(write.Earliest + HaloLinger - OneMs);

        Assert.Equal(1, opacity);
    }

    [Fact]
    public void HaloOpacity_HalfwayThroughFade_IsHalf()
    {
        var write = WriteWindow.Around(() => _scene.Announce("Clicking"));

        var opacity = _scene.HaloOpacity(write.Earliest + HaloLinger + Half(FadeOut));

        Assert.InRange(opacity, 0.5 - WriteWindow.Epsilon, 0.5 + write.Uncertainty(FadeOut));
    }

    [Fact]
    public void HaloOpacity_AfterFade_IsZero()
    {
        var write = WriteWindow.Around(() => _scene.Announce("Clicking"));

        var opacity = _scene.HaloOpacity(write.Latest + HaloLinger + FadeOut);

        Assert.Equal(0, opacity);
    }

    [Fact]
    public void Caption_BeforeAnyAnnounce_IsEmptyAndInvisible()
    {
        var caption = _scene.Caption(DateTime.UtcNow);

        Assert.Equal(("", 0.0), caption);
    }

    [Fact]
    public void Caption_RightAfterAnnounce_ShowsTheTextFullyOpaque()
    {
        var write = WriteWindow.Around(() => _scene.Announce("Opening notepad"));

        var caption = _scene.Caption(write.Latest);

        Assert.Equal(("Opening notepad", 1.0), caption);
    }

    [Fact]
    public void Caption_JustBeforeLingerEnds_IsStillFullyOpaque()
    {
        var write = WriteWindow.Around(() => _scene.Announce("Opening notepad"));

        var caption = _scene.Caption(write.Earliest + CaptionLinger - OneMs);

        Assert.Equal(1, caption.Opacity);
    }

    [Fact]
    public void Caption_HalfwayThroughFade_IsHalfOpaque()
    {
        var write = WriteWindow.Around(() => _scene.Announce("Opening notepad"));

        var caption = _scene.Caption(write.Earliest + CaptionLinger + Half(FadeOut));

        Assert.InRange(caption.Opacity, 0.5 - WriteWindow.Epsilon, 0.5 + write.Uncertainty(FadeOut));
    }

    [Fact]
    public void Caption_AfterFade_IsInvisible()
    {
        var write = WriteWindow.Around(() => _scene.Announce("Opening notepad"));

        var caption = _scene.Caption(write.Latest + CaptionLinger + FadeOut);

        Assert.Equal(0, caption.Opacity);
    }

    [Fact]
    public void Announce_ReplacesThePreviousCaption()
    {
        _scene.Announce("Clicking");

        _scene.Announce("Typing “hi”");

        Assert.Equal("Typing “hi”", _scene.Caption(DateTime.UtcNow).Text);
    }

    [Fact]
    public void Ripple_BeforeAnyClick_IsNull()
    {
        var ripple = _scene.Ripple(DateTime.UtcNow);

        Assert.Null(ripple);
    }

    [Fact]
    public void Ripple_IsCenteredOnTheLatestClick()
    {
        _scene.Click(new Point(10, 10));

        _scene.Click(new Point(300, 200));

        Assert.Equal(new Point(300, 200), _scene.Ripple(DateTime.UtcNow)?.At);
    }

    [Fact]
    public void Ripple_RightAfterClick_StartsAtZeroProgress()
    {
        var write = WriteWindow.Around(() => _scene.Click(new Point(300, 200)));

        var ripple = _scene.Ripple(write.Latest);

        Assert.NotNull(ripple);
        Assert.InRange(ripple.Value.Progress, 0, write.Uncertainty(RippleLength));
    }

    [Fact]
    public void Ripple_HalfwayThroughItsLength_IsHalfDone()
    {
        var write = WriteWindow.Around(() => _scene.Click(new Point(300, 200)));

        var ripple = _scene.Ripple(write.Earliest + Half(RippleLength));

        Assert.NotNull(ripple);
        Assert.InRange(ripple.Value.Progress, 0.5 - write.Uncertainty(RippleLength), 0.5 + WriteWindow.Epsilon);
    }

    [Fact]
    public void Ripple_JustBeforeItsLengthEnds_IsStillShowing()
    {
        var write = WriteWindow.Around(() => _scene.Click(new Point(300, 200)));

        var ripple = _scene.Ripple(write.Earliest + RippleLength - OneMs);

        Assert.NotNull(ripple);
    }

    [Fact]
    public void Ripple_AfterItsLength_IsNull()
    {
        var write = WriteWindow.Around(() => _scene.Click(new Point(300, 200)));

        var ripple = _scene.Ripple(write.Latest + RippleLength);

        Assert.Null(ripple);
    }

    [Fact]
    public async Task ConcurrentWritesAndReads_DoNotThrow()
    {
        using var start = new ManualResetEventSlim();
        var workers = new[]
        {
            Hammer(start, i => _scene.Announce($"Typing {i}")),
            Hammer(start, i => _scene.Announce($"Clicking {i}")),
            Hammer(start, i => _scene.Click(new Point(i, i))),
            Hammer(start, i => _scene.Click(new Point(-i, i))),
            Hammer(start, _ => ReadEverything(DateTime.UtcNow)),
            Hammer(start, _ => ReadEverything(DateTime.UtcNow)),
        };

        start.Set();
        var error = await Record.ExceptionAsync(() => Task.WhenAll(workers));

        Assert.Null(error);
    }

    private static TimeSpan Half(TimeSpan span) => TimeSpan.FromTicks(span.Ticks / 2);

    /// <summary>Runs <paramref name="call"/> on a dedicated thread once <paramref name="start"/> opens.</summary>
    private static Task Hammer(ManualResetEventSlim start, Action<int> call) =>
        Task.Factory.StartNew(() =>
        {
            start.Wait();
            for (var i = 0; i < HammerIterations; i++) call(i);
        }, TaskCreationOptions.LongRunning);

    private void ReadEverything(DateTime now)
    {
        _scene.HaloOpacity(now);
        _scene.Caption(now);
        _scene.Ripple(now);
    }

    /// <summary>The clock readings either side of a scene write; the scene's own stamp lies between them.</summary>
    private sealed class WriteWindow
    {
        // Absorbs double rounding where the scene and the test convert ticks to milliseconds differently.
        public const double Epsilon = 1e-9;

        private WriteWindow(DateTime earliest, DateTime latest)
        {
            Earliest = earliest;
            Latest = latest;
        }

        public DateTime Earliest { get; }

        public DateTime Latest { get; }

        public static WriteWindow Around(Action write)
        {
            var earliest = DateTime.UtcNow;
            write();
            return new WriteWindow(earliest, DateTime.UtcNow);
        }

        /// <summary>How far a value that runs 0 to 1 over <paramref name="span"/> can move within the window.</summary>
        public double Uncertainty(TimeSpan span) => (double)(Latest - Earliest).Ticks / span.Ticks + Epsilon;
    }
}
