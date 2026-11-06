using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Winbox.Protocol;

namespace Winbox.Core;

/// <summary>Turns an agent's note on why it acts into a caption that fits the guest's one-line caption pill.</summary>
internal static partial class IntentCaption
{
    public const int MaxLength = 60;
    private const string Ellipsis = "…";

    /// <summary>Returns null when the intent has nothing to show.</summary>
    public static SayCommand? ToSay(string? intent)
    {
        var caption = Shorten(SingleLine(intent ?? ""));
        return caption.Length > 0 ? new SayCommand(caption) : null;
    }

    private static string SingleLine(string text) => LineBreaksAndControls().Replace(text, " ").Trim();

    private static string Shorten(string text)
    {
        if (text.Length <= MaxLength) return text;
        var kept = new StringBuilder();
        var elements = StringInfo.GetTextElementEnumerator(text);
        // Whole text elements only, so an emoji or accented letter is never cut in half.
        while (elements.MoveNext() && kept.Length + elements.GetTextElement().Length < MaxLength)
            kept.Append(elements.GetTextElement());
        return kept.ToString().TrimEnd() + Ellipsis;
    }

    // Line breaks, tabs and other control characters would break the pill's single line of text.
    [GeneratedRegex(@"[\s\p{Cc}]+")]
    private static partial Regex LineBreaksAndControls();
}
