using System.Threading.Channels;
using Winbox.Core;
using Winbox.Protocol;

namespace Winbox.Cli.Viewer;

/// <summary>
/// Sends input commands to the guest one at a time, in the order they were typed or clicked,
/// on its own task so a slow round trip never stalls the screen updates.
/// </summary>
internal sealed class CommandQueue(GuestClient guest)
{
    private readonly Channel<Command> pending = Channel.CreateUnbounded<Command>(
        new UnboundedChannelOptions { SingleReader = true });

    public void Enqueue(Command command) => pending.Writer.TryWrite(command);

    public async Task RunAsync(CancellationToken cancel)
    {
        await foreach (var command in pending.Reader.ReadAllAsync(cancel))
        {
            await guest.SendAsync(command, cancel);
        }
    }
}
