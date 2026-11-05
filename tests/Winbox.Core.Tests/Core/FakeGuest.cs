using Winbox.Protocol;

namespace Winbox.Core.Tests.Core;

/// <summary>Plays the guest helper's side of the shared-folder protocol for one request at a time.</summary>
internal sealed class FakeGuest(ControlPaths paths)
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);

    public void WriteHeartbeat(DateTimeOffset writtenAt) =>
        ControlPaths.WriteAtomically(paths.StatusFile, new GuestStatus(writtenAt, 1280, 720, "test").Encode());

    /// <summary>Waits for the next request, answers it, and returns the command the host sent.</summary>
    public async Task<Command> AnswerNextAsync(Reply reply, byte[]? png = null)
    {
        var request = await WaitForRequestAsync();
        var id = Path.GetFileNameWithoutExtension(request);
        var command = CommandCodec.Decode(await File.ReadAllTextAsync(request));
        File.Delete(request);
        // The real guest saves the image before the reply, so the host never reads a missing PNG.
        if (png is not null) await File.WriteAllBytesAsync(paths.ImageFile(id), png);
        ControlPaths.WriteAtomically(paths.ReplyFile(id), reply.Encode());
        return command;
    }

    private async Task<string> WaitForRequestAsync()
    {
        var deadline = DateTime.UtcNow + RequestTimeout;
        while (DateTime.UtcNow < deadline)
        {
            var request = Directory.GetFiles(paths.RequestsDir).FirstOrDefault(file => file.EndsWith(".cmd"));
            if (request is not null) return request;
            await Task.Delay(5);
        }
        throw new TimeoutException("The host never wrote a request.");
    }
}
