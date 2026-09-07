using System.Collections.Concurrent;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Win32.SimpleGui;

public class Window : IDisposable, ISizeProvider
{
    internal nint Hwnd;

    private const string ClassName = "Win32.SimpleGui.Window";

    private static readonly ConcurrentDictionary<nint, Element> Elements = new();
    public static bool AnyElements => !Elements.IsEmpty;
    private static bool _classRegistered;

    private readonly List<Element> _elements = new();
    private readonly ManualLayout _rootLayout;
    private GCHandle _self;
    private Font _font;
    private Icon _icon;
    private int _width;
    private int _height;
    private int _frameWidth;
    private int _frameHeight;
    private bool _canMinimize = true;
    private bool _canMaximize = true;
    private bool _canResize = true;
    private bool _closed;

    public Window(string title, int width, int height, Font font, Icon icon = null)
    {
        EnsureClass();
        _font = font ?? throw new ArgumentNullException(nameof(font));
        _icon = icon;
        _width = width;
        _height = height;
        _self = GCHandle.Alloc(this);
        Hwnd = NativeBindings.CreateWindowExW(NativeBindings.WS_EX_COMPOSITED, ClassName, title,
            NativeBindings.WS_OVERLAPPEDWINDOW | NativeBindings.WS_CLIPCHILDREN,
            NativeBindings.CW_USEDEFAULT, NativeBindings.CW_USEDEFAULT,
            Application.ScaleDip(width), Application.ScaleDip(height),
            0, 0, NativeBindings.GetModuleHandleW(null), GCHandle.ToIntPtr(_self));
        if (Hwnd == 0)
        {
            _self.Free();
            throw new InvalidOperationException("CreateWindowExW failed.");
        }
        MeasureFrame();
        NativeBindings.SetWindowPos(Hwnd, 0, 0, 0,
            Application.ScaleDip(width) + _frameWidth, Application.ScaleDip(height) + _frameHeight,
            NativeBindings.SWP_NOMOVE | NativeBindings.SWP_NOZORDER);
        _rootLayout = new ManualLayout();
        Attach(_rootLayout);
        ApplyIcon();

        // Windows can replace a process's first ShowWindow command with STARTUPINFO.wShowWindow.
        // Users of this library may be running under some cursed embedded setup or whatever, so
        // avoid this issue by always explicitly showing the window twice. Idk man.
        NativeBindings.ShowWindow(Hwnd, NativeBindings.SW_SHOWNORMAL);
        NativeBindings.ShowWindow(Hwnd, NativeBindings.SW_SHOWNORMAL);
        if (Application.DarkModeEnabled)
        {
            int on = 1;
            NativeBindings.DwmSetWindowAttribute(Hwnd, NativeBindings.DWMWA_USE_IMMERSIVE_DARK_MODE, ref on, sizeof(int));
        }
    }

    public string Title
    {
        get => Hwnd != 0 ? NativeBindings.GetWindowText(Hwnd) : "";
        set
        {
            if (Hwnd != 0) NativeBindings.SetWindowTextW(Hwnd, value);
        }
    }

    public Icon Icon
    {
        get => _icon;
        set
        {
            _icon = value;
            ApplyIcon();
        }
    }

    /// <summary>
    /// Client area size in DIPs; element coordinates are client-relative DIPs.
    /// The getter returns the achieved client size (may differ slightly from the set value).
    /// </summary>
    public int Width
    {
        get => _width;
        set
        {
            _width = value;
            if (Hwnd != 0) NativeBindings.SetWindowPos(Hwnd, 0, 0, 0, Application.ScaleDip(value) + _frameWidth, Application.ScaleDip(_height) + _frameHeight, NativeBindings.SWP_NOMOVE | NativeBindings.SWP_NOZORDER);
        }
    }

    /// <summary>
    /// Client area size in DIPs; element coordinates are client-relative DIPs.
    /// The getter returns the achieved client size (may differ slightly from the set value).
    /// </summary>
    public int Height
    {
        get => _height;
        set
        {
            _height = value;
            if (Hwnd != 0) NativeBindings.SetWindowPos(Hwnd, 0, 0, 0, Application.ScaleDip(_width) + _frameWidth, Application.ScaleDip(value) + _frameHeight, NativeBindings.SWP_NOMOVE | NativeBindings.SWP_NOZORDER);
        }
    }

    int ISizeProvider.ClientWidth => (int)Math.Round(ClientSize.Width / Application.Scale);
    int ISizeProvider.ClientHeight => (int)Math.Round(ClientSize.Height / Application.Scale);

