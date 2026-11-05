using System.Text;

namespace Winbox.Cli.Viewer;

/// <summary>
/// Puts the console into raw VT mode for the viewer and puts every setting back on dispose:
/// input arrives as unprocessed VT bytes (so Ctrl+C is just a key) and output understands VT.
/// </summary>
internal sealed class ConsoleModeScope : IDisposable
{
    private const uint ProcessedInput = 0x0001, LineInput = 0x0002, EchoInput = 0x0004;
    private const uint QuickEditMode = 0x0040, ExtendedFlags = 0x0080, VirtualTerminalInput = 0x0200;
    private const uint ProcessedOutput = 0x0001, VirtualTerminalProcessing = 0x0004;

    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly ConsoleSnapshot original;

    private ConsoleModeScope(ConsoleSnapshot original) => this.original = original;

    public static ConsoleModeScope EnterRawMode()
    {
        var scope = new ConsoleModeScope(ConsoleSnapshot.Capture());
        try
        {
            scope.ApplyRawMode();
            return scope;
        }
        catch
        {
            scope.Dispose();
            throw;
        }
    }

    public void Dispose() => original.Restore();

    private void ApplyRawMode()
    {
        Console.InputEncoding = Utf8;
        Console.OutputEncoding = Utf8;
        NativeConsole.SetMode(original.InputHandle, RawInput(original.InputMode));
        NativeConsole.SetMode(original.OutputHandle, original.OutputMode | ProcessedOutput | VirtualTerminalProcessing);
    }

    // Quick edit would let the console grab mouse drags for text selection instead of reporting them.
    private static uint RawInput(uint mode) =>
        (mode & ~(ProcessedInput | LineInput | EchoInput | QuickEditMode)) | VirtualTerminalInput | ExtendedFlags;

    private sealed record ConsoleSnapshot(
        IntPtr InputHandle, uint InputMode, IntPtr OutputHandle, uint OutputMode, Encoding InputEncoding,
        Encoding OutputEncoding)
    {
        public static ConsoleSnapshot Capture()
        {
            var input = NativeConsole.Handle(NativeConsole.StandardInput);
            var output = NativeConsole.Handle(NativeConsole.StandardOutput);
            return new ConsoleSnapshot(input, NativeConsole.ModeOf(input), output, NativeConsole.ModeOf(output),
                Console.InputEncoding, Console.OutputEncoding);
        }

        public void Restore()
        {
            NativeConsole.TrySetMode(InputHandle, InputMode);
            NativeConsole.TrySetMode(OutputHandle, OutputMode);
            Console.InputEncoding = InputEncoding;
            Console.OutputEncoding = OutputEncoding;
        }
    }
}
