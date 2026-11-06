using Winbox.Protocol;

namespace Winbox.Core;

public sealed record Screenshot(byte[] Png, int Width, int Height);

/// <summary>Sends commands to the guest helper through the shared control folder.</summary>
public sealed class GuestClient(ControlPaths paths)
{
    private static readonly TimeSpan ReplyTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(5);
    private static readonly TimeSpan LivenessCheckInterval = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan StatusRetryDelay = TimeSpan.FromMilliseconds(5);
    private const int StatusReadAttempts = 3;

    /// <summary>
    /// Retries briefly because the guest's replace of the status file leaves a sub-millisecond gap; reading once
    /// wrongly reported a live guest as dead about once per thousand checks.
    /// </summary>
    public GuestStatus? ReadStatus()
    {
        for (var attempt = 1; attempt <= StatusReadAttempts; attempt++)
        {
            if (TryReadStatusOnce() is { } status) return status;
            if (attempt < StatusReadAttempts) Thread.Sleep(StatusRetryDelay);
        }
        return null;
    }

    private GuestStatus? TryReadStatusOnce()
    {
        try
        {
            return File.Exists(paths.StatusFile) ? GuestStatus.Decode(ControlPaths.ReadShared(paths.StatusFile)) : null;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ProtocolException)
        {
            return null; // Mid-rewrite or corrupt: either way this heartbeat can't vouch for the guest.
        }
    }

    public bool IsGuestAlive() => ReadStatus()?.IsAliveAt(DateTimeOffset.UtcNow) ?? false;

    public async Task<string> SendAsync(Command command, CancellationToken cancel = default)
    {
        var (_, message) = await RoundTripAsync(command, cancel);
        return message;
    }

    public async Task<Screenshot> ScreenshotAsync(CancellationToken cancel = default)
    {
        var (id, message) = await RoundTripAsync(new ScreenshotCommand(), cancel);
        try
        {
            var size = message.Split(' ');
            var png = await File.ReadAllBytesAsync(paths.ImageFile(id), cancel);
            return new Screenshot(png, int.Parse(size[0]), int.Parse(size[1]));
        }
        finally
        {
            TryDelete(paths.ImageFile(id));
        }
    }

    private async Task<(string Id, string Message)> RoundTripAsync(Command command, CancellationToken cancel)
    {
        if (!IsGuestAlive()) throw new GuestUnavailableException();
        var id = Guid.NewGuid().ToString("N");
        ControlPaths.WriteAtomically(paths.RequestFile(id), CommandCodec.Encode(command));
        var reply = await WaitForReplyAsync(id, cancel);
        if (!reply.Ok) throw new WinboxException($"The guest refused the command: {reply.Message}");
        return (id, reply.Message);
    }

    private async Task<Reply> WaitForReplyAsync(string id, CancellationToken cancel)
    {
        try
        {
            await WaitUntilReplyExistsAsync(id, cancel);
        }
        catch
        {
            // A request nobody is waiting for must never run later, e.g. a click replayed minutes after a timeout.
            TryDelete(paths.RequestFile(id));
            throw;
        }
        return ReadAndDeleteReply(id);
    }

    private async Task WaitUntilReplyExistsAsync(string id, CancellationToken cancel)
    {
        var started = DateTime.UtcNow;
        var lastLivenessCheck = started;
        while (!File.Exists(paths.ReplyFile(id)))
        {
            var now = DateTime.UtcNow;
            if (now - started > ReplyTimeout) throw new GuestUnavailableException();
            if (now - lastLivenessCheck > LivenessCheckInterval)
            {
                if (!IsGuestAlive()) throw new GuestUnavailableException();
                lastLivenessCheck = now;
            }
            await Task.Delay(PollInterval, cancel);
        }
    }

    private Reply ReadAndDeleteReply(string id)
    {
        var replyFile = paths.ReplyFile(id);
        var line = ControlPaths.ReadShared(replyFile);
        TryDelete(replyFile);
        return Reply.Decode(line);
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            // Leftovers are harmless: the guest drops stale requests and `winbox start` clears the folder.
        }
    }
}
