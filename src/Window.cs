namespace Win32.SimpleGui;

public class Window : IDisposable, ISizeProvider
{
    public string Title { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public enum WindowState
    {
        Normal,
        Minimized,
        Maximized
    }
    
    public WindowState State { get; set; }

    public T AddElement<T>(T element)
        where T: Element
    {
        return element; // fluent API
    }

    // blocking call that runs the window's event loop until it closes
    public void RunEventLoop()
    {
    }

    // close the window and abort the event loop if it's running, thread-safe
    public void Dispose()
    {
    }
}