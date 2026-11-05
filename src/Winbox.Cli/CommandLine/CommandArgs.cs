using System.Globalization;
using Winbox.Core;
using Winbox.Protocol;

namespace Winbox.Cli.CommandLine;

internal sealed class UsageException(string usage) : WinboxException($"usage: winbox {usage}");

/// <summary>
/// The arguments after the subcommand name. Options are taken out first, so what remains is positional.
/// </summary>
internal sealed class CommandArgs(IEnumerable<string> values, string usage)
{
    private readonly List<string> values = values.ToList();

    public int Count => values.Count;

    public bool TakeFlag(string flag) => values.Remove(flag);

    public string? TakeOption(string option)
    {
        var index = values.IndexOf(option);
        if (index < 0) return null;
        if (index + 1 >= values.Count) throw Usage();
        var value = values[index + 1];
        values.RemoveRange(index, 2);
        return value;
    }

    public void ExpectBetween(int min, int max)
    {
        var hasUnknownOption = values.Any(value => value.StartsWith("--", StringComparison.Ordinal));
        if (hasUnknownOption || values.Count < min || values.Count > max) throw Usage();
    }

    public string TextAt(int index) => index < values.Count ? values[index] : throw Usage();

    public int IntAt(int index) => ParseInt(TextAt(index));

    public Point PointAt(int index) => new(IntAt(index), IntAt(index + 1));

    /// <summary>Everything from <paramref name="index"/> on, rejoined, for free text such as what to type.</summary>
    public string RestFrom(int index) =>
        index < values.Count ? string.Join(' ', values.Skip(index)) : throw Usage();

    public int ParseInt(string text) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : throw Usage();

    public UsageException Usage() => new(usage);
}
