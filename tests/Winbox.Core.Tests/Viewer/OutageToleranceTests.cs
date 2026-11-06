using Winbox.Core.Terminal;

namespace Winbox.Core.Tests.Viewer;

public class OutageToleranceTests
{
    [Fact]
    public void RecordMiss_FewerMissesThanTheLimit_DoesNotThrow()
    {
        var tolerance = new OutageTolerance(3);

        tolerance.RecordMiss(new GuestUnavailableException());
        var error = Record.Exception(() => tolerance.RecordMiss(new GuestUnavailableException()));

        Assert.Null(error);
    }

    [Fact]
    public void RecordMiss_ReachingTheLimit_RethrowsThatMiss()
    {
        var tolerance = new OutageTolerance(3);
        var third = new GuestUnavailableException();
        tolerance.RecordMiss(new GuestUnavailableException());
        tolerance.RecordMiss(new GuestUnavailableException());

        var error = Record.Exception(() => tolerance.RecordMiss(third));

        Assert.Same(third, error);
    }

    [Fact]
    public void RecordSuccess_BetweenMisses_StartsTheCountOver()
    {
        var tolerance = new OutageTolerance(3);
        tolerance.RecordMiss(new GuestUnavailableException());
        tolerance.RecordMiss(new GuestUnavailableException());
        tolerance.RecordSuccess();

        var error = Record.Exception(() => tolerance.RecordMiss(new GuestUnavailableException()));

        Assert.Null(error);
    }
}
