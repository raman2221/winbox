using Winbox.Protocol;

namespace Winbox.Core.Tests.Core;

public class IntentCaptionTests
{
    [Fact]
    public void ToSay_ShortIntent_KeepsItAsIs()
    {
        var say = IntentCaption.ToSay("Opening the Start menu");

        Assert.Equal(new SayCommand("Opening the Start menu"), say);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\r\n\t\0")]
    public void ToSay_BlankIntent_ReturnsNull(string? intent)
    {
        Assert.Null(IntentCaption.ToSay(intent));
    }

    [Theory]
    [InlineData("Opening\r\nthe   Start\tmenu")]
    [InlineData(" Opening the\u2028Start menu\n")]
    [InlineData("Opening\0the\u001bStart menu")]
    public void ToSay_LineBreaksAndControlCharacters_BecomeSingleSpaces(string intent)
    {
        var say = IntentCaption.ToSay(intent);

        Assert.Equal(new SayCommand("Opening the Start menu"), say);
    }

    [Fact]
    public void ToSay_IntentOfExactlyMaxLength_IsNotShortened()
    {
        var intent = new string('a', IntentCaption.MaxLength);

        var say = IntentCaption.ToSay(intent);

        Assert.Equal(intent, say?.Caption);
    }

    [Fact]
    public void ToSay_LongIntent_IsCutToMaxLengthEndingInEllipsis()
    {
        var say = IntentCaption.ToSay(new string('a', 100));

        Assert.Equal(new string('a', IntentCaption.MaxLength - 1) + "…", say?.Caption);
    }

    [Fact]
    public void ToSay_LongIntentOfEmoji_KeepsEveryEmojiWhole()
    {
        var intent = string.Concat(Enumerable.Repeat("🚀", 40));

        var say = IntentCaption.ToSay(intent);

        // Each rocket is two UTF-16 chars, so 29 of them plus the ellipsis is the most that fits in 60.
        Assert.Equal(string.Concat(Enumerable.Repeat("🚀", 29)) + "…", say?.Caption);
    }
}
