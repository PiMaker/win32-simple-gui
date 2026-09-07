using System.Drawing;

namespace Win32.SimpleGui;

public abstract class Element
{
    internal nint Hwnd;
    internal bool Attached;
    internal string TextValue = "";

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

    protected string GetText() => Hwnd != 0 ? NativeBindings.GetWindowText(Hwnd) : TextValue;

    protected void SetText(string value)
    {
        TextValue = value ?? "";
        if (Hwnd != 0) NativeBindings.SetWindowTextW(Hwnd, TextValue);
    }

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

public class Label : ColorableElement
{
    public Label(string text = "")
    {
        TextValue = text;
        Width = 200;
        Height = 20;
    }

    public string Text { get => GetText(); set => SetText(value); }
}

public class Button : Element
{
    public Button(string text = "")
    {
        TextValue = text;
        Height = 28;
    }

    public string Text { get => GetText(); set => SetText(value); }

    public Action<Button> OnClick;
}

public class TextBox : ColorableElement
{
    public TextBox()
    {
        Width = 200;
    }

    public string Text { get => GetText(); set => SetText(value); }

    public Action<TextBox, string> OnTextChanged;
}

public class Checkbox : Element
{
    public Checkbox(string text = "")
    {
        TextValue = text;
        Width = 120;
        Height = 20;
    }

    public string Text { get => GetText(); set => SetText(value); }

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

    public ObservableList<string> Items { get; } = new();

    public int SelectedIndex
    {
        get => Hwnd != 0 ? (int)NativeBindings.SendMessage(Hwnd, NativeBindings.LB_GETCURSEL) : -1;
        set
        {
            if (Hwnd != 0) NativeBindings.SendMessage(Hwnd, NativeBindings.LB_SETCURSEL, value);
        }
    }

    public Action<ListBox, int> OnSelectedIndexChanged;

    internal override void OnAttached()
    {
        Items.Added += OnItemAdded;
        Items.Removed += OnItemRemoved;
        foreach (var item in Items) NativeBindings.SendMessageW(Hwnd, NativeBindings.LB_ADDSTRING, 0, item);
    }

    internal override void OnDetached()
    {
        Items.Added -= OnItemAdded;
        Items.Removed -= OnItemRemoved;
    }

    private void OnItemAdded(string item) => NativeBindings.SendMessageW(Hwnd, NativeBindings.LB_ADDSTRING, 0, item);

    private void OnItemRemoved(string item)
    {
        int index = Items.IndexOf(item);
        if (index >= 0) NativeBindings.SendMessage(Hwnd, NativeBindings.LB_DELETESTRING, index);
    }
}
