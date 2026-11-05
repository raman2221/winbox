using Winbox.Protocol;

namespace Winbox.Core.Tests.Core;

public sealed class GuestClientTests : IDisposable
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(10);

    private readonly TempDirectory _temp = new();
    private readonly ControlPaths _paths;
    private readonly FakeGuest _guest;
    private readonly GuestClient _client;
    private readonly CancellationTokenSource _cancel = new(TestTimeout);

    public GuestClientTests()
    {
        _paths = new ControlPaths(_temp.Path);
        _paths.EnsureCreated();
        _guest = new FakeGuest(_paths);
        _client = new GuestClient(_paths);
    }

    public void Dispose()
    {
        _cancel.Dispose();
        _temp.Dispose();
    }

    [Fact]
    public async Task SendAsync_GuestAnswersOk_DeliversCommandAndReturnsMessage()
    {
        _guest.WriteHeartbeat(DateTimeOffset.UtcNow);
        var command = new TypeTextCommand("hello sandbox");
        var guestTurn = _guest.AnswerNextAsync(Reply.Success("typed 13 chars"));

        var message = await _client.SendAsync(command, _cancel.Token);

        Assert.Equal("typed 13 chars", message);
        Assert.Equal(command, await guestTurn);
    }

    [Fact]
    public async Task SendAsync_AfterReply_LeavesNoProtocolFilesBehind()
    {
        _guest.WriteHeartbeat(DateTimeOffset.UtcNow);
        var guestTurn = _guest.AnswerNextAsync(Reply.Success());

        await _client.SendAsync(new MoveCommand(new Point(1, 1)), _cancel.Token);
        await guestTurn;

        Assert.Empty(Directory.GetFiles(_paths.RequestsDir));
        Assert.Empty(Directory.GetFiles(_paths.RepliesDir));
    }

    [Fact]
    public async Task SendAsync_GuestAnswersErr_ThrowsWinboxExceptionWithReason()
    {
        _guest.WriteHeartbeat(DateTimeOffset.UtcNow);
        var guestTurn = _guest.AnswerNextAsync(Reply.Failure("no such program"));

        var error = await Assert.ThrowsAsync<WinboxException>(
            () => _client.SendAsync(new RunCommand("nope.exe"), _cancel.Token));

        await guestTurn;
        Assert.IsNotType<GuestUnavailableException>(error);
        Assert.Contains("no such program", error.Message);
    }

    [Fact]
    public async Task ScreenshotAsync_GuestSendsImage_ReturnsPngAndSizeAndDeletesImage()
    {
        _guest.WriteHeartbeat(DateTimeOffset.UtcNow);
        byte[] png = [0x89, 0x50, 0x4E, 0x47];
        var guestTurn = _guest.AnswerNextAsync(Reply.Success("1280 720"), png);

        var screenshot = await _client.ScreenshotAsync(_cancel.Token);

        Assert.IsType<ScreenshotCommand>(await guestTurn);
        Assert.Equal(png, screenshot.Png);
        Assert.Equal((1280, 720), (screenshot.Width, screenshot.Height));
        Assert.Empty(Directory.GetFiles(_paths.RepliesDir));
    }

    [Fact]
    public async Task SendAsync_NoStatusFile_ThrowsGuestUnavailableWithoutWritingRequest()
    {
        await Assert.ThrowsAsync<GuestUnavailableException>(
            () => _client.SendAsync(new ScreenshotCommand(), _cancel.Token));

        Assert.Empty(Directory.GetFiles(_paths.RequestsDir));
    }

    [Fact]
    public async Task SendAsync_StaleHeartbeat_ThrowsGuestUnavailable()
    {
        _guest.WriteHeartbeat(DateTimeOffset.UtcNow - TimeSpan.FromMinutes(1));

        await Assert.ThrowsAsync<GuestUnavailableException>(
            () => _client.SendAsync(new ScreenshotCommand(), _cancel.Token));
    }

    [Fact]
    public void IsGuestAlive_FreshHeartbeat_ReturnsTrue()
    {
        _guest.WriteHeartbeat(DateTimeOffset.UtcNow);

        Assert.True(_client.IsGuestAlive());
    }

    [Fact]
    public void IsGuestAlive_CorruptStatusFile_ReturnsFalse()
    {
        File.WriteAllText(_paths.StatusFile, "not a heartbeat");

        Assert.False(_client.IsGuestAlive());
    }

    [Fact]
    public void ReadStatus_FreshHeartbeat_ReturnsScreenSize()
    {
        _guest.WriteHeartbeat(DateTimeOffset.UtcNow);

        var status = _client.ReadStatus();

        Assert.Equal((1280, 720), (status?.ScreenWidth, status?.ScreenHeight));
    }
}
