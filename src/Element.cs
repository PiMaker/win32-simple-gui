using System.Drawing;
using System.Runtime.CompilerServices;

namespace Win32.SimpleGui;

public abstract class Element : IObservableElement
{
    event Action<IObservableElement> IObservableElement.Changed { add {} remove {} } // does not need to be called, Window handles notifications anyway

    internal nint Hwnd;
    internal bool Attached;

    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; } = 100;
    public int Height { get; set; } = 24;
    public Margin Margin { get; set; }
    public Font Font { get; set; }

    private bool _disabled;

    // no-ops for non-interactive elements (Label, Rectangle, ...)
    public bool Disabled
    {
        get => _disabled;
        set
        {
            _disabled = value;
            if (Hwnd != 0) NativeBindings.EnableWindow(Hwnd, !value);
        }
    }

    public void BringToFront()
    {
        if (Hwnd != 0) NativeBindings.SetWindowPos(Hwnd, NativeBindings.HWND_TOP, 0, 0, 0, 0, NativeBindings.SWP_NOMOVE | NativeBindings.SWP_NOSIZE | NativeBindings.SWP_NOACTIVATE);
    }

    public void SendToBack()
    {
        if (Hwnd != 0) NativeBindings.SetWindowPos(Hwnd, NativeBindings.HWND_BOTTOM, 0, 0, 0, 0, NativeBindings.SWP_NOMOVE | NativeBindings.SWP_NOSIZE | NativeBindings.SWP_NOACTIVATE);
    }

    // relative-to-root position and size assigned by the last Arrange pass
    public int AbsoluteX { get; internal set; }
    public int AbsoluteY { get; internal set; }
    public int AbsoluteWidth { get; internal set; }
    public int AbsoluteHeight { get; internal set; }

#region Text Helpers

    protected virtual int TextLength { get; set; }
    internal string TextInitState
    {
        get;
        set
        {
            field = value;
            TextLength = value?.Length ?? 0;
        }
    }

    protected unsafe void SetTextNoAlloc(TextBuffer value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (Hwnd == 0) throw new InvalidOperationException("Element is not attached to a window.");
        fixed (char* text = value.RawBuffer) NativeBindings.SetWindowTextWNoAlloc(Hwnd, text);
        TextLength = value.Count;
    }

    protected unsafe void GetTextNoAlloc(TextBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        if (Hwnd == 0) throw new InvalidOperationException("Element is not attached to a window.");
        fixed (char* text = buffer.RawBuffer)
            buffer.RawSetCount(NativeBindings.GetWindowTextW(Hwnd, (nint)text, buffer.Capacity + 1));
    }

    internal void SyncTextInitState()
    {
        if (Hwnd != 0)
        {
            var buffer = new TextBuffer(TextLength);
            GetTextNoAlloc(buffer);
            TextInitState = buffer.ToString();
        }
    }

#endregion

    internal virtual void OnAttached() { }
    internal virtual void OnDetached() { }
}

// elements whose text and background colors are controllable through the WM_CTLCOLOR* messages;
// themed buttons and trackbars are not, and are left out
public abstract class ColorableElement : Element
{
    private Color _background;
    private nint _backgroundBrush;

    // Color.Empty keeps the system default
    public Color Foreground { get; set; }

    public Color Background
    {
        get => _background;
        set
        {
            _background = value;
            if (_backgroundBrush != 0) NativeBindings.DeleteObject(_backgroundBrush);
            _backgroundBrush = 0;
            if (Hwnd != 0) NativeBindings.InvalidateRect(Hwnd, 0, true);
        }
    }

    internal nint BackgroundBrush => _backgroundBrush != 0 ? _backgroundBrush
        : _backgroundBrush = Background == Color.Empty ? 0 : NativeBindings.CreateSolidBrush(ToColorRef(Background));

    internal static uint ToColorRef(Color color) => (uint)(color.R | (color.G << 8) | (color.B << 16));

    internal override void OnDetached()
    {
        if (_backgroundBrush != 0) NativeBindings.DeleteObject(_backgroundBrush);
        _backgroundBrush = 0;
    }
}

public abstract class TextElement : Element
{
    public void SetText(TextBuffer buffer) => SetTextNoAlloc(buffer);
    public void SetText([InterpolatedStringHandlerArgument] TextBuffer.StaticInterpolatedStringHandler handler) => SetText(handler.ActiveBuffer);
    public void GetText(TextBuffer buffer) => GetTextNoAlloc(buffer);
    public TextBuffer GetText()
    {
        var buffer = new TextBuffer(TextLength);
        GetTextNoAlloc(buffer);
        return buffer;
    }
}

// I want traits -.-
public abstract class ColorableTextElement : ColorableElement
{
    public void SetText(TextBuffer buffer) => SetTextNoAlloc(buffer);
    public void SetText([InterpolatedStringHandlerArgument] TextBuffer.StaticInterpolatedStringHandler handler) => SetText(handler.ActiveBuffer);
    public void GetText(TextBuffer buffer) => GetTextNoAlloc(buffer);
    public TextBuffer GetText()
    {
        var buffer = new TextBuffer(TextLength);
        GetTextNoAlloc(buffer);
        return buffer;
    }
}

public class Panel : ColorableElement
{
    public Panel(int x, int y, int width, int height, Color color)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        Background = color;
    }
}

