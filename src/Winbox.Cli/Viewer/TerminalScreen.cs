using System.Text;

namespace Winbox.Cli.Viewer;

/// <summary>
/// The viewer's full-screen terminal surface. Each frame is written in one flush, wrapped in a
/// synchronized update, and drawn over the previous one in place so the picture never blanks.
/// </summary>
internal sealed class TerminalScreen(TextWriter output) : IDisposable
{
    private const string Csi = "\u001b[";
    private const string EnterSequence =
        Csi + "?1049h" + Csi + "?25l" + Csi + "?1000h" + Csi + "?1006h" + Csi + "16t";
    private const string LeaveSequence =
        Csi + "?2026l" + Csi + "?1006l" + Csi + "?1000l" + Csi + "?25h" + Csi + "?1049l";
    private const string BeginUpdate = Csi + "?2026h";
    private const string EndUpdate = Csi + "?2026l";

    public static TerminalScreen OverStandardOutput()
    {
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        return new TerminalScreen(new StreamWriter(Console.OpenStandardOutput(), encoding, 1 << 16));
    }

    private bool clearPending;

    /// <summary>Switches to the alternate screen, hides the cursor, enables mouse reports, asks cell size.</summary>
    public void Enter() => WriteNow(EnterSequence);

    /// <summary>Clears as part of the next frame's update, so the screen is never shown blank.</summary>
    public void ClearBeforeNextFrame() => clearPending = true;

    public void DrawFrame(string sixel, StatusText status)
    {
        output.Write(BeginUpdate);
        if (clearPending) output.Write(Csi + "2J");
        clearPending = false;
        output.Write(Csi + "H");
        output.Write(sixel);
        output.Write(StatusSequence(status));
        WriteNow(EndUpdate);
    }

    /// <summary>Rewrites only the status line, leaving the picture untouched.</summary>
    public void DrawStatus(StatusText status) => WriteNow(BeginUpdate + StatusSequence(status) + EndUpdate);

    public void Dispose()
    {
        WriteNow(LeaveSequence);
        output.Dispose();
    }

    private static string StatusSequence(StatusText status) => $"{Csi}{status.Row};1H{status.Text}{Csi}K";

    private void WriteNow(string text)
    {
        output.Write(text);
        output.Flush();
    }
}

/// <summary>The status line text and the 1-based terminal row it belongs on.</summary>
internal readonly record struct StatusText(string Text, int Row);