    private (int Width, int Height) ClientSize => Hwnd != 0 ? NativeBindings.GetClientSize(Hwnd) : (_width, _height);

    public bool CanMinimize
    {
        get => _canMinimize;
        set
        {
            _canMinimize = value;
            UpdateWindowStyle();
        }
    }

    public bool CanMaximize
    {
        get => _canMaximize;
        set
        {
            _canMaximize = value;
            UpdateWindowStyle();
        }
    }

    public bool CanResize
    {
        get => _canResize;
        set
        {
            _canResize = value;
            UpdateWindowStyle();
        }
    }

    public enum WindowState
    {
        Normal,
        Minimized,
        Maximized
    }

    public WindowState State
    {
        get => NativeBindings.IsIconic(Hwnd) ? WindowState.Minimized
             : NativeBindings.IsZoomed(Hwnd) ? WindowState.Maximized
             : WindowState.Normal;
        set => NativeBindings.ShowWindow(Hwnd, value switch
        {
            WindowState.Minimized => NativeBindings.SW_MINIMIZE,
            WindowState.Maximized => NativeBindings.SW_MAXIMIZE,
            _ => NativeBindings.SW_RESTORE,
        });
    }

    public ObservableList<Element> Children => _rootLayout.Children;
    public void Arrange() => _rootLayout?.Arrange();

    public Action OnResize;
    public bool AutoLayoutOnResize { get; set; } = true;

    // return true to proceed with the close
    public Func<bool> OnCloseRequest;

    /// <summary>
    /// Shows a message box attached to this window: the window is blocked while it is open
    /// (clicks are rejected with the system ding), and the box closes with the window,
    /// reporting as if the user cancelled.
    /// </summary>
    public bool MessageBox(string title, string message, Icon icon = null, MessageBoxIcon image = MessageBoxIcon.None, bool canCancel = true) =>
        Application.ShowMessageBox(Hwnd, title, message, icon, image, canCancel);

    // thread-safe: the close request is posted to the window's owning thread
    public void Dispose()
    {
        if (Hwnd != 0 && !_closed) NativeBindings.PostMessageW(Hwnd, NativeBindings.WM_CLOSE, 0, 0);
    }

    internal void Attach(Element element)
    {
        if (element.Attached) return;
        element.Attached = true;
        _elements.Add(element);
        if (element is BaseLayout layout)
        {
            // the root layout hangs off the window; nested layouts get their Parent
            // from the layout that contains them
            if (layout.Parent == null) layout.Parent = this;
            layout.Children.Added += Attach;
            layout.Children.Removed += Detach;
            layout.Children.Set += OnChildSet;
            foreach (var child in layout.Children) Attach(child);
            return;
        }
        element.Hwnd = CreateControl(element);
        Elements[element.Hwnd] = element;
        NativeBindings.SendMessage(element.Hwnd, NativeBindings.WM_SETFONT, (element.Font ?? _font).Handle, 1);
        if (element.Disabled) NativeBindings.EnableWindow(element.Hwnd, false);
        element.OnAttached();
    }

    internal void Detach(Element element)
    {
        if (!element.Attached) return;
        element.Attached = false;
        _elements.Remove(element);
        if (element is BaseLayout layout)
        {
            layout.Children.Added -= Attach;
            layout.Children.Removed -= Detach;
            layout.Children.Set -= OnChildSet;
            foreach (var child in layout.Children) Detach(child);
            return;
        }
        if (element.Hwnd != 0)
        {
            Elements.TryRemove(element.Hwnd, out _);
            NativeBindings.DestroyWindow(element.Hwnd);
            element.Hwnd = 0;
        }
        element.OnDetached();
    }

    private void OnChildSet(int index, Element oldElement, Element newElement)
    {
        Detach(oldElement);
        Attach(newElement);
    }

