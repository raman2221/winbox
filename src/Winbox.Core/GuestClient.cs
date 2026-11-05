using Winbox.Protocol;

namespace Winbox.Core;

public sealed record Screenshot(byte[] Png, int Width, int Height);

/// <summary>Sends commands to the guest helper through the shared control folder.</summary>
public sealed class GuestClient(ControlPaths paths)
{
    private static readonly TimeSpan ReplyTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(5);

    public GuestStatus? ReadStatus()
    {
        if (!File.Exists(paths.StatusFile)) return null;
        try
        {
            return GuestStatus.Decode(File.ReadAllText(paths.StatusFile));
        }
        catch (IOException)
        {
            return null; // The guest is mid-rewrite; the next heartbeat will be readable.
        }
        catch (ProtocolException)
        {
            return null; // An unreadable heartbeat means the guest can't be trusted to answer.
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
        var size = message.Split(' ');
        var png = await File.ReadAllBytesAsync(paths.ImageFile(id), cancel);
        File.Delete(paths.ImageFile(id));
        return new Screenshot(png, int.Parse(size[0]), int.Parse(size[1]));
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
        var deadline = DateTime.UtcNow + ReplyTimeout;
        var replyFile = paths.ReplyFile(id);
        while (!File.Exists(replyFile))
        {
            if (DateTime.UtcNow > deadline) throw new GuestUnavailableException();
            await Task.Delay(PollInterval, cancel);
        }
        var line = await File.ReadAllTextAsync(replyFile, cancel);
        File.Delete(replyFile);
        return Reply.Decode(line);
    }
}
