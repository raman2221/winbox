using System.Text.RegularExpressions;

namespace Winbox.Core.Terminal;

public abstract record TerminalEvent;

/// <summary>A mouse press or release at a 1-based terminal cell.</summary>
public sealed record MouseEvent(int Button, int Column, int Row, bool Pressed) : TerminalEvent;

/// <summary>A key that maps to a combo, e.g. "enter" or "ctrl+c".</summary>
public sealed record KeyEvent(string Combo) : TerminalEvent;

public sealed record TextEvent(string Text) : TerminalEvent;

/// <summary>The terminal's answer to "CSI 16 t": how many pixels one character cell covers.</summary>
public sealed record CellSizeEvent(int Width, int Height) : TerminalEvent;

/// <summary>Turns raw VT input (keys, SGR mouse reports, size reports) into events.</summary>
public static partial class TerminalInputParser
{
    private static readonly Dictionary<string, string> EscapeKeys = new()
    {
        ["[A"] = "up", ["[B"] = "down", ["[C"] = "right", ["[D"] = "left",
        ["[H"] = "home", ["[F"] = "end", ["[2~"] = "insert", ["[3~"] = "delete",
        ["[5~"] = "pageup", ["[6~"] = "pagedown", ["[Z"] = "shift+tab",
        ["OP"] = "f1", ["OQ"] = "f2", ["OR"] = "f3", ["OS"] = "f4",
    };

    public static IEnumerable<TerminalEvent> Parse(string input)
    {
        var position = 0;
        while (position < input.Length)
        {
            var (terminalEvent, length) = ReadOne(input, position);
            if (terminalEvent is not null) yield return terminalEvent;
            position += length;
        }
    }

    private static (TerminalEvent? Event, int Length) ReadOne(string input, int position)
    {
        var ch = input[position];
        if (ch == '\u001b') return ReadEscape(input, position);
        if (ch < ' ' || ch == '\u007f') return (ControlKey(ch), 1);
        var end = position;
        while (end < input.Length && input[end] >= ' ' && input[end] != '\u007f' && input[end] != '\u001b') end++;
        return (new TextEvent(input[position..end]), end - position);
    }

    private static (TerminalEvent? Event, int Length) ReadEscape(string input, int position)
    {
        var rest = input[(position + 1)..];
        if (MouseReport().Match(rest) is { Success: true } mouse) return (ToMouseEvent(mouse), mouse.Length + 1);
        if (CellSizeReport().Match(rest) is { Success: true } size) return (ToCellSize(size), size.Length + 1);
        foreach (var (sequence, combo) in EscapeKeys)
        {
            if (rest.StartsWith(sequence, StringComparison.Ordinal)) return (new KeyEvent(combo), sequence.Length + 1);
        }
        // Unmapped sequences (e.g. ctrl+arrow) are dropped so their bytes never reach the guest as typed text.
        if (UnknownControlSequence().Match(rest) is { Success: true } unknown) return (null, unknown.Length + 1);
        return (new KeyEvent("esc"), 1);
    }

    private static TerminalEvent? ControlKey(char ch) => ch switch
    {
        '\r' or '\n' => new KeyEvent("enter"),
        '\t' => new KeyEvent("tab"),
        '\b' or '\u007f' => new KeyEvent("backspace"),
        >= '\u0001' and <= '\u001a' => new KeyEvent("ctrl+" + (char)('a' + ch - 1)),
        _ => null,
    };

    private static MouseEvent ToMouseEvent(Match match) => new(
        int.Parse(match.Groups[1].Value),
        int.Parse(match.Groups[2].Value),
        int.Parse(match.Groups[3].Value),
        match.Groups[4].Value == "M");

    private static CellSizeEvent ToCellSize(Match match) =>
        new(int.Parse(match.Groups[2].Value), int.Parse(match.Groups[1].Value));

    [GeneratedRegex(@"^\[<(\d+);(\d+);(\d+)([Mm])")]
    private static partial Regex MouseReport();

    [GeneratedRegex(@"^\[6;(\d+);(\d+)t")]
    private static partial Regex CellSizeReport();

    // ECMA-48 CSI: parameter bytes 0x30-0x3F, intermediate bytes 0x20-0x2F, one final byte 0x40-0x7E.
    [GeneratedRegex(@"^\[[0-?]*[ -/]*[@-~]")]
    private static partial Regex UnknownControlSequence();
}
