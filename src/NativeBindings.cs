using System.Runtime.InteropServices;

namespace Win32.SimpleGui;

[StructLayout(LayoutKind.Sequential)]
internal struct MSG
{
    public nint Hwnd;
    public uint Message;
    public nint WParam;
    public nint LParam;
    public uint Time;
    public int PtX;
    public int PtY;
}

[StructLayout(LayoutKind.Sequential)]
internal struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct WNDCLASSEXW
{
    public uint CbSize;
    public uint Style;
    public nint WndProc;
    public int ClsExtra;
    public int WndExtra;
    public nint Instance;
    public nint Icon;
    public nint Cursor;
    public nint Background;
    public nint MenuName;
    public nint ClassName;
    public nint IconSm;
}

[StructLayout(LayoutKind.Sequential)]
internal struct INITCOMMONCONTROLSEX
{
    public int Size;
    public int Classes;
}

[StructLayout(LayoutKind.Sequential)]
internal struct ACTCTXW
{
    public uint CbSize;
    public uint Flags;
    public nint Source;
    public ushort ProcessorArchitecture;
    public ushort LangId;
    public nint AssemblyDirectory;
    public nint ResourceName;
    public nint ApplicationName;
    public nint Module;
}

internal static partial class NativeBindings
{
    internal const uint WM_SIZE = 0x0005;
    internal const uint WM_DESTROY = 0x0002;
    internal const uint WM_CLOSE = 0x0010;
    internal const uint WM_SETICON = 0x0080;
    internal const uint WM_SETFONT = 0x0030;
    internal const uint WM_NCCREATE = 0x0081;
    internal const uint WM_COMMAND = 0x0111;
    internal const uint WM_CTLCOLOREDIT = 0x0133;
    internal const uint WM_CTLCOLORLISTBOX = 0x0134;
    internal const uint WM_HSCROLL = 0x0114;
    internal const uint WM_CTLCOLORSTATIC = 0x0138;

    internal const uint WS_CHILD = 0x40000000;
    internal const uint WS_VISIBLE = 0x10000000;
    internal const uint WS_CLIPCHILDREN = 0x02000000;
    internal const uint WS_CLIPSIBLINGS = 0x04000000;
    internal const uint WS_VSCROLL = 0x00200000;
    internal const uint WS_TABSTOP = 0x00010000;
    internal const uint WS_OVERLAPPEDWINDOW = 0x00CF0000;
    internal const uint WS_EX_CLIENTEDGE = 0x00000200;
    internal const uint WS_MAXIMIZEBOX = 0x00010000;
    internal const uint WS_MINIMIZEBOX = 0x00020000;
    internal const uint WS_THICKFRAME = 0x00040000;

    internal const int GWL_STYLE = -16;
    internal const int SIZE_MINIMIZED = 1;

    internal const uint SWP_NOSIZE = 0x0001;
    internal const uint SWP_NOMOVE = 0x0002;
    internal const uint SWP_NOZORDER = 0x0004;
    internal const uint SWP_NOACTIVATE = 0x0010;
    internal const uint SWP_FRAMECHANGED = 0x0020;

    internal static readonly nint HWND_TOP = 0;
    internal static readonly nint HWND_BOTTOM = 1;

    internal const int WH_CBT = 5;
    internal const int HCBT_ACTIVATE = 5;

    internal static readonly nint INVALID_HANDLE_VALUE = new nint(-1);

    internal const uint BS_AUTOCHECKBOX = 0x00000003;
    internal const uint ES_AUTOHSCROLL = 0x00000080;
    internal const uint LBS_NOTIFY = 0x0001;

    internal const int GWLP_USERDATA = -21;
    internal const int CW_USEDEFAULT = unchecked((int)0x80000000);

    internal const uint BM_GETCHECK = 0x00F0;
    internal const uint BM_SETCHECK = 0x00F1;
    internal const nint BST_CHECKED = 1;

    internal const uint LB_ADDSTRING = 0x0180;
    internal const uint LB_INSERTSTRING = 0x0143;
    internal const uint LB_RESETCONTENT = 0x0184;
    internal const uint LB_SETCURSEL = 0x0186;
    internal const uint LB_GETCURSEL = 0x0188;
    internal const uint LB_DELETESTRING = 0x0183;

    internal const uint TBM_GETPOS = 0x0400;
    internal const uint TBM_SETPOS = 0x0405;
    internal const uint TBM_SETRANGE = 0x0406;

    internal const uint PBM_SETPOS = 0x0402;
    internal const uint PBM_SETRANGE32 = 0x0406;

    internal const int BN_CLICKED = 0;
    internal const int LBN_SELCHANGE = 1;
    internal const int EN_CHANGE = 0x0300;

    internal const int ICC_BAR_CLASSES = 0x0004;
    internal const int ICC_PROGRESS_CLASSES = 0x0020;

    internal const uint MB_OK = 0x00000000;
    internal const uint MB_OKCANCEL = 0x00000001;
    internal const uint MB_ICONINFORMATION = 0x00000040;
    internal const int IDOK = 1;

    internal const int SW_MINIMIZE = 6;
    internal const int SW_MAXIMIZE = 3;
    internal const int SW_SHOWNORMAL = 1;
    internal const int SW_RESTORE = 9;

    internal const nint IDC_ARROW = 32512;
    internal const nint COLOR_WINDOW = 5;

    internal const int DEFAULT_CHARSET = 1;
    internal const int CLEARTYPE_QUALITY = 5;

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial nint GetModuleHandleW(string moduleName);

