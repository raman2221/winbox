using Winbox.Cli.CommandLine;
using Winbox.Cli.Commands;
using Winbox.Cli.Desktop;
using Winbox.Cli.Mcp;
using Winbox.Core;

namespace Winbox.Cli;

/// <summary>Wires every subcommand to its dependencies, in the order the help screen lists them.</summary>
internal static class CommandCatalog
{
    public static CommandSet Create(GuestClient guest, SandboxLauncher launcher)
    {
        var actions = new DesktopActions(new ActionSender(guest));
        var commands = new CommandSet();
        commands.Register(new HelpCommand(commands));
        commands.Register(new SetupCommand());
        commands.Register(new StartCommand(launcher));
        commands.Register(new StopCommand(launcher));
        commands.Register(new StatusCommand(guest));
        commands.Register(new ViewCommand(guest));
        commands.Register(new SaveScreenshotCommand(guest));
        foreach (var input in InputCommands(actions)) commands.Register(input);
        commands.Register(new McpCommand(guest, launcher, actions));
        commands.Register(new McpConfigCommand());
        return commands;
    }

    private static IEnumerable<ICliCommand> InputCommands(DesktopActions actions) =>
    [
        new InputCommand(new("click <x> <y> [left|right|middle] [--double]", "Click at a screen position"),
            InputParsers.Click, actions),
        new InputCommand(new("move <x> <y>", "Move the mouse"), InputParsers.Move, actions),
        new InputCommand(new("drag <x1> <y1> <x2> <y2>", "Drag with the left button"), InputParsers.Drag, actions),
        new InputCommand(new("scroll <x> <y> <notches>", "Scroll the wheel (positive is up)"),
            InputParsers.Scroll, actions),
        new InputCommand(new("type <text...>", "Type text into the focused window"), InputParsers.TypeText, actions),
        new InputCommand(new("key <combo>", "Press a key combo, e.g. ctrl+shift+esc"), InputParsers.PressKey, actions),
        new InputCommand(new("run <command line...>", "Start a program, e.g. notepad"), InputParsers.Run, actions),
    ];
}
