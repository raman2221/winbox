using System;
using System.Diagnostics;
using Winbox.Guest.Overlay;
using Winbox.Protocol;

namespace Winbox.Guest;

/// <summary>Turns one decoded command into a screenshot, an input event, or a process launch.</summary>
internal sealed class CommandExecutor(ScreenCapture screen, InputInjector input, IActivityFeedback feedback)
{
    private readonly Narrator _narrator = new();

    public Reply Execute(string imagePath, string line)
    {
        try
        {
            var command = CommandCodec.Decode(line);
            using (ShowCaption(command)) return Run(imagePath, command);
        }
        catch (Exception error)
        {
            return Reply.Failure(error.Message);
        }
    }

    /// <summary>Keeps the command's caption on screen while it runs, or returns null when it has none.</summary>
    private CaptionKeepAlive? ShowCaption(Command command) =>
        _narrator.CaptionFor(command) is { } caption ? new CaptionKeepAlive(feedback, caption) : null;

    private Reply Run(string imagePath, Command command)
    {
        switch (command)
        {
            case ScreenshotCommand: return TakeScreenshot(imagePath);
            case SayCommand: break; // Showing its caption is all it does.
            case ClickCommand c: input.Click(c.At, c.Button, c.Count); break;
            case MoveCommand m: input.MoveTo(m.To); break;
            case DragCommand d: input.Drag(d.From, d.To); break;
            case ScrollCommand s: input.Scroll(s.At, s.Notches); break;
            case TypeTextCommand t: input.TypeText(t.Text); break;
            case PressKeyCommand k: input.PressKeys(KeyCombo.Parse(k.Combo)); break;
            case RunCommand r: StartProcess(r.CommandLine); break;
            case ShutdownCommand: ShutDown(); break;
        }
        return Reply.Success();
    }

    private static void ShutDown()
    {
        // /f closes apps that would otherwise hold shutdown up over unsaved work; the 1 s delay lets this reply
        // reach the host first.
        Process.Start("shutdown", "/s /f /t 1")?.Dispose();
    }

    private Reply TakeScreenshot(string imagePath)
    {
        var size = screen.SaveAsPng(imagePath);
        return Reply.Success($"{size.Width} {size.Height}");
    }

    private static void StartProcess(string commandLine)
    {
        // The shell resolves app names like the Run dialog does, without cmd reading &, | or % in the arguments.
        // A program it can't find throws, which becomes an error reply.
        var (program, arguments) = ProgramLine.Split(commandLine);
        Process.Start(new ProcessStartInfo(program, arguments) { UseShellExecute = true })?.Dispose();
    }
}
