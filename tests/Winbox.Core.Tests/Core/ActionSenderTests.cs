using Winbox.Core.Terminal;
using Winbox.Protocol;

namespace Winbox.Core.Tests.Core;

public sealed class ActionSenderTests : IDisposable
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(10);
    private static readonly SayCommand StartMenuSay = new("Opening the Start menu");

    private readonly TempDirectory _temp = new();
    private readonly FakeGuest _guest;
    private readonly ActionSender _sender;
    private readonly CancellationTokenSource _cancel = new(TestTimeout);

    public ActionSenderTests()
    {
        var paths = new ControlPaths(_temp.Path);
        paths.EnsureCreated();
        _guest = new FakeGuest(paths);
        _guest.WriteHeartbeat(DateTimeOffset.UtcNow);
        _sender = new ActionSender(new GuestClient(paths));
    }

    public void Dispose()
    {
        _cancel.Dispose();
        _temp.Dispose();
    }

    [Fact]
    public async Task SendAsync_WithIntent_SendsSayThenTheAction()
    {
        var click = new ClickCommand(new Point(10, 700), MouseButton.Left, 1);
        var guestTurn = AnswerAsync(count: 2);

        await _sender.SendAsync(click, "Opening the Start menu", _cancel.Token);

        Assert.Equal(new Command[] { StartMenuSay, click }, await guestTurn);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \r\n ")]
    public async Task SendAsync_WithoutIntent_SendsOnlyTheAction(string? intent)
    {
        var key = new PressKeyCommand("win");
        var guestTurn = AnswerAsync(count: 1);

        await _sender.SendAsync(key, intent, _cancel.Token);

        Assert.Equal(new Command[] { key }, await guestTurn);
    }

    [Fact]
    public async Task SendAsync_IntentWithLongText_SaysBeforeEveryTypedPiece()
    {
        // Two full pieces and a short third one.
        var text = new string('x', TypingChunks.MaxChars * 2 + 10);
        var guestTurn = AnswerAsync(count: 6);

        await _sender.SendAsync(new TypeTextCommand(text), "Opening the Start menu", _cancel.Token);

        Assert.All((await guestTurn).Chunk(2), pair => Assert.Equal(StartMenuSay, pair[0]));
    }

    /// <summary>Answers the next requests one by one, as the guest would, and returns them in order.</summary>
    private async Task<IReadOnlyList<Command>> AnswerAsync(int count)
    {
        var sent = new List<Command>();
        for (var i = 0; i < count; i++) sent.Add(await _guest.AnswerNextAsync(Reply.Success()));
        return sent;
    }
}
