using Winbox.Protocol;

namespace Winbox.Core.Tests.Protocol;

public class CommandCodecTests
{
    public static TheoryData<Command> EveryCommandType => new()
    {
        new ScreenshotCommand(),
        new ClickCommand(new Point(10, 20), MouseButton.Left, 1),
        new ClickCommand(new Point(0, 0), MouseButton.Right, 2),
        new ClickCommand(new Point(-5, 7), MouseButton.Middle, 3),
        new MoveCommand(new Point(1919, 1079)),
        new DragCommand(new Point(1, 2), new Point(300, 400)),
        new ScrollCommand(new Point(50, 60), -3),
        new ScrollCommand(new Point(50, 60), 5),
        new TypeTextCommand("hello world"),
        new TypeTextCommand("line one\nline two\r\n\ttabbed"),
        new TypeTextCommand("héllo 世界 🚀"),
        new TypeTextCommand(""),
        new PressKeyCommand("ctrl+shift+esc"),
        new RunCommand(@"cmd /k dir C:\"),
        new RunCommand("notepad \"C:\\some file.txt\"\nécho ✓"),
        new ShutdownCommand(),
    };

    [Theory]
    [MemberData(nameof(EveryCommandType))]
    public void Decode_OfEncodedCommand_ReturnsEqualCommand(Command command)
    {
        var line = CommandCodec.Encode(command);

        var decoded = CommandCodec.Decode(line);

        Assert.Equal(command, decoded);
    }

    [Fact]
    public void Encode_TextWithSpacesAndNewlines_ProducesOneLineWithTwoFields()
    {
        var command = new TypeTextCommand("a b\nc d");

        var line = CommandCodec.Encode(command);

        Assert.DoesNotContain('\n', line);
        Assert.Equal(2, line.Split(' ').Length);
    }

    [Fact]
    public void Decode_LineWithTrailingNewline_IgnoresTheNewline()
    {
        var decoded = CommandCodec.Decode("move 3 4\r\n");

        Assert.Equal(new MoveCommand(new Point(3, 4)), decoded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("jump 1 2")]
    [InlineData("SCREENSHOT")]
    public void Decode_UnknownVerb_ThrowsProtocolException(string line)
    {
        Assert.Throws<ProtocolException>(() => CommandCodec.Decode(line));
    }

    [Theory]
    [InlineData("click 1 2 left")]
    [InlineData("move 10")]
    [InlineData("drag 1 2 3")]
    [InlineData("scroll 1 2")]
    [InlineData("type")]
    [InlineData("key")]
    [InlineData("run")]
    public void Decode_MissingArgument_ThrowsProtocolException(string line)
    {
        Assert.Throws<ProtocolException>(() => CommandCodec.Decode(line));
    }

    [Theory]
    [InlineData("click x 2 left 1")]
    [InlineData("click 1 2 left once")]
    [InlineData("scroll 1 2 lots")]
    [InlineData("move 1.5 2")]
    [InlineData("move 99999999999 1")]
    public void Decode_BadNumber_ThrowsProtocolException(string line)
    {
        Assert.Throws<ProtocolException>(() => CommandCodec.Decode(line));
    }

    [Theory]
    [InlineData("click 1 2 thumb 1")]
    [InlineData("click 1 2 5 1")]
    public void Decode_BadMouseButton_ThrowsProtocolException(string line)
    {
        Assert.Throws<ProtocolException>(() => CommandCodec.Decode(line));
    }

    [Theory]
    [InlineData("type not*base64")]
    [InlineData("run abc")]
    public void Decode_TextThatIsNotBase64_ThrowsProtocolException(string line)
    {
        Assert.Throws<ProtocolException>(() => CommandCodec.Decode(line));
    }

    [Fact]
    public void Encode_UnknownCommandType_ThrowsProtocolException()
    {
        Assert.Throws<ProtocolException>(() => CommandCodec.Encode(new UnknownCommand()));
    }

    private sealed record UnknownCommand : Command;
}