public class Label : ColorableTextElement
{
    public Label(string text = "", Alignment alignment = Alignment.Left, bool centerVertically = false)
    {
        TextInitState = text;
        Width = 200;
        Height = 20;

        // can only be set at creation time
        Alignment = alignment;
        CenterVertically = centerVertically;
    }

    public Alignment Alignment { get; }
    public bool CenterVertically { get; }
}

public class Button : TextElement
{
    public Button(string text = "")
    {
        TextInitState = text;
        Height = 28;
    }

    public Action<Button> OnClick;
}

public class TextBox : ColorableTextElement
{
    public TextBox()
    {
        Width = 200;
    }

    public Action<TextBox, TextBuffer> OnTextChanged;

    // actual text length is user input, this specifies max buffer size
    protected override int TextLength { get => 4096; set {} }
}

public class Checkbox : ColorableTextElement
{
    public Checkbox(string text = "")
    {
        TextInitState = text;
        Width = 120;
        Height = 20;
    }

    public bool Checked
    {
        get => Hwnd != 0 && NativeBindings.SendMessage(Hwnd, NativeBindings.BM_GETCHECK) == NativeBindings.BST_CHECKED;
        set
        {
            if (Hwnd != 0) NativeBindings.SendMessage(Hwnd, NativeBindings.BM_SETCHECK, value ? NativeBindings.BST_CHECKED : 0);
        }
    }

    public Action<Checkbox, bool> OnCheckedChanged;
}

public class Slider : Element
{
    private int _min;
    private int _max = 100;

    public Slider()
    {
        Width = 200;
        Height = 28;
    }

    public int Min
    {
        get => _min;
        set
        {
            _min = value;
            if (Hwnd != 0) NativeBindings.SendMessage(Hwnd, NativeBindings.TBM_SETRANGE, 0, NativeBindings.MakeLParam(_min, _max));
        }
    }

    public int Max
    {
        get => _max;
        set
        {
            _max = value;
            if (Hwnd != 0) NativeBindings.SendMessage(Hwnd, NativeBindings.TBM_SETRANGE, 0, NativeBindings.MakeLParam(_min, _max));
        }
    }

    public int Value
    {
        get => Hwnd != 0 ? (int)NativeBindings.SendMessage(Hwnd, NativeBindings.TBM_GETPOS) : 0;
        set
        {
            if (Hwnd != 0) NativeBindings.SendMessage(Hwnd, NativeBindings.TBM_SETPOS, 1, value);
        }
    }

    public Action<Slider, float> OnValueChanged;
}

public class ProgressBar : Element
{
    private int _value;

    public ProgressBar()
    {
        Width = 200;
        Height = 18;
    }

    public int Value
    {
        get => _value;
        set
        {
            _value = Math.Clamp(value, 0, 100);
            if (Hwnd != 0) NativeBindings.SendMessage(Hwnd, NativeBindings.PBM_SETPOS, _value);
        }
    }

    internal override void OnAttached() => NativeBindings.SendMessage(Hwnd, NativeBindings.PBM_SETRANGE32, 0, 100);
}

public class ListBox : ColorableElement
{
    public ListBox()
    {
        Width = 200;
        Height = 120;
    }

    public ObservableList<TextBuffer> Items { get; } = new();

    public int SelectedIndex
    {
        get => Hwnd != 0 ? (int)NativeBindings.SendMessage(Hwnd, NativeBindings.LB_GETCURSEL) : -1;
        set
        {
            if (Hwnd != 0) NativeBindings.SendMessage(Hwnd, NativeBindings.LB_SETCURSEL, value);
        }
    }

    public Action<ListBox, int> OnSelectedIndexChanged;

    internal bool SuppressSelectionEvent;

    internal override unsafe void OnAttached()
    {
        if (Application.DarkModeEnabled) NativeBindings.SetWindowTheme(Hwnd, "DarkMode_Explorer", null);
        Items.Added += OnItemAdded;
        Items.Removed += OnItemRemoved;
        Items.Set += OnItemSet;
        foreach (var item in Items)
        {
            fixed (char* pItem = item.RawBuffer)
                NativeBindings.SendMessageW(Hwnd, NativeBindings.LB_ADDSTRING, 0, (IntPtr)pItem);
        }
    }

    internal override void OnDetached()
    {
        Items.Added -= OnItemAdded;
        Items.Removed -= OnItemRemoved;
        Items.Set -= OnItemSet;
    }

    private unsafe void OnItemAdded(TextBuffer item)
    {
        fixed (char* pItem = item.RawBuffer)
            NativeBindings.SendMessageW(Hwnd, NativeBindings.LB_ADDSTRING, 0, (IntPtr)pItem);
    }

    private void OnItemRemoved(int index, TextBuffer _)
    {
        if (index >= 0) NativeBindings.SendMessage(Hwnd, NativeBindings.LB_DELETESTRING, index);
    }

    // delete+insert repaints only the affected row; keep the selection without firing selection events
    private unsafe void OnItemSet(int index, TextBuffer _, TextBuffer item)
    {
        int selected = SelectedIndex;
        SuppressSelectionEvent = true;
        NativeBindings.SendMessage(Hwnd, NativeBindings.LB_DELETESTRING, index);
        fixed (char* pItem = item.RawBuffer)
            NativeBindings.SendMessageW(Hwnd, NativeBindings.LB_INSERTSTRING, index, (IntPtr)pItem);
        if (selected == index) NativeBindings.SendMessage(Hwnd, NativeBindings.LB_SETCURSEL, index);
        SuppressSelectionEvent = false;
    }
}
