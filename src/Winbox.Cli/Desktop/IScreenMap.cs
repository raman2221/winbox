using Winbox.Protocol;

namespace Winbox.Cli.Desktop;

/// <summary>Converts an action from the coordinates its caller gave to guest screen pixels.</summary>
internal interface IScreenMap
{
    Command ToGuest(Command command);
}

/// <summary>For callers that already work in guest pixels, such as the command line.</summary>
internal sealed class GuestPixels : IScreenMap
{
    public Command ToGuest(Command command) => command;
}
