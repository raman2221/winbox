using System.Drawing;
using Winbox.Core.Terminal;

namespace Winbox.Cli.Viewer;

/// <summary>Routes terminal events: cell-size reports to the shared state, everything else to the guest.</summary>
internal sealed class InputDispatcher(InputReader reader, ViewerState state, CommandQueue commands)
{
    /// <summary>Runs until the user presses Ctrl+Q or input ends.</summary>
    public async Task RunUntilQuitAsync(CancellationToken cancel)
    {
        reader.Start();
        await foreach (var input in reader.Events.ReadAllAsync(cancel))
        {
            if (ViewerInput.IsQuit(input)) return;
            Dispatch(input);
        }
    }

    private void Dispatch(TerminalEvent input)
    {
        if (input is CellSizeEvent size)
        {
            state.ReportCell(new Size(size.Width, size.Height));
            return;
        }
        if (ViewerInput.ToCommand(input, state.Layout) is { } command) commands.Enqueue(command);
    }
}