    [LibraryImport("user32.dll", EntryPoint = "RegisterClassExW")]
    internal static partial ushort RegisterClassExW(in WNDCLASSEXW windowClass);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial nint CreateWindowExW(uint exStyle, string className, string windowName, uint style, int x, int y, int width, int height, nint parent, nint menu, nint instance, nint param);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DestroyWindow(nint hwnd);

    [LibraryImport("user32.dll")]
    internal static partial nint DefWindowProcW(nint hwnd, uint msg, nint wParam, nint lParam);

    [LibraryImport("user32.dll")]
    internal static partial int GetMessageW(out MSG msg, nint hwnd, uint min, uint max);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool TranslateMessage(ref MSG msg);

    [LibraryImport("user32.dll")]
    internal static partial nint DispatchMessageW(ref MSG msg);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool PostMessageW(nint hwnd, uint msg, nint wParam, nint lParam);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int MessageBoxW(nint hwnd, string text, string caption, uint type);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetWindowTextW(nint hwnd, string text);

    [LibraryImport("user32.dll")]
    internal static partial int GetWindowTextW(nint hwnd, nint buffer, int max);

    [LibraryImport("user32.dll")]
    internal static partial nint SendMessageW(nint hwnd, uint msg, nint wParam, nint lParam);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial nint SendMessageW(nint hwnd, uint msg, nint wParam, string lParam);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool MoveWindow(nint hwnd, int x, int y, int width, int height, [MarshalAs(UnmanagedType.Bool)] bool repaint);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetClientRect(nint hwnd, out RECT rect);

    [LibraryImport("user32.dll")]
    internal static partial nint LoadCursorW(nint instance, nint cursorName);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ShowWindow(nint hwnd, int command);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsIconic(nint hwnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsZoomed(nint hwnd);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    internal static partial nint GetWindowLongPtrW(nint hwnd, int index);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    internal static partial nint SetWindowLongPtrW(nint hwnd, int index, nint value);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool EnableWindow(nint hwnd, [MarshalAs(UnmanagedType.Bool)] bool enable);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool InvalidateRect(nint hwnd, nint rect, [MarshalAs(UnmanagedType.Bool)] bool erase);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetProcessDpiAwarenessContext(nint value);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetProcessDPIAware();

    [LibraryImport("user32.dll")]
    internal static partial uint GetDpiForSystem();

    internal static readonly nint DPI_AWARENESS_CONTEXT_SYSTEM_AWARE = new nint(-2);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetWindowRect(nint hwnd, out RECT rect);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetWindowPos(nint hwnd, nint insertAfter, int x, int y, int width, int height, uint flags);

    [LibraryImport("user32.dll")]
    internal static partial nint CreateIconFromResourceEx(nint presBits, int resSize, [MarshalAs(UnmanagedType.Bool)] bool fIcon, int version, int width, int height, uint flags);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DestroyIcon(nint icon);

    [LibraryImport("user32.dll")]
    internal static partial nint SetWindowsHookExW(int idHook, nint procedure, nint module, uint threadId);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool UnhookWindowsHookEx(nint hook);

    [LibraryImport("user32.dll")]
    internal static partial nint CallNextHookEx(nint hook, int code, nint wParam, nint lParam);

    [LibraryImport("kernel32.dll")]
    internal static partial uint GetCurrentThreadId();

    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial nint SetTimer(nint hwnd, nint id, uint intervalMilliseconds, nint procedure);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool KillTimer(nint hwnd, nint id);

    [LibraryImport("kernel32.dll")]
    internal static partial nint CreateActCtxW(ref ACTCTXW actctx);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ActivateActCtx(nint actCtx, out nint cookie);

    internal static unsafe nint CreateIconFromResource(byte[] data, int offset, int size, int dimension)
    {
        fixed (byte* bits = data)
            return CreateIconFromResourceEx((nint)(bits + offset), size, true, 0x00030000, dimension, dimension, 0);
    }

    [LibraryImport("gdi32.dll")]
    internal static partial nint CreateSolidBrush(uint color);

    [LibraryImport("gdi32.dll")]
    internal static partial uint SetTextColor(nint hdc, uint color);

    [LibraryImport("gdi32.dll")]
    internal static partial int SetBkMode(nint hdc, int mode);

    internal const int TRANSPARENT = 1;

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DeleteObject(nint obj);

    [LibraryImport("gdi32.dll", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial nint CreateFontW(int height, int width, int escapement, int orientation, int weight, uint italic, uint underline, uint strikeout, uint charset, uint outPrecision, uint clipPrecision, uint quality, uint pitchAndFamily, string faceName);

    [LibraryImport("comctl32.dll")]
    internal static partial void InitCommonControlsEx(in INITCOMMONCONTROLSEX icc);

    internal static nint SendMessage(nint hwnd, uint msg, nint wParam = 0, nint lParam = 0) => SendMessageW(hwnd, msg, wParam, lParam);

    internal static unsafe string GetWindowText(nint hwnd)
    {
        const int max = 512;
        char* buffer = stackalloc char[max];
        GetWindowTextW(hwnd, (nint)buffer, max);
        return new string(buffer);
    }

    internal static (int Width, int Height) GetClientSize(nint hwnd)
    {
        GetClientRect(hwnd, out var rect);
        return (rect.Right - rect.Left, rect.Bottom - rect.Top);
    }

    internal static nint MakeLParam(int low, int high) => (nint)((low & 0xFFFF) | ((high & 0xFFFF) << 16));
}
