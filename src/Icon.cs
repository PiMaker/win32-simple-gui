namespace Win32.SimpleGui;

// reads ICONDIR/ICONDIRENTRY (.ico) data directly
public class Icon(byte[] data) : IDisposable
{
    private readonly byte[] _data = data;
    private nint _small;
    private nint _big;

    internal nint SmallHandle => GetHandle(ref _small, 16);
    internal nint BigHandle => GetHandle(ref _big, 32);

    public void Dispose()
    {
        if (_small != 0) NativeBindings.DestroyIcon(_small);
        if (_big != 0) NativeBindings.DestroyIcon(_big);
        _small = 0;
        _big = 0;
    }

    private nint GetHandle(ref nint handle, int size)
    {
        if (handle != 0) return handle;
        int count = BitConverter.ToUInt16(_data, 4);
        int best = -1, bestDiff = int.MaxValue;
        for (int i = 0; i < count; i++)
        {
            int entry = 6 + i * 16;
            int width = _data[entry] == 0 ? 256 : _data[entry];
            int diff = Math.Abs(width - size);
            if (diff < bestDiff)
            {
                bestDiff = diff;
                best = entry;
            }
        }
        if (best < 0) return 0;
        return handle = NativeBindings.CreateIconFromResource(_data, BitConverter.ToInt32(_data, best + 12), BitConverter.ToInt32(_data, best + 8), size);
    }
}
