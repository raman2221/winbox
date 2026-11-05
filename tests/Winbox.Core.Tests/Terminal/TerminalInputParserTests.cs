using Winbox.Core.Terminal;

namespace Winbox.Core.Tests.Terminal;

public class TerminalInputParserTests
{
    private const string Esc = "\u001b";

    [Theory]
    [InlineData(Esc + "[<0;10;5M", 0, 10, 5, true)]
    [InlineData(Esc + "[<0;10;5m", 0, 10, 5, false)]
    [InlineData(Esc + "[<2;120;48M", 2, 120, 48, true)]
    public void Parse_SgrMouseReport_ReturnsMouseEvent(string input, int button, int column, int row, bool pressed)
    {
        var events = TerminalInputParser.Parse(input);

        Assert.Equal([new MouseEvent(button, column, row, pressed)], events);
    }

    [Theory]
    [InlineData(Esc + "[A", "up")]
    [InlineData(Esc + "[B", "down")]
    [InlineData(Esc + "[C", "right")]
    [InlineData(Esc + "[D", "left")]
    [InlineData(Esc + "[3~", "delete")]
    [InlineData(Esc + "[Z", "shift+tab")]
    [InlineData(Esc + "OP", "f1")]
    public void Parse_EscapeKeySequence_ReturnsKeyEvent(string input, string combo)
    {
        Assert.Equal([new KeyEvent(combo)], TerminalInputParser.Parse(input));
    }

    [Theory]
    [InlineData("\r", "enter")]
    [InlineData("\n", "enter")]
    [InlineData("\u007f", "backspace")]
    [InlineData("\b", "backspace")]
    [InlineData("\t", "tab")]
    public void Parse_EditingControlCharacter_ReturnsNamedKey(string input, string combo)
    {
        Assert.Equal([new KeyEvent(combo)], TerminalInputParser.Parse(input));
    }

    [Theory]
    [InlineData("\u0001", "ctrl+a")]
    [InlineData("\u0003", "ctrl+c")]
    [InlineData("\u001a", "ctrl+z")]
    public void Parse_ControlLetter_ReturnsCtrlCombo(string input, string combo)
    {
        Assert.Equal([new KeyEvent(combo)], TerminalInputParser.Parse(input));
    }

    [Fact]
    public void Parse_TextAroundEscapeSequence_SplitsIntoTextKeyText()
    {
        var events = TerminalInputParser.Parse("ab" + Esc + "[Acd\r");

        TerminalEvent[] expected =
            [new TextEvent("ab"), new KeyEvent("up"), new TextEvent("cd"), new KeyEvent("enter")];
        Assert.Equal(expected, events);
    }

    [Fact]
    public void Parse_UnicodeText_ReturnsSingleTextEvent()
    {
        Assert.Equal([new TextEvent("héllo 世界 🚀")], TerminalInputParser.Parse("héllo 世界 🚀"));
    }

    [Fact]
    public void Parse_CellSizeReport_ReturnsWidthAndHeight()
    {
        var events = TerminalInputParser.Parse(Esc + "[6;20;10t");

        Assert.Equal([new CellSizeEvent(Width: 10, Height: 20)], events);
    }

    [Fact]
    public void Parse_LoneEscape_ReturnsEscKey()
    {
        Assert.Equal([new KeyEvent("esc")], TerminalInputParser.Parse(Esc));
    }

    [Fact]
    public void Parse_UnrecognizedCsiSequence_IsDroppedInsteadOfTyped()
    {
        var events = TerminalInputParser.Parse("x" + Esc + "[1;5Ay");

        Assert.Equal([new TextEvent("x"), new TextEvent("y")], events);
    }
}
