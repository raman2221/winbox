using System;

namespace Winbox.Protocol;

/// <summary>
/// Splits a command line such as <c>"C:\My Tools\app.exe" --flag</c> into the program and its arguments.
/// The arguments stay exactly as written, so characters like &amp; and % in a URL reach the program untouched.
/// </summary>
public static class ProgramLine
{
    private static readonly char[] Whitespace = { ' ', '\t', '\r', '\n' };

    public static (string Program, string Arguments) Split(string commandLine)
    {
        var line = commandLine.Trim();
        var (program, rest) = line.StartsWith("\"", StringComparison.Ordinal) ? SplitQuoted(line) : SplitAtSpace(line);
        if (program.Length == 0) throw new ProtocolException("There is no program to run");
        return (program, rest.TrimStart());
    }

    private static (string Program, string Remainder) SplitQuoted(string line)
    {
        var closing = line.IndexOf('"', 1);
        if (closing < 0) throw new ProtocolException($"The program name in '{line}' has no closing quote");
        return (line.Substring(1, closing - 1), line.Substring(closing + 1));
    }

    private static (string Program, string Remainder) SplitAtSpace(string line)
    {
        var space = line.IndexOfAny(Whitespace);
        return space < 0 ? (line, "") : (line.Substring(0, space), line.Substring(space));
    }
}
