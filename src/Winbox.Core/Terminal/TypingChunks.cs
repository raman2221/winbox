using System.Globalization;
using System.Text;
using Winbox.Protocol;

namespace Winbox.Core.Terminal;

/// <summary>
/// Splits typed text into short requests. The guest types at a human pace (about 50 ms a key),
/// so a long paste sent whole would outlast the guest client's reply timeout.
/// </summary>
public static class TypingChunks
{
    public const int MaxChars = 120;

    /// <summary>Splits a <see cref="TypeTextCommand"/> into paced pieces; any other command passes through.</summary>
    public static IEnumerable<Command> Split(Command command) =>
        command is TypeTextCommand typing
            ? SplitText(typing.Text).Select(text => new TypeTextCommand(text))
            : [command];

    /// <summary>
    /// Cuts only between text elements so no surrogate pair or combining sequence is broken.
    /// A single element longer than the limit is kept whole rather than corrupted.
    /// </summary>
    public static IEnumerable<string> SplitText(string text)
    {
        var elements = StringInfo.GetTextElementEnumerator(text);
        var chunk = new StringBuilder();
        while (elements.MoveNext())
        {
            var element = elements.GetTextElement();
            if (chunk.Length > 0 && chunk.Length + element.Length > MaxChars)
            {
                yield return chunk.ToString();
                chunk.Clear();
            }
            chunk.Append(element);
        }
        if (chunk.Length > 0) yield return chunk.ToString();
    }
}
