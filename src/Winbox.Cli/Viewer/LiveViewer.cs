namespace Winbox.Cli.Viewer;

/// <summary>
/// Runs rendering, input and command sending side by side. When any of them stops (Ctrl+Q,
/// cancellation or a guest error) the others are cancelled, and a real error is rethrown.
/// </summary>
internal sealed class LiveViewer(FrameRenderer renderer, InputDispatcher input, CommandQueue commands)
{
    public async Task RunAsync(CancellationToken cancel)
    {
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancel);
        Task[] loops =
            [renderer.RunAsync(stop.Token), input.RunUntilQuitAsync(stop.Token), commands.RunAsync(stop.Token)];
        await Task.WhenAny(loops);
        await stop.CancelAsync();
        await SettleAsync(loops);
    }

    private static async Task SettleAsync(Task[] loops)
    {
        try
        {
            await Task.WhenAll(loops);
        }
        catch (OperationCanceledException)
        {
            // Expected for the loops we stopped; a faulted loop surfaces its own exception instead.
        }
    }
}