    private nint CreateControl(Element element)
    {
        uint style = NativeBindings.WS_CHILD | NativeBindings.WS_VISIBLE | NativeBindings.WS_TABSTOP | NativeBindings.WS_CLIPSIBLINGS;
        uint exStyle = 0;
        string className;
        string text = element.TextValue ?? "";
        int x = element.X == BaseLayout.Fill ? 0 : element.X;
        int y = element.Y == BaseLayout.Fill ? 0 : element.Y;
        int width = element.Width == BaseLayout.Fill ? 0 : element.Width;
        int height = element.Height == BaseLayout.Fill ? 0 : element.Height;

        switch (element)
        {
            case Panel:
                className = "STATIC";
                style = NativeBindings.WS_CHILD | NativeBindings.WS_VISIBLE | NativeBindings.WS_CLIPSIBLINGS;
                break;
            case Label label:
                className = "STATIC";
                if (label.CenterHorizontally) style |= NativeBindings.SS_CENTER;
                if (label.CenterVertically) style |= NativeBindings.SS_CENTERIMAGE;
                break;
            case Button:
                className = "BUTTON";
                break;
            case Checkbox:
                className = "BUTTON";
                style |= NativeBindings.BS_AUTOCHECKBOX;
                break;
            case TextBox:
                className = "EDIT";
                style |= NativeBindings.ES_AUTOHSCROLL;
                exStyle = NativeBindings.WS_EX_CLIENTEDGE;
                break;
            case Slider:
                className = "msctls_trackbar32";
                break;
            case ProgressBar:
                className = "msctls_progress32";
                break;
            case ListBox:
                className = "LISTBOX";
                style |= NativeBindings.LBS_NOTIFY | NativeBindings.WS_VSCROLL;
                exStyle = NativeBindings.WS_EX_CLIENTEDGE;
                break;
            default:
                throw new NotSupportedException(element.GetType().Name);
        }

        return NativeBindings.CreateWindowExW(exStyle, className, text, style,
            Application.ScaleDip(x), Application.ScaleDip(y),
            Application.ScaleDip(width), Application.ScaleDip(height), Hwnd, 0, NativeBindings.GetModuleHandleW(null), 0);
    }

    private void ApplyIcon()
    {
        if (_icon == null || Hwnd == 0) return;
        NativeBindings.SendMessage(Hwnd, NativeBindings.WM_SETICON, 0, _icon.SmallHandle);
        NativeBindings.SendMessage(Hwnd, NativeBindings.WM_SETICON, 1, _icon.BigHandle);
    }

    private void UpdateWindowStyle()
    {
        if (Hwnd == 0) return;
        uint style = (uint)NativeBindings.GetWindowLongPtrW(Hwnd, NativeBindings.GWL_STYLE);
        SetStyleBit(ref style, NativeBindings.WS_MAXIMIZEBOX, _canMaximize);
        SetStyleBit(ref style, NativeBindings.WS_MINIMIZEBOX, _canMinimize);
        SetStyleBit(ref style, NativeBindings.WS_THICKFRAME, _canResize);
        NativeBindings.SetWindowLongPtrW(Hwnd, NativeBindings.GWL_STYLE, (nint)style);
        NativeBindings.SetWindowPos(Hwnd, 0, 0, 0, 0, 0, NativeBindings.SWP_NOMOVE | NativeBindings.SWP_NOSIZE | NativeBindings.SWP_NOZORDER | NativeBindings.SWP_NOACTIVATE | NativeBindings.SWP_FRAMECHANGED);
        MeasureFrame();
    }

    // outer size minus client size, in pixels; depends on style and DPI
    private void MeasureFrame()
    {
        NativeBindings.GetWindowRect(Hwnd, out var outer);
        var (clientWidth, clientHeight) = NativeBindings.GetClientSize(Hwnd);
        _frameWidth = (int)((outer.Right - outer.Left) - clientWidth);
        _frameHeight = (int)((outer.Bottom - outer.Top) - clientHeight);
    }

    private static void SetStyleBit(ref uint style, uint bit, bool on)
    {
        if (on) style |= bit;
        else style &= ~bit;
    }

    private void OnSize(nint wParam)
    {
        if ((int)wParam == NativeBindings.SIZE_MINIMIZED) return;
        var (clientWidth, clientHeight) = NativeBindings.GetClientSize(Hwnd);
        _width = (int)Math.Round(clientWidth / Application.Scale);
        _height = (int)Math.Round(clientHeight / Application.Scale);
        OnResize?.Invoke();
        if (AutoLayoutOnResize) Arrange();
    }

    private void OnDestroyed()
    {
        _closed = true;
        foreach (var element in _elements)
        {
            element.Attached = false;
            if (element.Hwnd != 0) Elements.TryRemove(element.Hwnd, out _);
            element.Hwnd = 0;
            if (element is BaseLayout layout)
            {
                layout.Children.Added -= Attach;
                layout.Children.Removed -= Detach;
                layout.Children.Set -= OnChildSet;
            }
            element.OnDetached();
        }
        _elements.Clear();
        NativeBindings.SetWindowLongPtrW(Hwnd, NativeBindings.GWLP_USERDATA, 0);
        _self.Free();
        _self = default;
    }

