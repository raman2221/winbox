namespace Winbox.Core.Terminal;

/// <summary>
/// Joins raw input reads so an escape sequence split across two reads (e.g. a mouse report cut
/// in half) reaches the parser whole instead of turning into a stray Esc key plus typed text.
/// </summary>
public sealed class InputChunker
{
    private const char Escape = '\u001b';
    private const int LongestHeldSequence = 32;

    private string held = "";

    /// <summary>Returns the text that is safe to parse now, keeping back an unfinished trailing sequence.</summary>
    public string Feed(string chunk)
    {
        var text = held + chunk;
        var start = text.LastIndexOf(Escape);
        var keepBack = start >= 0 && IsUnfinished(text.AsSpan(start));
        held = keepBack ? text[start..] : "";
        return keepBack ? text[..start] : text;
    }

    // A lone trailing Esc is the Esc key itself, so it is never held back.
    private static bool IsUnfinished(ReadOnlySpan<char> sequence)
    {
        if (sequence.Length < 2 || sequence.Length > LongestHeldSequence) return false;
        if (sequence[1] == 'O') return sequence.Length == 2;
        return sequence[1] == '[' && !HasFinalByte(sequence[2..]);
    }

    // CSI parameter and intermediate bytes are 0x20-0x3F; the first byte at 0x40-0x7E ends it.
    private static bool HasFinalByte(ReadOnlySpan<char> body)
    {
        foreach (var ch in body)
        {
            if (ch is >= '@' and <= '~') return true;
        }
        return false;
    }
}
