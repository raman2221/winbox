using System.Threading.Channels;
using Winbox.Core;
using Winbox.Core.Terminal;
using Winbox.Protocol;

namespace Winbox.Cli.Viewer;

/// <summary>
/// Sends input commands to the guest one at a time, in the order they were typed or clicked,
/// on its own task so a slow round trip never stalls the screen updates. A failed command is
/// reported on the status line and dropped; it never ends the viewer.
/// </summary>
internal sealed class CommandQueue(GuestClient guest, ViewerState state)
{
    private const string DroppedNotice = "guest not responding, input dropped";

    private readonly Channel<Command> pending = Channel.CreateUnbounded<Command>(
        new UnboundedChannelOptions { SingleReader = true });

    public void Enqueue(Command command)
    {
        foreach (var part in TypingChunks.Split(command)) pending.Writer.TryWrite(part);
    }

    public async Task RunAsync(CancellationToken cancel)
    {
        await foreach (var command in pending.Reader.ReadAllAsync(cancel))
        {
            await SendAsync(command, cancel);
        }
    }

    private async Task SendAsync(Command command, CancellationToken cancel)
    {
        try
        {
            await guest.SendAsync(command, cancel);
        }
        catch (GuestUnavailableException)
        {
            // Replaying stale input later would be worse than losing it; the renderer decides
            // whether the guest is gone for good.
            state.ShowNotice(DroppedNotice);
        }
        catch (WinboxException refused)
        {
            state.ShowNotice(refused.Message);
        }
    }
}
