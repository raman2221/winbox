using System;

namespace Winbox.Guest.Motion;

/// <summary>Per-keystroke pauses so typing reads like a person rather than a paste.</summary>
internal sealed class TypingRhythm(Random random)
{
    private const int MinDelayMs = 25, MaxDelayMs = 75, WordPauseMs = 60;

    public int DelayAfter(char typed) =>
        random.Next(MinDelayMs, MaxDelayMs) + (typed == ' ' ? random.Next(WordPauseMs) : 0);
}
