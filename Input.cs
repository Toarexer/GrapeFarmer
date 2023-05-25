using System;
using System.Runtime.InteropServices;

namespace WinUser;

public static class Input {
    [StructLayout(LayoutKind.Sequential)]
    public struct Mouse {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Keyboard {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Hardware {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion {
        [FieldOffset(0)] public uint type;
        [FieldOffset(8)] public Mouse mi;
        [FieldOffset(8)] public Keyboard ki;
        [FieldOffset(8)] public Hardware hi;
        public int Size() => Marshal.SizeOf<InputUnion>();
    }

    public static int UnionSize => Marshal.SizeOf<InputUnion>();

    public static class User32 {
        [DllImport("user32.dll")] public extern static short GetAsyncKeyState(VirtualKey vKey);

        [DllImport("user32.dll")] public extern static short VkKeyScanW(ushort ch);

        [DllImport("user32.dll")] public extern static IntPtr GetKeyboardLayout(int idThread = 0);

        [DllImport("user32.dll")] public extern static uint MapVirtualKeyExW(uint uCode, MapType uMapType, IntPtr dwhkl = 0);

        [DllImport("user32.dll")] public static extern IntPtr GetMessageExtraInfo();

        [DllImport("user32.dll")] public extern static uint SendInput(uint cInputs, InputUnion[] pInputs, int cbSize);
    }

    public static bool IsKeyPressed(VirtualKey keycode) => (User32.GetAsyncKeyState(keycode) & 0x8000) != 0;

    public static bool WasKeyPressed(VirtualKey keycode) => (User32.GetAsyncKeyState(keycode) & 0x1) != 0;

    public static bool SetKeyDown(VirtualKey keycode) {
        ushort code = (ushort)User32.MapVirtualKeyExW((uint)keycode, MapType.MAPVK_VK_TO_VSC_EX, User32.GetKeyboardLayout(0));
        InputUnion input = new();

        input.type = (uint)Type.INPUT_KEYBOARD;
        input.ki.dwExtraInfo = User32.GetMessageExtraInfo();
        input.ki.dwFlags = (uint)(GetFlagIfExtendedKey(code) | KeyEvent.KEYEVENTF_SCANCODE);
        input.ki.time = 0;
        input.ki.wScan = code;

        return User32.SendInput(1, new[] { input }, UnionSize) > 0;
    }

    public static bool SetKeyUp(VirtualKey keycode) {
        ushort code = (ushort)User32.MapVirtualKeyExW((uint)keycode, MapType.MAPVK_VK_TO_VSC_EX, User32.GetKeyboardLayout(0));
        InputUnion input = new();

        input.type = (uint)Type.INPUT_KEYBOARD;
        input.ki.dwExtraInfo = User32.GetMessageExtraInfo();
        input.ki.dwFlags = (uint)(GetFlagIfExtendedKey(code) | KeyEvent.KEYEVENTF_SCANCODE | KeyEvent.KEYEVENTF_KEYUP);
        input.ki.time = 0;
        input.ki.wScan = code;

        return User32.SendInput(1, new[] { input }, UnionSize) > 0;
    }

    public static bool PressKey(VirtualKey keycode) {
        ushort code = (ushort)User32.MapVirtualKeyExW((uint)keycode, MapType.MAPVK_VK_TO_VSC_EX, User32.GetKeyboardLayout(0));
        InputUnion[] inputs = new InputUnion[] { new(), new() };

        inputs[0].type = (uint)Type.INPUT_KEYBOARD;
        inputs[0].ki.dwExtraInfo = User32.GetMessageExtraInfo();
        inputs[0].ki.dwFlags = (uint)(GetFlagIfExtendedKey(code) | KeyEvent.KEYEVENTF_SCANCODE);
        inputs[0].ki.time = 0;
        inputs[0].ki.wScan = code;

        inputs[1].type = (uint)Type.INPUT_KEYBOARD;
        inputs[1].ki.dwExtraInfo = User32.GetMessageExtraInfo();
        inputs[1].ki.dwFlags = (uint)(GetFlagIfExtendedKey(code) | KeyEvent.KEYEVENTF_SCANCODE | KeyEvent.KEYEVENTF_KEYUP);
        inputs[1].ki.time = 0;
        inputs[1].ki.wScan = code;

        return User32.SendInput(2, inputs, UnionSize) == 2;
    }
    public static bool PressKey(char c) => PressKey((VirtualKey)User32.VkKeyScanW(c));

    static KeyEvent GetFlagIfExtendedKey(ushort code) => (code & 0xFF00) == 0xE000 || (code & 0xFF00) == 0xE100 ? KeyEvent.KEYEVENTF_EXTENDEDKEY : 0;

    public static bool SetMouseState(int x, int y, MouseEvent type) {
        InputUnion input = new();
        input.type = (uint)Type.INPUT_MOUSE;
        input.mi.dx = x;
        input.mi.dy = y;
        input.mi.mouseData = 0;
        input.mi.dwFlags = (uint)type;
        input.mi.time = 0;
        input.mi.dwExtraInfo = User32.GetMessageExtraInfo();
        return User32.SendInput(1, new[] { input }, UnionSize) > 0;
    }

