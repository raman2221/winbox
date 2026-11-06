# Contributing to winbox

Thanks for helping. Bug reports, results from real Windows Sandbox machines, and pull requests are all welcome.

## Before you start

- For anything bigger than a small fix, open an issue first so we can agree on the approach.
- Keep pull requests focused on one change. Small PRs get reviewed faster.

## Build and test

You need Windows and the [.NET 10 SDK](https://dotnet.microsoft.com/download). The guest helper targets
.NET Framework 4.8, so building it needs the 4.8 targeting pack (included with Visual Studio's .NET desktop workload).

```powershell
dotnet restore winbox.slnx
dotnet build winbox.slnx -c Release
dotnet test winbox.slnx -c Release
```

`dotnet test winbox.slnx` runs both test projects: the host tests (net10.0-windows) and the guest helper tests
(net48).

To produce the same self-contained zip the release workflow builds:

```powershell
.\scripts\package.ps1
# dist\winbox-win-x64.zip and dist\winbox-win-x64.zip.sha256
```

To try a change end to end you need Windows Sandbox enabled (see the README). Run `winbox start` from your build
output, then `winbox status` and `winbox screenshot shot.png` to check the guest helper responds. If something goes
wrong inside the sandbox, the helper logs errors to `%LOCALAPPDATA%\winbox\control\guest.log` on the host.

## Project layout

- `src/Winbox.Protocol`: command format and control-folder layout shared by host and guest (netstandard2.0).
- `src/Winbox.Guest`: the helper that runs inside the sandbox (net48).
- `src/Winbox.Core`: host-side logic: sandbox config, launching, guest client, Sixel rendering (net10.0-windows).
- `src/Winbox.Cli`: `winbox.exe`, the CLI and MCP server (net10.0-windows).
- `tests/Winbox.Core.Tests`: xunit tests for the host side (net10.0-windows).
- `tests/Winbox.Guest.Tests`: xunit tests for the guest helper, on the runtime it ships on (net48).

## Code style

- **Small functions that do one thing.** If you need "and" to describe a function, split it.
- **Three parameters at most.** Group related values into a type when you need more.
- **No flag arguments.** Instead of `Capture(bool includeCursor)`, write two clearly named functions.
- **Exceptions, not error codes.** Throw a specific exception with a helpful message; don't return magic values.
- **No dead code.** Delete unused code and commented-out blocks. Git remembers them.
- **Comments explain why, not what.** Good names should make the "what" obvious.
- **Inject dependencies.** Pass collaborators (file system, clock, process launcher) in through constructors so
  code can be tested without a real sandbox.
- **Tests check one concept each** and follow Arrange, Act, Assert. Name them after the behavior they check.
- **Lines stay at or under 120 characters.**

## Pull requests

- Make sure `dotnet build` and `dotnet test` pass. CI runs both on every push and pull request.
- Describe what changed and why, and how you tested it. Say whether you ran it inside a real Windows Sandbox.
- Update the README if you change a command, flag or MCP tool.

## License

By contributing, you agree that your contributions are licensed under the [MIT License](LICENSE).
