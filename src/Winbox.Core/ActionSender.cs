using Winbox.Core.Terminal;
using Winbox.Protocol;

namespace Winbox.Core;

/// <summary>
/// Sends one input action to the guest as the requests it takes. Long typing goes in pieces, and when the agent
/// says why it acts, that intent goes before every piece, because the guest captions only the next action with it.
/// </summary>
public sealed class ActionSender(GuestClient guest)
{
    /// <param name="intent">Why the agent acts, shown to people watching; null for the guest's generic caption.</param>
    public async Task SendAsync(Command action, string? intent, CancellationToken cancel)
    {
        var say = IntentCaption.ToSay(intent);
        foreach (var part in TypingChunks.Split(action))
        {
            if (say is not null) await guest.SendAsync(say, cancel);
            await guest.SendAsync(part, cancel);
        }
    }
}
