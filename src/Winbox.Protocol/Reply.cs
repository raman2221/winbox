namespace Winbox.Protocol;

/// <summary>The guest's answer to one command, encoded as "ok &lt;message&gt;" or "err &lt;message&gt;".</summary>
public sealed record Reply(bool Ok, string Message)
{
    public static Reply Success(string message = "") => new(true, message);

    public static Reply Failure(string message) => new(false, message);

    public string Encode() => (Ok ? "ok " : "err ") + Message.Replace('\n', ' ');

    public static Reply Decode(string line)
    {
        var separator = line.IndexOf(' ');
        var status = separator < 0 ? line.TrimEnd() : line.Substring(0, separator);
        var message = separator < 0 ? "" : line.Substring(separator + 1).Trim();
        return status switch
        {
            "ok" => Success(message),
            "err" => Failure(message),
            _ => throw new ProtocolException($"Malformed reply '{line}'"),
        };
    }
}
