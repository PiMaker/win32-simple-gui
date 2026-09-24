using System.Numerics;
using System.Runtime.CompilerServices;

namespace Win32.SimpleGui;

public class TextBuffer : IObservableElement
{
    public event Action<IObservableElement> Changed;

    private readonly char[] _buffer;
    private int _count;

    public TextBuffer(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        _buffer = new char[capacity + 1]; // + \0 byte
    }

    public TextBuffer([InterpolatedStringHandlerArgument] StaticInterpolatedStringHandler handler)
    {
        _count = handler.ActiveBuffer.Count;
        _buffer = handler.ActiveBuffer.RawBuffer.Clone() as char[];
    }

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

            Changed?.Invoke(this);
        }
    }

    public TextBuffer Append([InterpolatedStringHandlerArgument("")] AppendInterpolatedStringHandler _)
    {
        Changed?.Invoke(this);
        return this;
    }

    public TextBuffer Set([InterpolatedStringHandlerArgument("")] SetInterpolatedStringHandler _)
    {
        Changed?.Invoke(this);
        return this;
    }

    public TextBuffer Insert(int index, string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (index < 0 || index > _count) throw new ArgumentOutOfRangeException(nameof(index));
        if (_count + value.Length > Capacity) throw new ArgumentOutOfRangeException(nameof(value), "New length exceeds TextBuffer capacity.");
        Array.Copy(_buffer, index, _buffer, index + value.Length, _count - index);
        value.CopyTo(0, _buffer, index, value.Length);
        _count += value.Length;
        _buffer[_count] = '\0';
        Changed?.Invoke(this);
        return this;
    }

    public void Clear()
    {
        _count = 0;
        _buffer[0] = '\0';
        Changed?.Invoke(this);
    }

    // internal API
    internal char[] RawBuffer => _buffer;
    internal void RawSetCount(int count)
    {
        if (count < 0 || count > Capacity) throw new ArgumentOutOfRangeException(nameof(count));
        _count = count;
        _buffer[_count] = '\0';
    }

    public override string ToString() => new(_buffer, 0, _count);

    [InterpolatedStringHandler]
    public readonly struct AppendInterpolatedStringHandler
    {
        private readonly TextBuffer _buffer;

        public AppendInterpolatedStringHandler(int literalLength, int formattedCount, TextBuffer buffer)
        {
            _buffer = buffer;
        }

        public void AppendLiteral(string value) => _buffer.Insert(_buffer._count, value);
        public void AppendFormatted(string value) => _buffer.Insert(_buffer._count, value);

        public void AppendFormatted(TextBuffer value)
        {
            if (value._count > _buffer.Capacity - _buffer._count) throw new ArgumentOutOfRangeException(nameof(value), "New length exceeds TextBuffer capacity.");
            Array.Copy(value._buffer, 0, _buffer._buffer, _buffer._count, value._count);
            _buffer._count += value._count;
            _buffer._buffer[_buffer._count] = '\0';
        }

        public void AppendFormatted<T>(T value) where T : ISpanFormattable
        {
            if (!value.TryFormat(_buffer._buffer.AsSpan(_buffer._count, _buffer.Capacity - _buffer._count), out int written, default, null))
                throw new ArgumentOutOfRangeException(nameof(value), "New length exceeds TextBuffer capacity.");
            _buffer._count += written;
            _buffer._buffer[_buffer._count] = '\0';
        }

        public void AppendFormatted<T>(T value, ReadOnlySpan<char> format) where T : ISpanFormattable
        {
            if (!value.TryFormat(_buffer._buffer.AsSpan(_buffer._count, _buffer.Capacity - _buffer._count), out int written, format, null))
                throw new ArgumentOutOfRangeException(nameof(value), "New length exceeds TextBuffer capacity.");
            _buffer._count += written;
            _buffer._buffer[_buffer._count] = '\0';
        }
    }

    [InterpolatedStringHandler]
    public readonly struct SetInterpolatedStringHandler
    {
        private readonly AppendInterpolatedStringHandler _append;

        public SetInterpolatedStringHandler(int literalLength, int formattedCount, TextBuffer buffer)
        {
            buffer.Clear();
            _append = new AppendInterpolatedStringHandler(literalLength, formattedCount, buffer);
        }

        public void AppendLiteral(string value) => _append.AppendLiteral(value);
        public void AppendFormatted(string value) => _append.AppendFormatted(value);
        public void AppendFormatted(TextBuffer value) => _append.AppendFormatted(value);
        public void AppendFormatted<T>(T value) where T : ISpanFormattable => _append.AppendFormatted(value);
        public void AppendFormatted<T>(T value, ReadOnlySpan<char> format) where T : ISpanFormattable => _append.AppendFormatted(value, format);
    }

    [InterpolatedStringHandler]
    public readonly struct StaticInterpolatedStringHandler
    {
        private static readonly ThreadLocal<TextBuffer> _threadBuffer = new();
        private readonly TextBuffer _buffer;
        internal TextBuffer ActiveBuffer => _buffer;

        public StaticInterpolatedStringHandler(int literalLength, int formattedCount)
        {
            if (!_threadBuffer.IsValueCreated)
                _threadBuffer.Value = new TextBuffer((int)BitOperations.RoundUpToPowerOf2((uint)literalLength) * 2);

            _buffer = _threadBuffer.Value;
        }

        public void AppendLiteral(string value) => _buffer.Set($"{value}");
        public void AppendFormatted(string value) => _buffer.Set($"{value}");
        public void AppendFormatted(TextBuffer value) => _buffer.Set($"{value}");
        public void AppendFormatted<T>(T value) where T : ISpanFormattable => _buffer.Set($"{value}");

        public void AppendFormatted<T>(T value, ReadOnlySpan<char> format) where T : ISpanFormattable
        {
            // can't forward `format` parameter, so need to open code it :(
            if (!value.TryFormat(_buffer._buffer.AsSpan(_buffer._count, _buffer.Capacity - _buffer._count), out int written, format, null))
                throw new ArgumentOutOfRangeException(nameof(value), "New length exceeds TextBuffer capacity.");
            _buffer._count += written;
            _buffer._buffer[_buffer._count] = '\0';
        }
    }
}