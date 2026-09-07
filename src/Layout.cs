namespace Win32.SimpleGui;

public abstract class BaseLayout : Element, ISizeProvider
{
    public const int Fill = int.MinValue;

    public ObservableList<Element> Children { get; } = new();
    public ISizeProvider Parent { get; set; }

    public abstract void Arrange();

    // box this layout arranges inside; a root layout insets itself by its own margin,
    // a nested one was positioned by its parent (own margin included)
    protected (int X, int Y, int Width, int Height) Box()
    {
        if (Parent is BaseLayout)
            return (AbsoluteX, AbsoluteY, Parent.ClientWidth, Parent.ClientHeight);
        return (Margin.Left, Margin.Top,
            Parent.ClientWidth - Margin.Left - Margin.Right,
            Parent.ClientHeight - Margin.Top - Margin.Bottom);
    }

    protected void Position(Element child, int x, int y, int width, int height)
    {
        if (child.Hwnd != 0)
            NativeBindings.MoveWindow(child.Hwnd,
                Application.ScaleDip(x), Application.ScaleDip(y),
                Application.ScaleDip(width), Application.ScaleDip(height), true);

        child.AbsoluteX = x;
        child.AbsoluteY = y;
        child.AbsoluteWidth = width;
        child.AbsoluteHeight = height;

        if (child is BaseLayout layout) layout.Arrange();
    }

    int ISizeProvider.ClientWidth => AbsoluteWidth;
    int ISizeProvider.ClientHeight => AbsoluteHeight;
}

public abstract class BaseStackLayout : BaseLayout
{
    public int Spacing { get; set; } = 4;

    protected void ArrangeStack(bool vertical)
    {
        if (Parent == null) return;
        var (boxX, boxY, boxW, boxH) = Box();

        var children = Children.ToList();
        int count = children.Count;
        if (count == 0) return;

        int marginTotal = 0;
        foreach (var child in children)
        {
            Margin margin = child.Margin;
            marginTotal += vertical ? margin.Top + margin.Bottom : margin.Left + margin.Right;
        }

        int mainAvail = (vertical ? boxH : boxW) - Spacing * (count - 1) - marginTotal;
        int fillCount = 0, fixedMain = 0;
        foreach (var child in children)
        {
            int main = vertical ? child.Height : child.Width;
            if (main == Fill) fillCount++;
            else fixedMain += main;
        }
        int fillSize = fillCount > 0 ? Math.Max(0, (mainAvail - fixedMain) / fillCount) : 0;

        int cursor = 0;
        foreach (var child in children)
        {
            Margin margin = child.Margin;
            int marginMain = vertical ? margin.Top + margin.Bottom : margin.Left + margin.Right;
            int marginCross = vertical ? margin.Left + margin.Right : margin.Top + margin.Bottom;

            int main = vertical ? child.Height : child.Width;
            int cross = vertical ? child.Width : child.Height;
            if (main == Fill) main = fillSize;
            if (cross == Fill) cross = (vertical ? boxW : boxH) - marginCross;

            int x = boxX + (vertical ? (child.Width == Fill ? margin.Left : child.X + margin.Left) : cursor + margin.Left);
            int y = boxY + (vertical ? cursor + margin.Top : (child.Height == Fill ? margin.Top : child.Y + margin.Top));

            Position(child, x, y, vertical ? cross : main, vertical ? main : cross);

            cursor += main + marginMain + Spacing;
        }
    }
}

// arranges children at their X/Y relative to the layout's origin; Fill takes the box size,
// manual sizes are left alone and may overflow
public class ManualLayout : BaseLayout
{
    public override void Arrange()
    {
        if (Parent == null) return;
        var (originX, originY, boxW, boxH) = Box();

        foreach (var child in Children)
        {
            int width = child.Width == Fill ? boxW : child.Width;
            int height = child.Height == Fill ? boxH : child.Height;
            Position(child, originX + child.X, originY + child.Y, width, height);
        }
    }
}

public class HorizontalLayout : BaseStackLayout
{
    public override void Arrange() => ArrangeStack(false);
}

public class VerticalLayout : BaseStackLayout
{
    public override void Arrange() => ArrangeStack(true);
}
