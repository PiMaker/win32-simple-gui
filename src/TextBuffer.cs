namespace Win32.SimpleGui;

public class TextBuffer
{
    private readonly char[] _buffer;
    private int _count;

    public TextBuffer(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        _buffer = new char[capacity + 1]; // + \0 byte
    }

    public TextBuffer(int capacity, string initial) : this(capacity) => Append(initial);

    public int Count => _count;
    public int Capacity => _buffer.Length - 1;

    public char this[int index]
    {
        get
        {
            if (index < 0 || index >= _count) throw new ArgumentOutOfRangeException(nameof(index));
            return _buffer[index];
        }
        set
        {
            if (index < 0 || index >= _count) throw new ArgumentOutOfRangeException(nameof(index));
            _buffer[index] = value;
        }
    }

    public void Append(string value)
    {
        if (value == null) return;
        Insert(_count, value);
    }

    public void Insert(int index, string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (index < 0 || index > _count) throw new ArgumentOutOfRangeException(nameof(index));
        if (_count + value.Length > Capacity) throw new ArgumentOutOfRangeException(nameof(value), "New length exceeds TextBuffer capacity.");
        Array.Copy(_buffer, index, _buffer, index + value.Length, _count - index);
        value.CopyTo(0, _buffer, index, value.Length);
        _count += value.Length;
        _buffer[_count] = '\0';
    }

    public void Clear()
    {
        _count = 0;
        _buffer[0] = '\0';
    }

    internal char[] RawBuffer => _buffer;

    internal void RawSetCount(int count)
    {
        if (count < 0 || count > Capacity) throw new ArgumentOutOfRangeException(nameof(count));
        _count = count;
        _buffer[_count] = '\0';
    }

    public override string ToString() => new(_buffer, 0, _count);
}
