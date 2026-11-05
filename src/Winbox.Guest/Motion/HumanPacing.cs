using System;

namespace Winbox.Guest.Motion;

/// <summary>The timing models that make agent input look hand-driven.</summary>
internal sealed record HumanPacing(MotionPlanner Motion, TypingRhythm Rhythm)
{
    public static HumanPacing Create()
    {
        var random = new Random();
        return new HumanPacing(new MotionPlanner(random), new TypingRhythm(random));
    }
}
