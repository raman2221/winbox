using Winbox.Guest.Motion;

namespace Winbox.Guest.Tests.Motion;

public class TypingRhythmTests
{
    // Inclusive bounds mirroring TypingRhythm's private tuning (Random.Next's upper bound is exclusive).
    private const int MinDelayMs = 25, MaxLetterDelayMs = 74, MaxWordPauseMs = 59;
    private const int Samples = 2000;

    [Theory]
    [InlineData('a')]
    [InlineData('Z')]
    [InlineData('7')]
    [InlineData('.')]
    [InlineData('\n')]
    public void DelayAfter_NonSpace_StaysWithinLetterRange(char typed)
    {
        var delays = DelaysAfter(typed);

        Assert.All(delays, delay => Assert.InRange(delay, MinDelayMs, MaxLetterDelayMs));
    }

    [Fact]
    public void DelayAfter_Space_StaysBounded()
    {
        var delays = DelaysAfter(' ');

        Assert.All(delays, delay => Assert.InRange(delay, MinDelayMs, MaxLetterDelayMs + MaxWordPauseMs));
    }

    [Fact]
    public void DelayAfter_Space_CanPauseLongerThanAnyLetter()
    {
        var delays = DelaysAfter(' ');

        Assert.Contains(delays, delay => delay > MaxLetterDelayMs);
    }

    [Fact]
    public void DelayAfter_RepeatedLetter_Varies()
    {
        var delays = DelaysAfter('e');

        Assert.True(delays.Distinct().Count() > 1, "a constant delay reads like a paste, not a person");
    }

    private static List<int> DelaysAfter(char typed)
    {
        var rhythm = new TypingRhythm(new Random(7));
        return Enumerable.Range(0, Samples).Select(_ => rhythm.DelayAfter(typed)).ToList();
    }
}
