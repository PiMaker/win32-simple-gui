namespace Win32.SimpleGui;

public interface ISizeProvider
{
    int Width { get; }
    int Height { get; }
}

public abstract class BaseLayout
{
    // set to Width/Height with an ISizeProvider as a parent to fill the available space (equally) on Arrange
    public const int Fill = int.MinValue;

    // call manually to run a layouting pass
    public abstract void Arrange();

    public ICollection<Element> Children { get; } // observable list or something

    public ISizeProvider Parent { get; set; }
}

public class HorizontalLayout : BaseLayout, ISizeProvider
{
}

public class VerticalLayout : BaseLayout, ISizeProvider
{
}