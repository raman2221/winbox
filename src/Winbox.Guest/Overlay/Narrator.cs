using Winbox.Protocol;

namespace Winbox.Guest.Overlay;

/// <summary>
/// Picks the caption for each command: the agent's own words when a say command explained the action just
/// before it, otherwise a generic description. Used from the command thread only.
/// </summary>
internal sealed class Narrator
{
    private string? _intent;

    /// <summary>Returns null for commands a watcher shouldn't be told about, like screenshots.</summary>
    public string? CaptionFor(Command command) => command switch
    {
        SayCommand say => Remember(say.Caption),
        // A screenshot between a say and its action must not use up the intent.
        ScreenshotCommand => null,
        _ => TakeIntent() ?? ActionCaptions.Describe(command),
    };

    private string? Remember(string caption)
    {
        _intent = string.IsNullOrWhiteSpace(caption) ? null : caption;
        return _intent;
    }

    private string? TakeIntent()
    {
        var intent = _intent;
        _intent = null;
        return intent;
    }
}
