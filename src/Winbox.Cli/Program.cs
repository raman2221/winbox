using Winbox.Cli;
using Winbox.Core;

var paths = WinboxPaths.Default();
var guest = new GuestClient(paths.Control);
var launcher = new SandboxLauncher(paths, guest);
var commands = CommandCatalog.Create(guest, launcher);

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, press) =>
{
    // Let the running command unwind through its cancellation token instead of killing the process.
    press.Cancel = true;
    cancellation.Cancel();
};

try
{
    await commands.RunAsync(args, cancellation.Token);
    return 0;
}
catch (WinboxException error)
{
    Console.Error.WriteLine($"winbox: {error.Message}");
    return 1;
}
catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
{
    Console.Error.WriteLine("winbox: cancelled");
    return 130;
}
