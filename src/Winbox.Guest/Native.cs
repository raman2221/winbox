using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Winbox.Protocol;

namespace Winbox.Guest;

/// <summary>Thin wrapper over user32 SendInput.</summary>
internal static class Native
{
    public const uint MouseMove = 0x0001, LeftDown = 0x0002, LeftUp = 0x0004;
    public const uint RightDown = 0x0008, RightUp = 0x0010, MiddleDown = 0x0020, MiddleUp = 0x0040;
    public const uint Wheel = 0x0800, MouseAbsolute = 0x8000;
    private const uint KeyUpFlag = 0x0002, UnicodeFlag = 0x0004, ExtendedKeyFlag = 0x0001;
    private const uint InputMouse = 0, InputKeyboard = 1;

    // Keys that live on the extended part of the keyboard; without the flag, arrows act like the numpad.
    private static readonly ushort[] ExtendedKeys = { 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x2D, 0x2E, 0x5B, 0x5D };

    public static (uint Down, uint Up) ButtonFlags(MouseButton button) => button switch
    {
        MouseButton.Right => (RightDown, RightUp),
        MouseButton.Middle => (MiddleDown, MiddleUp),
        _ => (LeftDown, LeftUp),
    };

    public static Input Mouse(uint flags, int dx, int dy, int wheel) => new()
    {
        Type = InputMouse,
        Data = { Mouse = new MouseInput { Dx = dx, Dy = dy, MouseData = unchecked((uint)wheel), Flags = flags } },
    };

    public static Input UnicodeKey(char ch, bool keyUp) =>
        Key(new KeyboardInput { Scan = ch, Flags = UnicodeFlag | (keyUp ? KeyUpFlag : 0) });

    public static Input VirtualKey(ushort key, bool keyUp)
    {
        var extended = Array.IndexOf(ExtendedKeys, key) >= 0 ? ExtendedKeyFlag : 0;
        return Key(new KeyboardInput { Vk = key, Flags = extended | (keyUp ? KeyUpFlag : 0) });
    }

    public static void Send(params Input[] inputs)
    {
        var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
        if (sent != inputs.Length) throw new Win32Exception(Marshal.GetLastWin32Error(), "SendInput was blocked");
    }

    public static Point CursorPosition()
    {
        if (!GetCursorPos(out var point)) throw new Win32Exception(Marshal.GetLastWin32Error());
        return new Point(point.X, point.Y);
    }

    private static Input Key(KeyboardInput keyboard) => new() { Type = InputKeyboard, Data = { Keyboard = keyboard } };

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, Input[] inputs, int size);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetCursorPos(out CursorPoint point);

    [StructLayout(LayoutKind.Sequential)]
    private struct CursorPoint
    {
        public int X, Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Input
    {
        public uint Type;
        public InputUnion Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion
    {
        [FieldOffset(0)] public MouseInput Mouse;
        [FieldOffset(0)] public KeyboardInput Keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MouseInput
    {
        public int Dx, Dy;
        public uint MouseData, Flags, Time;
        public IntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KeyboardInput
    {
        public ushort Vk, Scan;
        public uint Flags, Time;
        public IntPtr ExtraInfo;
    }
}
