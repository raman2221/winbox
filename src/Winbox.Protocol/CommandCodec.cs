using System;
using System.Globalization;
using System.Text;

namespace Winbox.Protocol;

/// <summary>
/// Encodes commands as one text line: a verb followed by space-separated arguments.
/// Free text is base64 so it can contain spaces and newlines.
/// </summary>
public static class CommandCodec
{
    public static string Encode(Command command) => command switch
    {
        ScreenshotCommand => "screenshot",
        ClickCommand c => $"click {c.At.X} {c.At.Y} {c.Button.ToString().ToLowerInvariant()} {c.Count}",
        MoveCommand m => $"move {m.To.X} {m.To.Y}",
        DragCommand d => $"drag {d.From.X} {d.From.Y} {d.To.X} {d.To.Y}",
        ScrollCommand s => $"scroll {s.At.X} {s.At.Y} {s.Notches}",
        TypeTextCommand t => $"type {ToBase64(t.Text)}",
        // Spaces would split the line into extra arguments; "ctrl + c" means the same as "ctrl+c".
        PressKeyCommand k => $"key {k.Combo.Replace(" ", "")}",
        RunCommand r => $"run {ToBase64(r.CommandLine)}",
        SayCommand s => $"say {ToBase64(s.Caption)}",
        ShutdownCommand => "shutdown",
        _ => throw new ProtocolException($"Unknown command type {command.GetType().Name}"),
    };

    public static Command Decode(string line)
    {
        // Only strip line endings: an empty base64 argument must survive as a trailing empty field.
        var parts = line.Trim('\r', '\n').Split(' ');
        var args = new Args(parts);
        return parts[0] switch
        {
            "screenshot" => new ScreenshotCommand(),
            "click" => new ClickCommand(args.PointAt(1), args.ButtonAt(3), args.IntAt(4)),
            "move" => new MoveCommand(args.PointAt(1)),
            "drag" => new DragCommand(args.PointAt(1), args.PointAt(3)),
            "scroll" => new ScrollCommand(args.PointAt(1), args.IntAt(3)),
            "type" => new TypeTextCommand(FromBase64(args.TextAt(1))),
            "key" => new PressKeyCommand(args.TextAt(1)),
            "run" => new RunCommand(FromBase64(args.TextAt(1))),
            "say" => new SayCommand(FromBase64(args.TextAt(1))),
            "shutdown" => new ShutdownCommand(),
            _ => throw new ProtocolException($"Unknown verb '{parts[0]}'"),
        };
    }

    private static string ToBase64(string text) => Convert.ToBase64String(Encoding.UTF8.GetBytes(text));

    private static string FromBase64(string text)
    {
        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(text));
        }
        catch (FormatException)
        {
            throw new ProtocolException($"Argument '{text}' is not valid base64");
        }
    }

    private sealed class Args(string[] parts)
    {
        public string TextAt(int index) =>
            index < parts.Length
                ? parts[index]
                : throw new ProtocolException($"Missing argument {index} in '{parts[0]}'");

        public int IntAt(int index) =>
            int.TryParse(TextAt(index), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value
                : throw new ProtocolException($"Argument {index} of '{parts[0]}' is not a number");

        public Point PointAt(int index) => new(IntAt(index), IntAt(index + 1));

        public MouseButton ButtonAt(int index) =>
            // TryParse also accepts any integer, so reject values outside the enum.
            Enum.TryParse<MouseButton>(TextAt(index), ignoreCase: true, out var button)
                && Enum.IsDefined(typeof(MouseButton), button)
                ? button
                : throw new ProtocolException($"Unknown mouse button '{TextAt(index)}'");
    }
}
