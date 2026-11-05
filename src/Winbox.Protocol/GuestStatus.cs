using System;
using System.Globalization;

namespace Winbox.Protocol;

/// <summary>Heartbeat the guest writes every second: time, screen size, and helper version.</summary>
public sealed record GuestStatus(DateTimeOffset WrittenAt, int ScreenWidth, int ScreenHeight, string Version)
{
    public static readonly TimeSpan StaleAfter = TimeSpan.FromSeconds(3);

    public bool IsAliveAt(DateTimeOffset now) => now - WrittenAt < StaleAfter;

    public string Encode() =>
        string.Join(" ", WrittenAt.ToUnixTimeMilliseconds(), ScreenWidth, ScreenHeight, Version);

    public static GuestStatus Decode(string line)
    {
        var parts = line.Trim().Split(' ');
        if (parts.Length != 4) throw Malformed(line);
        try
        {
            var unixMs = long.Parse(parts[0], CultureInfo.InvariantCulture);
            var writtenAt = DateTimeOffset.FromUnixTimeMilliseconds(unixMs);
            return new GuestStatus(writtenAt, ParseInt(parts[1]), ParseInt(parts[2]), parts[3]);
        }
        catch (Exception e) when (e is FormatException or OverflowException or ArgumentOutOfRangeException)
        {
            throw Malformed(line);
        }
    }

    private static ProtocolException Malformed(string line) => new($"Malformed status '{line}'");

    private static int ParseInt(string text) => int.Parse(text, CultureInfo.InvariantCulture);
}
