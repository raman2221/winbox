using System.Text;
using System.Threading.Channels;
using Winbox.Core.Terminal;

namespace Winbox.Cli.Viewer;

/// <summary>Reads raw terminal input on a background thread and publishes it as parsed events.</summary>
internal sealed class InputReader(Stream input)
{
    private const int BufferSize = 4096;

    private readonly Channel<TerminalEvent> events = Channel.CreateUnbounded<TerminalEvent>(
        new UnboundedChannelOptions { SingleWriter = true, SingleReader = true });

    public ChannelReader<TerminalEvent> Events => events.Reader;

    // A dedicated background thread, because a console read can't be cancelled and must not
    // hold a thread-pool thread or keep the process alive after the viewer exits.
    public void Start() => new Thread(ReadUntilEnd) { IsBackground = true, Name = "winbox view input" }.Start();

    private void ReadUntilEnd()
    {
        try
        {
            Pump();
            events.Writer.TryComplete();
        }
        catch (Exception error)
        {
            // Hand the failure to the consumer; an exception escaping a thread would kill the process.
            events.Writer.TryComplete(error);
        }
    }

    private void Pump()
    {
        var decoder = Encoding.UTF8.GetDecoder();
        var chunker = new InputChunker();
        var bytes = new byte[BufferSize];
        var chars = new char[Encoding.UTF8.GetMaxCharCount(BufferSize)];
        int read;
        while ((read = input.Read(bytes, 0, bytes.Length)) > 0)
        {
            var count = decoder.GetChars(bytes, 0, read, chars, 0);
            Publish(chunker.Feed(new string(chars, 0, count)));
        }
    }

    private void Publish(string text)
    {
        foreach (var terminalEvent in TerminalInputParser.Parse(text))
        {
            events.Writer.TryWrite(terminalEvent);
        }
    }
}