    private void RouteCommand(nint wParam, nint lParam)
    {
        int code = (int)((wParam >> 16) & 0xFFFF);
        if (!Elements.TryGetValue(lParam, out var element) || element.Disabled) return;
        switch (element)
        {
            case Button button when code == NativeBindings.BN_CLICKED:
                button.OnClick?.Invoke(button);
                break;
            case Checkbox checkbox when code == NativeBindings.BN_CLICKED:
                checkbox.OnCheckedChanged?.Invoke(checkbox, checkbox.Checked);
                break;
            case TextBox textBox when code == NativeBindings.EN_CHANGE:
                textBox.OnTextChanged?.Invoke(textBox, textBox.Text);
                break;
            case ListBox listBox when code == NativeBindings.LBN_SELCHANGE && !listBox.SuppressSelectionEvent:
                listBox.OnSelectedIndexChanged?.Invoke(listBox, listBox.SelectedIndex);
                break;
        }
    }

    private void RouteScroll(nint lParam)
    {
        if (Elements.TryGetValue(lParam, out var element) && element is Slider slider)
            slider.OnValueChanged?.Invoke(slider, slider.Value);
    }

    private nint ControlColor(nint hdc, nint control)
    {
        if (Elements.TryGetValue(control, out var element) && element is ColorableElement colorable)
        {
            if (colorable.Foreground != Color.Empty) NativeBindings.SetTextColor(hdc, ColorableElement.ToColorRef(colorable.Foreground));
            if (colorable.BackgroundBrush != 0)
            {
                NativeBindings.SetBkMode(hdc, NativeBindings.TRANSPARENT);
                return colorable.BackgroundBrush;
            }
        }
        return NativeBindings.COLOR_WINDOW + 1;
    }

    private static Window FromHwnd(nint hwnd)
    {
        nint ptr = NativeBindings.GetWindowLongPtrW(hwnd, NativeBindings.GWLP_USERDATA);
        return ptr != 0 ? (Window)GCHandle.FromIntPtr(ptr).Target : null;
    }

    private static void EnsureClass()
    {
        if (_classRegistered) return;
        _classRegistered = true;

        NativeBindings.InitCommonControlsEx(new INITCOMMONCONTROLSEX
        {
            Size = sizeof(int) * 2,
            Classes = NativeBindings.ICC_BAR_CLASSES | NativeBindings.ICC_PROGRESS_CLASSES,
        });

        nint wndProc;
        unsafe { wndProc = (nint)(delegate* unmanaged<nint, uint, nint, nint, nint>)&WndProc; }

        var windowClass = new WNDCLASSEXW
        {
            CbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
            WndProc = wndProc,
            Instance = NativeBindings.GetModuleHandleW(null),
            Cursor = NativeBindings.LoadCursorW(0, NativeBindings.IDC_ARROW),
            Background = NativeBindings.COLOR_WINDOW + 1,
            ClassName = Marshal.StringToHGlobalUni(ClassName),
        };
        if (NativeBindings.RegisterClassExW(windowClass) == 0)
            throw new InvalidOperationException("RegisterClassExW failed.");
    }

    [UnmanagedCallersOnly]
    private static nint WndProc(nint hwnd, uint msg, nint wParam, nint lParam)
    {
        var window = FromHwnd(hwnd);
        switch (msg)
        {
            case NativeBindings.WM_NCCREATE:
                unsafe { NativeBindings.SetWindowLongPtrW(hwnd, NativeBindings.GWLP_USERDATA, *(nint*)lParam); }
                break;
            case NativeBindings.WM_SIZE when window != null:
                window.OnSize(wParam);
                break;
            case NativeBindings.WM_COMMAND when window != null:
                window.RouteCommand(wParam, lParam);
                break;
            case NativeBindings.WM_HSCROLL when window != null:
                window.RouteScroll(lParam);
                break;
            case NativeBindings.WM_CTLCOLOREDIT when window != null:
            case NativeBindings.WM_CTLCOLORLISTBOX when window != null:
            case NativeBindings.WM_CTLCOLORBTN when window != null:
            case NativeBindings.WM_CTLCOLORSTATIC when window != null:
                return window.ControlColor(wParam, lParam);
            case NativeBindings.WM_CLOSE when window != null:
                if (window.OnCloseRequest != null && !window.OnCloseRequest())
                    return 0;
                NativeBindings.DestroyWindow(hwnd);
                return 0;
            case NativeBindings.WM_DESTROY when window != null:
                window.OnDestroyed();
                break;
        }
        return NativeBindings.DefWindowProcW(hwnd, msg, wParam, lParam);
    }
}
