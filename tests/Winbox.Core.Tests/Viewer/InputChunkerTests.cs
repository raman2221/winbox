using Winbox.Core.Terminal;

namespace Winbox.Core.Tests.Viewer;

public class InputChunkerTests
{
    [Fact]
    public void Feed_PlainText_PassesThrough()
    {
        var chunker = new InputChunker();

        Assert.Equal("hello", chunker.Feed("hello"));
    }

    [Fact]
    public void Feed_CompleteMouseReport_PassesThrough()
    {
        var chunker = new InputChunker();

        Assert.Equal("\u001b[<0;3;2M", chunker.Feed("\u001b[<0;3;2M"));
    }

    [Fact]
    public void Feed_MouseReportCutInHalf_HoldsBackTheUnfinishedPart()
    {
        var chunker = new InputChunker();

        Assert.Equal("ab", chunker.Feed("ab\u001b[<0;3"));
    }

    [Fact]
    public void Feed_RestOfSplitReport_ReturnsItJoined()
    {
        var chunker = new InputChunker();
        chunker.Feed("ab\u001b[<0;3");

        Assert.Equal("\u001b[<0;3;2M", chunker.Feed(";2M"));
    }

    [Fact]
    public void Feed_LoneEscape_PassesThroughAsEscKey()
    {
        var chunker = new InputChunker();

        Assert.Equal("\u001b", chunker.Feed("\u001b"));
    }

    [Fact]
    public void Feed_UnfinishedSs3_IsHeldBack()
    {
        var chunker = new InputChunker();

        Assert.Equal("", chunker.Feed("\u001bO"));
    }

    [Fact]
    public void Feed_RunawaySequence_IsReleasedInsteadOfHeldForever()
    {
        var chunker = new InputChunker();
        var runaway = "\u001b[" + new string('1', 40);

        Assert.Equal(runaway, chunker.Feed(runaway));
    }
}
