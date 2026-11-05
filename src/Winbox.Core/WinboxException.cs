namespace Winbox.Core;

/// <summary>An error with a message meant for the person at the terminal, not a stack trace.</summary>
public class WinboxException(string message) : Exception(message);

public sealed class GuestUnavailableException()
    : WinboxException("The winbox sandbox isn't running or its helper isn't responding. Run `winbox start` first.");
