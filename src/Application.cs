using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace Win32.SimpleGui;

public enum MessageBoxIcon
{
    None = 0x00,
    Error = 0x10,
    Question = 0x20,
    Warning = 0x30,
    Information = 0x40,
}

public static class Application
{
    private static Icon _pendingIcon;
    private static nint _hook;
    private static nint _actCtx;
    private static readonly object _actCtxGate = new();

    [ThreadStatic]
    private static bool _threadActivated;

    private static readonly ConcurrentDictionary<nint, Func<bool>> _registeredTimers = new();
    private static unsafe readonly nint _timerProc =
        (nint)(delegate* unmanaged<nint, uint, nuint, uint, void>)&TimerProc;

    // DIP to pixel factor; 1 until EnableHiDPISupportForCurrentProcess runs
    internal static float Scale = 1f;

    internal static int ScaleDip(int value) => (int)Math.Round(value * Scale);

    public static Window CreateWindow(string title, int width, int height, Font font, Icon icon = null) =>
        new(title, width, height, font, icon);

    /// <summary>
    /// Opts the process into system DPI awareness, so text and controls render crisp on
    /// scaled displays. Process-wide; call once before creating fonts or windows.
    /// All element and window sizes are then treated as DIPs.
    /// </summary>
    public static void EnableHiDPISupportForCurrentProcess()
    {
        if (!NativeBindings.SetProcessDpiAwarenessContext(NativeBindings.DPI_AWARENESS_CONTEXT_SYSTEM_AWARE))
            NativeBindings.SetProcessDPIAware();
        Scale = NativeBindings.GetDpiForSystem() / 96f;
    }

    /// <summary>
    /// Opts the calling thread into common controls v6 (system themed buttons, etc.).
    /// The scope is deliberately per-thread: only windows created on this thread after the
    /// call are themed, so the caller decides which threads get styled UI.
    /// Use before creating a window. Safe to call from multiple threads.
    /// </summary>
    public static void EnableVisualStylesForCurrentThread()
    {
        if (_threadActivated) return;
        try
        {
            EnsureActCtx();
            if (_actCtx != NativeBindings.INVALID_HANDLE_VALUE)
                NativeBindings.ActivateActCtx(_actCtx, out _);
            _threadActivated = true;
        }
        catch
        {
            // best-effort: unstyled fallback
        }
    }

    private static void EnsureActCtx()
    {
        if (_actCtx != 0) return;
        lock (_actCtxGate)
        {
            if (_actCtx != 0) return;

            // manifest temp file lives for the process lifetime; SxS may re-read it.
            // the PID suffix makes it unique among live processes, so only threads of this
            // process can contend; retry rides out transient holders (e.g. AV scanners)
            string path = Path.Combine(Path.GetTempPath(), $"Win32.SimpleGui.{Environment.ProcessId}.manifest");
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    File.WriteAllText(path, Manifest);
                    break;
                }
                catch (IOException) when (attempt < 3)
                {
                    Thread.Sleep(10 * attempt);
                }
            }

            // the exe manifest already holds the process default, so push the context per thread
            var actctx = new ACTCTXW
            {
                CbSize = (uint)Marshal.SizeOf<ACTCTXW>(),
                Source = Marshal.StringToHGlobalUni(path),
            };
            _actCtx = NativeBindings.CreateActCtxW(ref actctx);
        }
    }

    /// <summary>
    /// Shows a message box with the specified title, message, optional icon, and standard system image.
    /// When canCancel is false, only an OK button is shown.
    /// </summary>
    /// <returns>True if the user clicked OK; otherwise, false.</returns>
    public static bool MessageBox(string title, string message, Icon icon = null, MessageBoxIcon image = MessageBoxIcon.None, bool canCancel = true)
    {
        if (icon != null)
        {
            _pendingIcon = icon;
            unsafe { _hook = NativeBindings.SetWindowsHookExW(NativeBindings.WH_CBT, (nint)(delegate* unmanaged<int, nint, nint, nint>)&CbtProc, 0, NativeBindings.GetCurrentThreadId()); }
        }
        uint buttons = canCancel ? NativeBindings.MB_OKCANCEL : NativeBindings.MB_OK;
        int result = NativeBindings.MessageBoxW(0, message, title, buttons | (uint)image);
        if (_hook != 0)
        {
            NativeBindings.UnhookWindowsHookEx(_hook);
            _hook = 0;
            _pendingIcon = null;
        }
        return result == NativeBindings.IDOK;
    }

    // Hook to set an icon on a message box window.
    [UnmanagedCallersOnly]
    private static nint CbtProc(int code, nint wParam, nint lParam)
    {
        if (code == NativeBindings.HCBT_ACTIVATE && _pendingIcon != null)
        {
            NativeBindings.SendMessageW(wParam, NativeBindings.WM_SETICON, 0, _pendingIcon.SmallHandle);
            NativeBindings.SendMessageW(wParam, NativeBindings.WM_SETICON, 1, _pendingIcon.BigHandle);
            _pendingIcon = null;
        }
        return NativeBindings.CallNextHookEx(0, code, wParam, lParam);
    }

    /// <summary>
    /// Blocking call that runs the window's event loop until it closes.
    /// </summary>
    public static void RunEventLoop()
    {
        while (NativeBindings.GetMessageW(out var msg, 0, 0, 0) > 0)
        {
            NativeBindings.TranslateMessage(ref msg);
            NativeBindings.DispatchMessageW(ref msg);
            if (!Window.AnyElements) break;
        }

        foreach (var id in _registeredTimers.Keys) NativeBindings.KillTimer(0, id);
        _registeredTimers.Clear();
    }

    /// <summary>
    /// Schedule a Win32 timer to send WM_TIMER, `callback` runs on the thread that runs the event loop.
    /// Schedule it from that thread (typically the UI thread).
    /// If callback returns false, timer will be cancelled.
    /// When all Windows close and `RunEventLoop` breaks, all timers are cancelled automatically.
    /// </summary>
    public static void ScheduleTimer(Func<bool> callback, uint intervalMilliseconds)
    {
        nint id = NativeBindings.SetTimer(0, 0, intervalMilliseconds, _timerProc);
        if (id == 0) throw new InvalidOperationException("SetTimer failed.");
        _registeredTimers[id] = callback;
    }

    [UnmanagedCallersOnly]
    private static void TimerProc(nint hwnd, uint msg, nuint id, uint elapsed)
    {
        if (_registeredTimers.TryGetValue((nint)id, out var callback) && !callback())
        {
            if (_registeredTimers.TryRemove((nint)id, out _))
                NativeBindings.KillTimer(0, (nint)id);
        }
    }

    private const string Manifest =
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <assembly xmlns="urn:schemas-microsoft-com:asm.v1" manifestVersion="1.0">
          <dependency>
            <dependentAssembly>
              <assemblyIdentity type="win32" name="Microsoft.Windows.Common-Controls" version="6.0.0.0" processorArchitecture="*" publicKeyToken="6595b64144ccf1df" language="*"/>
            </dependentAssembly>
          </dependency>
        </assembly>
        """;
}
