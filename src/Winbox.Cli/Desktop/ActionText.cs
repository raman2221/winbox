using Winbox.Protocol;

namespace Winbox.Cli.Desktop;

/// <summary>The one-line confirmation shown after an input action, shared by the CLI and the MCP tools.</summary>
internal static class ActionText
{
    public static string Describe(Command command) => command switch
    {
        ClickCommand c => $"Clicked {At(c.At)}{ButtonSuffix(c.Button)}{CountSuffix(c.Count)}.",
        MoveCommand m => $"Moved the mouse to {At(m.To)}.",
        DragCommand d => $"Dragged from {At(d.From)} to {At(d.To)}.",
        ScrollCommand s => $"Scrolled {Direction(s.Notches)} {Math.Abs(s.Notches)} notch(es) at {At(s.At)}.",
        TypeTextCommand t => $"Typed {t.Text.Length} character(s).",
        PressKeyCommand k => $"Pressed {k.Combo}.",
        RunCommand r => $"Started \"{r.CommandLine}\".",
        _ => throw new ArgumentOutOfRangeException(nameof(command), command, "Not an input action."),
    };

    private static string At(Point point) => $"({point.X}, {point.Y})";

    private static string ButtonSuffix(MouseButton button) =>
        button == MouseButton.Left ? "" : $" with the {button.ToString().ToLowerInvariant()} button";

    private static string CountSuffix(int count) => count == 1 ? "" : $" {count} times";

    private static string Direction(int notches) => notches >= 0 ? "up" : "down";
}
