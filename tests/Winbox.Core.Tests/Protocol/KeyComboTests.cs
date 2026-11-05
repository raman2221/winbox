using Winbox.Protocol;

namespace Winbox.Core.Tests.Protocol;

public class KeyComboTests
{
    private const int Ctrl = 0x11, Shift = 0x10, Alt = 0x12, Win = 0x5B;

    [Fact]
    public void Parse_ModifiersAndKey_KeepsTheWrittenOrder()
    {
        var keys = ParseToInts("ctrl+shift+esc");

        Assert.Equal([Ctrl, Shift, 0x1B], keys);
    }

    [Theory]
    [InlineData("control", Ctrl)]
    [InlineData("alt", Alt)]
    [InlineData("win", Win)]
    [InlineData("super", Win)]
    [InlineData("cmd", Win)]
    public void Parse_ModifierAlias_MapsToModifierKey(string combo, int expected)
    {
        Assert.Equal([expected], ParseToInts(combo));
    }

    [Theory]
    [InlineData("a", 0x41)]
    [InlineData("z", 0x5A)]
    [InlineData("Q", 0x51)]
    public void Parse_Letter_MapsToUppercaseVirtualKey(string combo, int expected)
    {
        Assert.Equal([expected], ParseToInts(combo));
    }

    [Theory]
    [InlineData("0", 0x30)]
    [InlineData("9", 0x39)]
    public void Parse_Digit_MapsToDigitVirtualKey(string combo, int expected)
    {
        Assert.Equal([expected], ParseToInts(combo));
    }

    [Theory]
    [InlineData("f1", 0x70)]
    [InlineData("F12", 0x7B)]
    [InlineData("f24", 0x87)]
    public void Parse_FunctionKey_MapsToFunctionVirtualKey(string combo, int expected)
    {
        Assert.Equal([expected], ParseToInts(combo));
    }

    [Fact]
    public void Parse_MixedCaseAndSpaces_IgnoresBoth()
    {
        var keys = ParseToInts("CTRL + Alt + Del");

        Assert.Equal([Ctrl, Alt, 0x2E], keys);
    }

    [Theory]
    [InlineData("f0")]
    [InlineData("f25")]
    [InlineData("fx")]
    [InlineData("banana")]
    [InlineData("ctrl+nope")]
    [InlineData("é")]
    [InlineData("٣")]
    public void Parse_UnknownKey_ThrowsProtocolException(string combo)
    {
        Assert.Throws<ProtocolException>(() => KeyCombo.Parse(combo));
    }

    [Theory]
    [InlineData("")]
    [InlineData("+")]
    [InlineData("++")]
    [InlineData(" ")]
    public void Parse_EmptyCombo_ThrowsProtocolException(string combo)
    {
        Assert.Throws<ProtocolException>(() => KeyCombo.Parse(combo));
    }

    private static int[] ParseToInts(string combo) => KeyCombo.Parse(combo).Select(key => (int)key).ToArray();
}
