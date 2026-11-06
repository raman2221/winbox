using Winbox.Core.Terminal;
using Winbox.Protocol;

namespace Winbox.Core.Tests.Viewer;

public class TypingChunksTests
{
    [Fact]
    public void SplitText_ShortText_IsOneChunk()
    {
        var chunks = TypingChunks.SplitText("hello").ToList();

        Assert.Equal(["hello"], chunks);
    }

    [Fact]
    public void SplitText_LongText_ChunksAreAtMostTheLimit()
    {
        var chunks = TypingChunks.SplitText(new string('a', 300)).ToList();

        Assert.Equal([120, 120, 60], chunks.Select(chunk => chunk.Length));
    }

    [Fact]
    public void SplitText_LongText_ChunksJoinBackToTheOriginal()
    {
        var text = string.Concat(Enumerable.Range(0, 100).Select(i => $"line {i}; "));

        var chunks = TypingChunks.SplitText(text);

        Assert.Equal(text, string.Concat(chunks));
    }

    [Fact]
    public void SplitText_SurrogatePairAtTheLimit_MovesWholeToNextChunk()
    {
        var text = new string('a', 119) + "😀";

        var chunks = TypingChunks.SplitText(text).ToList();

        Assert.Equal([new string('a', 119), "😀"], chunks);
    }

    [Fact]
    public void SplitText_CombiningSequenceAtTheLimit_IsNotCut()
    {
        var text = new string('a', 119) + "é";

        var chunks = TypingChunks.SplitText(text).ToList();

        Assert.Equal("é", chunks[1]);
    }

    [Fact]
    public void Split_TypeTextCommand_BecomesOneCommandPerChunk()
    {
        var commands = TypingChunks.Split(new TypeTextCommand(new string('b', 250))).ToList();

        Assert.Equal(
            [new TypeTextCommand(new string('b', 120)), new TypeTextCommand(new string('b', 120)),
                new TypeTextCommand(new string('b', 10))],
            commands);
    }

    [Fact]
    public void Split_OtherCommand_PassesThroughUnchanged()
    {
        var command = new PressKeyCommand("enter");

        var commands = TypingChunks.Split(command).ToList();

        Assert.Equal([command], commands);
    }
}
