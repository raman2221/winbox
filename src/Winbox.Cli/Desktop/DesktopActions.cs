using System.Globalization;
using System.Text;
using Winbox.Core;
using Winbox.Protocol;

namespace Winbox.Cli.Desktop;

/// <summary>Performs input actions on the guest desktop and reports what was done.</summary>
internal sealed class DesktopActions(GuestClient guest)
{
    // The guest types at a human pace (about 50 ms a key), so long text is sent in pieces
    // that each finish well within the guest client's reply timeout.
    private const int MaxCharsPerRequest = 120;

    public async Task<string> PerformAsync(Command command, CancellationToken cancel)
    {
        foreach (var part in SplitForPacing(command)) await guest.SendAsync(part, cancel);
        return ActionText.Describe(command);
    }

    private static IEnumerable<Command> SplitForPacing(Command command) =>
        command is TypeTextCommand typing
            ? Chunk(typing.Text).Select(text => new TypeTextCommand(text))
            : [command];

    private static IEnumerable<string> Chunk(string text)
    {
        var elements = StringInfo.GetTextElementEnumerator(text);
        var chunk = new StringBuilder();
        while (elements.MoveNext())
        {
            chunk.Append(elements.GetTextElement());
            if (chunk.Length < MaxCharsPerRequest) continue;
            yield return chunk.ToString();
            chunk.Clear();
        }
        if (chunk.Length > 0) yield return chunk.ToString();
    }
}
