using Winbox.Protocol;

namespace Winbox.Guest.Overlay;

/// <summary>Lets the input layer tell watchers what the agent is doing, without knowing how it's drawn.</summary>
internal interface IActivityFeedback
{
    void Announce(string caption);

    void ShowClick(Point at);
}
