using System.Drawing;

namespace Win32.SimpleGui;

public abstract class Element(int x, int y, int width, int height)
{
    public int X { get; set; } = x;
    public int Y { get; set; } = y;
    public int Width { get; set; } = width;
    public int Height { get; set; } = height;
}

public class Rectangle : Element
{
    public Color Color { get; set; }

    public Rectangle(int x, int y, int width, int height, Color color) : base(x, y, width, height)
    {
        Color = color;
    }
}

public class Button : Element
{
    public event Action<Button> OnClick;
}

public class Label : Element
{
}

public class InputField : Element
{
    public event Action<InputField, string> OnTextChanged;
}

public class Checkbox : Element
{
    public event Action<Checkbox, bool> OnCheckedChanged;
}

public class Slider : Element
{
    public event Action<Slider, float> OnValueChanged;
}

public class ProgressBar : Element
{
}

public class ListBox : Element
{
    public event Action<ListBox, int> OnSelectedIndexChanged;

    public ICollection<string> Items { get; set; } // observable again or something
}