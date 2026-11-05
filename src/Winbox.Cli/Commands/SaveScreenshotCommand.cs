using Winbox.Cli.CommandLine;
using Winbox.Core;

namespace Winbox.Cli.Commands;

internal sealed class SaveScreenshotCommand(GuestClient guest) : ICliCommand
{
    public CommandHelp Help { get; } = new("screenshot <out.png>", "Save a PNG of the desktop");

    public async Task RunAsync(CommandArgs args, CancellationToken cancel)
    {
        args.ExpectBetween(1, 1);
        var path = Path.GetFullPath(args.TextAt(0));
        var shot = await guest.ScreenshotAsync(cancel);
        await File.WriteAllBytesAsync(path, shot.Png, cancel);
        Console.WriteLine($"Saved a {shot.Width}x{shot.Height} screenshot to {path}");
    }
}
