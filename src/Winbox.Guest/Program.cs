using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Winbox.Guest;
using Winbox.Guest.Motion;
using Winbox.Guest.Overlay;
using Winbox.Protocol;

const string DefaultControlRoot = @"C:\winbox\control";

var paths = new ControlPaths(args.Length > 0 ? args[0] : DefaultControlRoot);
var log = new GuestLog(paths.LogFile);

// Without a handler, a UI-thread exception shows WinForms' error dialog, whose Quit button an agent might click.
Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
Application.ThreadException += (_, e) => log.Write(e.Exception);
AppDomain.CurrentDomain.UnhandledException += (_, e) => log.Write((Exception)e.ExceptionObject);

// The default ~15.6 ms timer tick would make 8 ms motion frames and key delays run about twice as slow.
TimerResolution.Request1Ms();

var screen = new ScreenCapture();
var overlay = AgentOverlay.Start();
var input = new InputInjector(screen, HumanPacing.Create(), overlay);
var executor = new CommandExecutor(screen, input, overlay);
new GuestServer(paths, executor, screen).Run();

internal static class TimerResolution
{
    public static void Request1Ms() => timeBeginPeriod(1);

    [DllImport("winmm.dll")]
    private static extern uint timeBeginPeriod(uint milliseconds);
}
