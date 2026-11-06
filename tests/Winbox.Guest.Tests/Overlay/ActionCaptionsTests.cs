using Winbox.Guest.Overlay;
using Winbox.Protocol;

namespace Winbox.Guest.Tests.Overlay;

public class ActionCaptionsTests
{
    private static readonly Point Anywhere = new(10, 20);
    private static readonly string FortyChars = new('a', 40);

    public static TheoryData<Command, string> CaptionedCommands => new()
    {
        { new ClickCommand(Anywhere, MouseButton.Left, 1), "Clicking" },
        { new MoveCommand(Anywhere), "Moving the pointer" },
        { new DragCommand(Anywhere, new Point(30, 40)), "Dragging" },
        { new ScrollCommand(Anywhere, 1), "Scrolling up" },
        { new TypeTextCommand("hello"), "Typing “hello”" },
        { new PressKeyCommand("ctrl+s"), "Pressing ctrl+s" },
        { new RunCommand("notepad"), "Opening notepad" },
        { new ShutdownCommand(), "Shutting down" },
    };

    public static TheoryData<Command, string> OverlongTextCommands => new()
    {
        { new TypeTextCommand(FortyChars + "b"), $"Typing “{FortyChars}…”" },
        { new RunCommand(FortyChars + " --flag"), $"Opening {FortyChars}…" },
    };

    [Theory]
    [MemberData(nameof(CaptionedCommands))]
    public void Describe_EachCommandType_ReturnsItsCaption(Command command, string expected)
    {
        var caption = ActionCaptions.Describe(command);

        Assert.Equal(expected, caption);
    }

    [Theory]
    [InlineData(MouseButton.Right, 1, "Right-clicking")]
    [InlineData(MouseButton.Right, 2, "Right-clicking")]
    [InlineData(MouseButton.Middle, 1, "Middle-clicking")]
    [InlineData(MouseButton.Left, 2, "Double-clicking")]
    public void Describe_Click_NamesTheButtonOrDoubleClick(MouseButton button, int count, string expected)
    {
        var caption = ActionCaptions.Describe(new ClickCommand(Anywhere, button, count));

        Assert.Equal(expected, caption);
    }

    [Theory]
    [InlineData(3, "Scrolling up")]
    [InlineData(-3, "Scrolling down")]
    public void Describe_Scroll_NamesTheDirection(int notches, string expected)
    {
        var caption = ActionCaptions.Describe(new ScrollCommand(Anywhere, notches));

        Assert.Equal(expected, caption);
    }

    [Theory]
    [MemberData(nameof(OverlongTextCommands))]
    public void Describe_TextOver40Characters_IsShortenedWithEllipsis(Command command, string expected)
    {
        var caption = ActionCaptions.Describe(command);

        Assert.Equal(expected, caption);
    }

    [Fact]
    public void Describe_TextOfExactly40Characters_IsNotShortened()
    {
        var caption = ActionCaptions.Describe(new TypeTextCommand(FortyChars));

        Assert.Equal($"Typing “{FortyChars}”", caption);
    }

    [Theory]
    [InlineData("line one\nline two")]
    [InlineData("line one\r\nline two")]
    public void Describe_MultilineText_IsFlattenedToOneLine(string text)
    {
        var caption = ActionCaptions.Describe(new TypeTextCommand(text));

        Assert.Equal("Typing “line one line two”", caption);
    }

    [Fact]
    public void Describe_Screenshot_ReturnsNull()
    {
        var caption = ActionCaptions.Describe(new ScreenshotCommand());

        Assert.Null(caption);
    }
}
