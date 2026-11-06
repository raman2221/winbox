namespace Winbox.Protocol;

public enum MouseButton { Left, Right, Middle }

public readonly record struct Point(int X, int Y);

public abstract record Command;

public sealed record ScreenshotCommand : Command;

public sealed record ClickCommand(Point At, MouseButton Button, int Count) : Command;

public sealed record MoveCommand(Point To) : Command;

public sealed record DragCommand(Point From, Point To) : Command;

/// <summary>Positive notches scroll up, negative scroll down.</summary>
public sealed record ScrollCommand(Point At, int Notches) : Command;

public sealed record TypeTextCommand(string Text) : Command;

/// <summary>A key combination such as "ctrl+shift+esc" or "enter".</summary>
public sealed record PressKeyCommand(string Combo) : Command;

/// <summary>Starts a program inside the guest, e.g. "notepad" or "cmd /k dir".</summary>
public sealed record RunCommand(string CommandLine) : Command;

/// <summary>
/// Tells people watching why the agent takes its next action, e.g. "Opening the Start menu".
/// The guest shows this caption in place of its own generic one for that action.
/// </summary>
public sealed record SayCommand(string Caption) : Command;

public sealed record ShutdownCommand : Command;
