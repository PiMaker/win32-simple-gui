using System.Collections;

namespace Win32.SimpleGui;

public interface IObservableElement
{
    public event Action<IObservableElement> Changed;
}

public class ObservableList<T> : IEnumerable<T>
    where T: IObservableElement
{
    private readonly List<T> _items = new();

    public Action<T> Added;
    public Action<int, T> Removed;
    public Action<int, T, T> Set;

    public int Count => _items.Count;

    public T this[int index]
    {
        get => _items[index];
        set
        {
            T old = _items[index];
            old.Changed -= OnElementChanged;
            _items[index] = value;
            value.Changed += OnElementChanged;
            Set?.Invoke(index, old, value);
        }
    }

    public T Add(T item)
    {
        _items.Add(item);
        item.Changed += OnElementChanged;
        Added?.Invoke(item);
        return item;
    }

    public TItem Add<TItem>(TItem item) where TItem : T
    {
        _items.Add(item);
        item.Changed += OnElementChanged;
        Added?.Invoke(item);
        return item;
    }

    public bool Remove(T item)
    {
        int index = _items.IndexOf(item);
        if (index < 0) return false;
        item.Changed -= OnElementChanged;
        _items.RemoveAt(index);
        Removed?.Invoke(index, item);
        return true;
    }

    public void Clear()
    {
        if (_items.Count == 0) return;
        for (int i = _items.Count - 1; i >= 0; i--)
        {
            var item = _items[i];
            item.Changed -= OnElementChanged;
            _items.RemoveAt(i);
            Removed?.Invoke(i, item);
        }
    }

    private void OnElementChanged(IObservableElement item)
    {
        if (item is T typed)
        {
            int index = _items.IndexOf(typed);
            if (index >= 0) Set?.Invoke(index, typed, typed);
        }
        else
        {
            throw new InvalidOperationException($"Item of type {item.GetType()} called Changed callback for type {typeof(T)}.");
        }
    }

    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public interface ISizeProvider
{
    int ClientWidth { get; }
    int ClientHeight { get; }
}

public enum Alignment
{
    Left,
    Center,
    Right,
}

public struct Margin(int left = 0, int top = 0, int right = 0, int bottom = 0)
{
    public int Left = left;
    public int Top = top;
    public int Right = right;
    public int Bottom = bottom;

    public Margin(int horizontal = 0, int vertical = 0) : this(horizontal, vertical, horizontal, vertical) {}
    public Margin(int all) : this(all, all, all, all) {}
}

public class Font : IDisposable
{
    public string Family { get; }
    public float Size { get; }

    private nint _handle;

    public Font() : this("Segoe UI", 9f) { }

    public Font(string family, float size)
    {
        Family = family;
        Size = size;
    }

    internal nint Handle => _handle != 0 ? _handle : _handle = NativeBindings.CreateFontW(
        -(int)Math.Round(Size * 96f / 72f * Application.Scale),
        0, 0, 0, 400, 0, 0, 0,
        NativeBindings.DEFAULT_CHARSET, 0, 0, NativeBindings.CLEARTYPE_QUALITY, 0, Family);

    public void Dispose()
    {
        if (_handle != 0) NativeBindings.DeleteObject(_handle);
        _handle = 0;
    }
}
