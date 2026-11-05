using System;
using System.Collections.Generic;
using System.Linq;

namespace Winbox.Protocol;

/// <summary>Parses combos like "ctrl+shift+esc" into Windows virtual-key codes, modifiers first.</summary>
public static class KeyCombo
{
    private static readonly Dictionary<string, ushort> NamedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ctrl"] = 0x11, ["control"] = 0x11, ["shift"] = 0x10, ["alt"] = 0x12,
        ["win"] = 0x5B, ["super"] = 0x5B, ["cmd"] = 0x5B,
        ["enter"] = 0x0D, ["return"] = 0x0D, ["tab"] = 0x09, ["esc"] = 0x1B, ["escape"] = 0x1B,
        ["space"] = 0x20, ["backspace"] = 0x08, ["delete"] = 0x2E, ["del"] = 0x2E, ["insert"] = 0x2D,
        ["up"] = 0x26, ["down"] = 0x28, ["left"] = 0x25, ["right"] = 0x27,
        ["home"] = 0x24, ["end"] = 0x23, ["pageup"] = 0x21, ["pagedown"] = 0x22,
        ["printscreen"] = 0x2C, ["capslock"] = 0x14, ["menu"] = 0x5D,
    };

    public static IReadOnlyList<ushort> Parse(string combo)
    {
        var names = combo.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
        if (names.Length == 0) throw new ProtocolException("Empty key combo");
        return names.Select(name => ToVirtualKey(name.Trim())).ToList();
    }

    private static ushort ToVirtualKey(string name)
    {
        if (NamedKeys.TryGetValue(name, out var code)) return code;
        if (name.Length == 1 && IsAsciiLetterOrDigit(name[0])) return char.ToUpperInvariant(name[0]);
        if (TryParseFunctionKey(name, out code)) return code;
        throw new ProtocolException($"Unknown key '{name}'");
    }

    // Virtual-key codes for letters and digits equal their ASCII codes; other scripts have no such key.
    private static bool IsAsciiLetterOrDigit(char ch) => ch < 128 && char.IsLetterOrDigit(ch);

    private static bool TryParseFunctionKey(string name, out ushort code)
    {
        var number = 0;
        var isFunctionKey = name.Length > 1 && char.ToLowerInvariant(name[0]) == 'f'
            && int.TryParse(name.Substring(1), out number) && number is >= 1 and <= 24;
        code = isFunctionKey ? (ushort)(0x70 + number - 1) : (ushort)0;
        return isFunctionKey;
    }
}
