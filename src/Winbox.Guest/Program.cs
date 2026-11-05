using Winbox.Guest;
using Winbox.Guest.Motion;
using Winbox.Guest.Overlay;
using Winbox.Protocol;

const string DefaultControlRoot = @"C:\winbox\control";

var paths = new ControlPaths(args.Length > 0 ? args[0] : DefaultControlRoot);
var screen = new ScreenCapture();
var overlay = AgentOverlay.Start();
var input = new InputInjector(screen, HumanPacing.Create(), overlay);
var executor = new CommandExecutor(screen, input, overlay);
new GuestServer(paths, executor, screen).Run();