    public enum Type : byte {
        INPUT_MOUSE,
        INPUT_KEYBOARD,
        INPUT_HARDWARE
    }

    public enum MapType : byte {
        MAPVK_VK_TO_VSC,
        MAPVK_VSC_TO_VK,
        MAPVK_VK_TO_CHAR,
        MAPVK_VSC_TO_VK_EX,
        MAPVK_VK_TO_VSC_EX
    }

    [Flags]
    public enum KeyEvent : byte {
        KEYEVENTF_EXTENDEDKEY = 0x1,
        KEYEVENTF_KEYUP = 0x2,
        KEYEVENTF_SCANCODE = 0x8,
        KEYEVENTF_UNICODE = 0x4,
    }

    public enum VirtualKey : byte {
        VK_LBUTTON = 0x01,
        VK_RBUTTON = 0x02,
        VK_CANCEL = 0x03,
        VK_MBUTTON = 0x04,
        VK_XBUTTON1 = 0x05,
        VK_XBUTTON2 = 0x06,
        VK_BACK = 0x08,
        VK_TAB = 0x09,
        VK_CLEAR = 0x0C,
        VK_RETURN = 0x0D,
        VK_SHIFT = 0x10,
        VK_CONTROL = 0x11,
        VK_MENU = 0x12,
        VK_PAUSE = 0x13,
        VK_CAPITAL = 0x14,
        VK_KANA = 0x15,
        VK_HANGEUL = 0x15,
        VK_HANGUL = 0x15,
        VK_JUNJA = 0x17,
        VK_FINAL = 0x18,
        VK_HANJA = 0x19,
        VK_KANJI = 0x19,
        VK_ESCAPE = 0x1B,
        VK_CONVERT = 0x1C,
        VK_NONCONVERT = 0x1D,
        VK_ACCEPT = 0x1E,
        VK_MODECHANGE = 0x1F,
        VK_SPACE = 0x20,
        VK_PRIOR = 0x21,
        VK_NEXT = 0x22,
        VK_END = 0x23,
        VK_HOME = 0x24,
        VK_LEFT = 0x25,
        VK_UP = 0x26,
        VK_RIGHT = 0x27,
        VK_DOWN = 0x28,
        VK_SELECT = 0x29,
        VK_PRINT = 0x2A,
        VK_EXECUTE = 0x2B,
        VK_SNAPSHOT = 0x2C,
        VK_INSERT = 0x2D,
        VK_DELETE = 0x2E,
        VK_HELP = 0x2F,
        VK_0 = 0x30,
        VK_1 = 0x31,
        VK_2 = 0x32,
        VK_3 = 0x33,
        VK_4 = 0x34,
        VK_5 = 0x35,
        VK_6 = 0x36,
        VK_7 = 0x37,
        VK_8 = 0x38,
        VK_9 = 0x39,
        VK_A = 0x41,
        VK_B = 0x42,
        VK_C = 0x43,
        VK_D = 0x44,
        VK_E = 0x45,
        VK_F = 0x46,
        VK_G = 0x47,
        VK_H = 0x48,
        VK_I = 0x49,
        VK_J = 0x4A,
        VK_K = 0x4B,
        VK_L = 0x4C,
        VK_M = 0x4D,
        VK_N = 0x4E,
        VK_O = 0x4F,
        VK_P = 0x50,
        VK_Q = 0x51,
        VK_R = 0x52,
        VK_S = 0x53,
        VK_T = 0x54,
        VK_U = 0x55,
        VK_V = 0x56,
        VK_W = 0x57,
        VK_X = 0x58,
        VK_Y = 0x59,
        VK_Z = 0x5A,
        VK_LWIN = 0x5B,
        VK_RWIN = 0x5C,
        VK_APPS = 0x5D,
        VK_SLEEP = 0x5F,
        VK_NUMPAD0 = 0x60,
        VK_NUMPAD1 = 0x61,
        VK_NUMPAD2 = 0x62,
        VK_NUMPAD3 = 0x63,
        VK_NUMPAD4 = 0x64,
        VK_NUMPAD5 = 0x65,
        VK_NUMPAD6 = 0x66,
        VK_NUMPAD7 = 0x67,
        VK_NUMPAD8 = 0x68,
        VK_NUMPAD9 = 0x69,
        VK_MULTIPLY = 0x6A,
        VK_ADD = 0x6B,
        VK_SEPARATOR = 0x6C,
        VK_SUBTRACT = 0x6D,
        VK_DECIMAL = 0x6E,
        VK_DIVIDE = 0x6F,
        VK_F1 = 0x70,
        VK_F2 = 0x71,
        VK_F3 = 0x72,
        VK_F4 = 0x73,
        VK_F5 = 0x74,
        VK_F6 = 0x75,
        VK_F7 = 0x76,
        VK_F8 = 0x77,
        VK_F9 = 0x78,
        VK_F10 = 0x79,
        VK_F11 = 0x7A,
        VK_F12 = 0x7B,
        VK_F13 = 0x7C,
        VK_F14 = 0x7D,
        VK_F15 = 0x7E,
        VK_F16 = 0x7F,
        VK_F17 = 0x80,
        VK_F18 = 0x81,
        VK_F19 = 0x82,
        VK_F20 = 0x83,
        VK_F21 = 0x84,
        VK_F22 = 0x85,
        VK_F23 = 0x86,
        VK_F24 = 0x87,
        VK_NUMLOCK = 0x90,
        VK_SCROLL = 0x91,
        VK_OEM_NEC_EQUAL = 0x92,
        VK_OEM_FJ_JISHO = 0x92,
        VK_OEM_FJ_MASSHOU = 0x93,
        VK_OEM_FJ_TOUROKU = 0x94,
        VK_OEM_FJ_LOYA = 0x95,
        VK_OEM_FJ_ROYA = 0x96,
        VK_LSHIFT = 0xA0,
        VK_RSHIFT = 0xA1,
        VK_LCONTROL = 0xA2,
        VK_RCONTROL = 0xA3,
        VK_LMENU = 0xA4,
        VK_RMENU = 0xA5,
        VK_BROWSER_BACK = 0xA6,
        VK_BROWSER_FORWARD = 0xA7,
        VK_BROWSER_REFRESH = 0xA8,
        VK_BROWSER_STOP = 0xA9,
        VK_BROWSER_SEARCH = 0xAA,
        VK_BROWSER_FAVORITES = 0xAB,
        VK_BROWSER_HOME = 0xAC,
        VK_VOLUME_MUTE = 0xAD,
        VK_VOLUME_DOWN = 0xAE,
        VK_VOLUME_UP = 0xAF,
        VK_MEDIA_NEXT_TRACK = 0xB0,
        VK_MEDIA_PREV_TRACK = 0xB1,
        VK_MEDIA_STOP = 0xB2,
        VK_MEDIA_PLAY_PAUSE = 0xB3,
        VK_LAUNCH_MAIL = 0xB4,
        VK_LAUNCH_MEDIA_SELECT = 0xB5,
        VK_LAUNCH_APP1 = 0xB6,
        VK_LAUNCH_APP2 = 0xB7,
        VK_OEM_1 = 0xBA,
        VK_OEM_PLUS = 0xBB,
        VK_OEM_COMMA = 0xBC,
        VK_OEM_MINUS = 0xBD,
        VK_OEM_PERIOD = 0xBE,
        VK_OEM_2 = 0xBF,
        VK_OEM_3 = 0xC0,
        VK_OEM_4 = 0xDB,
        VK_OEM_5 = 0xDC,
        VK_OEM_6 = 0xDD,
        VK_OEM_7 = 0xDE,
        VK_OEM_8 = 0xDF,
        VK_OEM_AX = 0xE1,
        VK_OEM_102 = 0xE2,
        VK_ICO_HELP = 0xE3,
        VK_ICO_00 = 0xE4,
        VK_PROCESSKEY = 0xE5,
        VK_ICO_CLEAR = 0xE6,
        VK_PACKET = 0xE7,
        VK_OEM_RESET = 0xE9,
        VK_OEM_JUMP = 0xEA,
        VK_OEM_PA1 = 0xEB,
        VK_OEM_PA2 = 0xEC,
        VK_OEM_PA3 = 0xED,
        VK_OEM_WSCTRL = 0xEE,
        VK_OEM_CUSEL = 0xEF,
        VK_OEM_ATTN = 0xF0,
        VK_OEM_FINISH = 0xF1,
        VK_OEM_COPY = 0xF2,
        VK_OEM_AUTO = 0xF3,
        VK_OEM_ENLW = 0xF4,
        VK_OEM_BACKTAB = 0xF5,
        VK_ATTN = 0xF6,
        VK_CRSEL = 0xF7,
        VK_EXSEL = 0xF8,
        VK_EREOF = 0xF9,
        VK_PLAY = 0xFA,
        VK_ZOOM = 0xFB,
        VK_NONAME = 0xFC,
        VK_PA1 = 0xFD,
        VK_OEM_CLEAR = 0xFE
    }

    [Flags]
    public enum MouseEvent : short {
        MOUSEEVENTF_MOVE = 0x0001,
        MOUSEEVENTF_LEFTDOWN = 0x0002,
        MOUSEEVENTF_LEFTUP = 0x0004,
        MOUSEEVENTF_RIGHTDOWN = 0x0008,
        MOUSEEVENTF_RIGHTUP = 0x0010,
        MOUSEEVENTF_MIDDLEDOWN = 0x0020,
        MOUSEEVENTF_MIDDLEUP = 0x0040,
        MOUSEEVENTF_XDOWN = 0x0080,
        MOUSEEVENTF_XUP = 0x0100,
        MOUSEEVENTF_WHEEL = 0x0800
    }
}
