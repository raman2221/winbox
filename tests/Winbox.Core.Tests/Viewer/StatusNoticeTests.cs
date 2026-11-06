using Winbox.Core.Terminal;

namespace Winbox.Core.Tests.Viewer;

public class StatusNoticeTests
{
    private static readonly DateTimeOffset Posted = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan ShownFor = TimeSpan.FromSeconds(4);

    [Fact]
    public void CurrentAt_NothingPosted_IsNull()
    {
        var notice = new StatusNotice(ShownFor);

        Assert.Null(notice.CurrentAt(Posted));
    }

    [Fact]
    public void CurrentAt_WithinDuration_ShowsMessage()
    {
        var notice = new StatusNotice(ShownFor);
        notice.Post("refused", Posted);

        Assert.Equal("refused", notice.CurrentAt(Posted + TimeSpan.FromSeconds(3)));
    }

    [Fact]
    public void CurrentAt_AfterDuration_IsNull()
    {
        var notice = new StatusNotice(ShownFor);
        notice.Post("refused", Posted);

        Assert.Null(notice.CurrentAt(Posted + ShownFor));
    }

    [Fact]
    public void Post_NewerMessage_ReplacesOlderAndRestartsTimer()
    {
        var notice = new StatusNotice(ShownFor);
        notice.Post("first", Posted);
        notice.Post("second", Posted + TimeSpan.FromSeconds(3));

        Assert.Equal("second", notice.CurrentAt(Posted + TimeSpan.FromSeconds(6)));
    }
}
