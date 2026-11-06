namespace Winbox.Core.Terminal;

/// <summary>
/// A short message for the viewer's status line (e.g. a command the guest refused) that
/// disappears on its own after a few seconds. Safe to post from one thread and read from another.
/// </summary>
public sealed class StatusNotice(TimeSpan shownFor)
{
    private readonly Lock gate = new();
    private string? text;
    private DateTimeOffset expiresAt;

    public void Post(string message, DateTimeOffset now)
    {
        lock (gate)
        {
            text = message;
            expiresAt = now + shownFor;
        }
    }

    /// <summary>The message still on display at <paramref name="now"/>, or null once it has expired.</summary>
    public string? CurrentAt(DateTimeOffset now)
    {
        lock (gate) return now < expiresAt ? text : null;
    }
}
