using Microsoft.Win32;

namespace Winbox.Core;

/// <summary>Checks the host's Windows edition and explains how to enable Windows Sandbox.</summary>
public static class SetupAdvice
{
    public const string EnableCommand =
        "Enable-WindowsOptionalFeature -Online -FeatureName Containers-DisposableClientVM -All";

    public static readonly string SandboxMissing =
        "Windows Sandbox isn't enabled. In an *administrator* PowerShell, run:\n\n    "
        + EnableCommand + "\n\nthen restart your PC and run `winbox start` again.";

    private static readonly string[] HomeEditions = ["Core", "CoreSingleLanguage", "CoreCountrySpecific", "CoreN"];

    public static string EditionId() =>
        Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "EditionID", "")
            as string ?? "";

    public static bool IsHomeEdition(string editionId) => HomeEditions.Contains(editionId);
}
