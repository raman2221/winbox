using Winbox.Protocol;

namespace Winbox.Guest.Overlay;

/// <summary>Short, human-readable captions for what the agent is about to do.</summary>
internal static class ActionCaptions
{
    private const int MaxQuotedLength = 40;

    /// <summary>Returns null for commands a watcher shouldn't be told about, like screenshots.</summary>
    public static string? Describe(Command command) => command switch
    {
        ClickCommand c => ClickVerb(c),
        MoveCommand => "Moving the pointer",
        DragCommand => "Dragging",
        ScrollCommand s => s.Notches > 0 ? "Scrolling up" : "Scrolling down",
        TypeTextCommand t => $"Typing “{Shorten(t.Text)}”",
        PressKeyCommand k => $"Pressing {k.Combo}",
        RunCommand r => $"Opening {Shorten(r.CommandLine)}",
        ShutdownCommand => "Shutting down",
        _ => null,
    };

    private static string ClickVerb(ClickCommand click) => (click.Button, click.Count) switch
    {
        (MouseButton.Right, _) => "Right-clicking",
        (MouseButton.Middle, _) => "Middle-clicking",
        (_, >= 2) => "Double-clicking",
        _ => "Clicking",
    };

    private static string Shorten(string text)
    {
        var singleLine = text.Replace("\r", "").Replace('\n', ' ');
        return singleLine.Length <= MaxQuotedLength ? singleLine : singleLine.Substring(0, MaxQuotedLength) + "…";
    }
}
