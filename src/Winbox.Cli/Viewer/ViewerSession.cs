using Winbox.Core;

namespace Winbox.Cli.Viewer;

/// <summary>Entry point for <c>winbox view</c>: shows the guest desktop live in the terminal, forwards input.</summary>
public static class ViewerSession
{
    /// <summary>
    /// Runs until Ctrl+Q or cancellation. The terminal is always restored before this returns or
    /// throws; a <see cref="WinboxException"/> from the guest is rethrown for the CLI to print.
    /// </summary>
    public static async Task RunAsync(GuestClient guest, CancellationToken cancel)
    {
        // Fail before touching the terminal, so a stopped sandbox gives a plain error message.
        if (!guest.IsGuestAlive()) throw new GuestUnavailableException();
        using var console = ConsoleModeScope.EnterRawMode();
        using var screen = TerminalScreen.OverStandardOutput();
        screen.Enter();
        await Build(guest, screen).RunAsync(cancel);
    }

    private static LiveViewer Build(GuestClient guest, TerminalScreen screen)
    {
        var state = new ViewerState();
        var commands = new CommandQueue(guest);
        var input = new InputDispatcher(new InputReader(Console.OpenStandardInput()), state, commands);
        return new LiveViewer(new FrameRenderer(guest, screen, state), input, commands);
    }
}
