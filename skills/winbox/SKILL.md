---
name: winbox
description: >-
  Drive a disposable Windows desktop inside Windows Sandbox through the winbox MCP tools (screenshot, click,
  type_text, press_key, run_program and friends). Use this when a task needs a real Windows GUI, such as testing a
  desktop app, clicking through an installer, or using a browser, without touching the user's own mouse, keyboard
  or screen.
---

# Working in a winbox sandbox

winbox gives you your own Windows desktop inside Windows Sandbox. You see it only through screenshots and act on it
only through the winbox MCP tools. The user's real desktop is never affected.

## Start up

1. Call `status`. If the sandbox isn't running, call `start_sandbox` (pass `offline=true` when the task doesn't need
   the internet or involves untrusted files or sites).
2. Booting takes a few minutes. `start_sandbox` returns once the desktop is ready; then take a `screenshot`. If
   anything fails later, `status` tells you whether the sandbox is running and the helper responds.
3. Windows Sandbox runs only one instance at a time. If `start_sandbox` fails because a sandbox is already open,
   tell the user rather than retrying.

## The loop: observe, act, verify

Never act blind. Every step goes:

1. **Observe.** Call `screenshot` and read the screen: which window has focus, what's on it, where the target is.
2. **Act.** Do exactly one thing: one click, one `type_text`, one `press_key`, one `run_program`, each with a
   short `intent` (see below).
3. **Verify.** Call `screenshot` again and confirm the action did what you expected before moving on.

If the screen didn't change, don't repeat the same action over and over. Work out why first: a dialog may have
stolen focus, the app may still be loading, or your coordinates may be off.

## Say what you're doing: pass `intent` on every action

Every input tool (`click`, `move_mouse`, `drag`, `scroll`, `type_text`, `press_key`, `run_program`) takes an
optional `intent`. It's shown in a caption at the bottom of the sandbox screen for the whole action, so the person
watching can follow your plan. **Always pass one.** Without it they only see "Clicking" or "Pressing enter".

- Keep it short, under about six words. Anything past 60 characters is cut off.
- Describe the goal, not the mechanics: "Opening the Start menu", "Searching for the invoice",
  "Saving the report", not "Clicking at 512, 300".
- Write it for a person: plain words, present tense, no jargon.
- Never put secrets or personal data in an intent. It's displayed on screen.

```
click(x=38, y=60, intent="Opening the File menu")
type_text(text="quarterly report", intent="Searching for the report")
press_key(combo="ctrl+s", intent="Saving the document")
```

## Actions are animated for people watching

A person is usually watching the sandbox, so winbox makes your input look hand-driven:

- The cursor glides to its target along a curved path instead of jumping, which takes a few hundred milliseconds
  (roughly 180 to 650 ms, longer for longer distances).
- `type_text` types one key at a time with short human-like gaps, so long text takes seconds, not milliseconds.
  Type what's needed and nothing more.
- A violet halo, click ripple and your intent caption show what you're doing on screen. They're hidden from your
  screenshots, so you won't see them and don't need to work around them.

This pacing doesn't replace verification. Still take a fresh `screenshot` after every action before deciding the
next one.

## Coordinates

- Coordinates are pixels of the **latest** screenshot, measured from its top-left corner. Use them exactly as you
  read them off the image.
- Screenshots may be scaled down to fit the model (at most 1568 px on the long edge and about 1.15 megapixels).
  winbox maps your coordinates back to the real screen for you. `status` reports the guest's real resolution,
  which can be larger than the screenshot; don't rescale coordinates yourself.
- Aim for the center of buttons, fields and icons, not their edges.
- Anything that moves the screen (opening a window, scrolling, resizing) makes old coordinates stale. Take a fresh
  screenshot before the next click.
- `scroll(x, y, notches)` scrolls the element under `(x, y)`. Positive `notches` scroll up, negative scroll down.
- For double-click use `click(x, y, clicks=2)`; for context menus use `button="right"`.

## Prefer the keyboard and commands over hunting for icons

Finding icons by pixels is slow and error-prone. Usually there's a more direct route:

- **Launch programs with `run_program`**, for example `msedge https://example.com`, `explorer C:\`, `cmd`,
  `powershell` or a full path to an `.exe` (quote paths with spaces: `"C:\My Tools\app.exe" --flag`). Windows Sandbox
  has no Store apps, so Notepad, Paint and Calculator aren't available.
- `run_program` starts the program directly, **not through `cmd`**. `&`, `|`, `>` and `%` are passed to the program
  as plain text. When you need shell features, ask for a shell: `cmd /c dir > C:\out.txt`. If the program doesn't
  exist, you get an error back.
- Or use **`press_key("win+r")`**, take a screenshot to confirm the Run dialog is open, `type_text` the command, then
  `press_key("enter")`.
- Use shortcuts: `ctrl+l` focuses a browser's address bar, `alt+f4` closes a window, `ctrl+s` saves, `alt+tab`
  switches windows, `win+d` shows the desktop, `ctrl+shift+esc` opens Task Manager.
- Click into a text field (and check it has focus) before `type_text`, or the text goes somewhere else.

## Waiting

- Programs, installers and web pages take time to appear. After `run_program` or anything that loads, wait a moment
  and screenshot again rather than assuming the window is ready.
- If something is still loading, screenshot again before acting. Don't click where a window *will* be.

## Safety

- **Never enter the user's real credentials** (passwords, API keys, tokens, card numbers) into the sandbox, and
  don't type secrets you find elsewhere. Screenshots and anything inside the guest can see what you type. If a task
  needs a login, stop and ask the user.
- Networking is usually on, so the sandbox can reach the internet and the user's local network. Treat it like any
  online machine and don't download or run things the task doesn't need.
- The sandbox is **wiped when it closes**. Anything you create inside it is lost after `stop_sandbox`. Tell the user
  before stopping if there's work they may want to keep.
- Call `stop_sandbox` only when the user asks or the task is clearly finished.
