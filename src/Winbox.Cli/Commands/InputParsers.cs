using Winbox.Cli.CommandLine;
using Winbox.Cli.Desktop;
using Winbox.Protocol;

namespace Winbox.Cli.Commands;

/// <summary>Command-line parsers for the input subcommands, one per action.</summary>
internal static class InputParsers
{
    public static Command Click(CommandArgs args)
    {
        var clicks = args.TakeFlag("--double") ? 2 : 1;
        args.ExpectBetween(2, 3);
        var button = args.Count == 3 ? args.TextAt(2) : "left";
        return DesktopRequests.Click(args.PointAt(0), button, clicks);
    }

    public static Command Move(CommandArgs args)
    {
        args.ExpectBetween(2, 2);
        return new MoveCommand(args.PointAt(0));
    }

    public static Command Drag(CommandArgs args)
    {
        args.ExpectBetween(4, 4);
        return new DragCommand(args.PointAt(0), args.PointAt(2));
    }

    public static Command Scroll(CommandArgs args)
    {
        args.ExpectBetween(3, 3);
        return new ScrollCommand(args.PointAt(0), args.IntAt(2));
    }

    public static Command TypeText(CommandArgs args) => DesktopRequests.TypeText(args.RestFrom(0));

    public static Command PressKey(CommandArgs args)
    {
        args.ExpectBetween(1, 1);
        return DesktopRequests.PressKey(args.TextAt(0));
    }

    public static Command Run(CommandArgs args) => DesktopRequests.Run(args.RestFrom(0));
}
