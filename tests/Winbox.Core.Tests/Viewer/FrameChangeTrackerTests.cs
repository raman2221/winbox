using System.Drawing;
using Winbox.Core.Terminal;

namespace Winbox.Core.Tests.Viewer;

public class FrameChangeTrackerTests
{
    private static readonly Size Box = new(800, 600);

    [Fact]
    public void IsNew_FirstFrame_IsTrue()
    {
        var tracker = new FrameChangeTracker();

        Assert.True(tracker.IsNew([1, 2, 3], Box));
    }

    [Fact]
    public void IsNew_SameBytesAndBox_IsFalse()
    {
        var tracker = new FrameChangeTracker();
        tracker.IsNew([1, 2, 3], Box);

        Assert.False(tracker.IsNew([1, 2, 3], Box));
    }

    [Fact]
    public void IsNew_DifferentBytes_IsTrue()
    {
        var tracker = new FrameChangeTracker();
        tracker.IsNew([1, 2, 3], Box);

        Assert.True(tracker.IsNew([1, 2, 4], Box));
    }

    [Fact]
    public void IsNew_SameBytesInResizedBox_IsTrue()
    {
        var tracker = new FrameChangeTracker();
        tracker.IsNew([1, 2, 3], Box);

        Assert.True(tracker.IsNew([1, 2, 3], new Size(640, 480)));
    }
}
