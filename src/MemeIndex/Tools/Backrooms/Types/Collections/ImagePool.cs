using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using LogLevel = MemeIndex.Tools.Logging.LogLevel;

namespace MemeIndex.Tools.Backrooms.Types.Collections;

/// Don't load the same image twice!
public class ImagePool
{
    private readonly Dictionary<string, ImageBooking>         _cache    = new();
    private readonly Dictionary<string, Task<Image<Rgba32>>>  _loadings = new();

    [MethodImpl(Synchronized)]
    public void Book(IEnumerable<string> paths, int ensureCapacity = 0)
    {
        if (ensureCapacity > 0)
            _cache.EnsureCapacity(ensureCapacity);

        int b = 0, a = 0;
        foreach (var path in paths)
        {
            if (_cache.TryGetValue(path, out var booking))
                booking.Bookings++;
            else
            {
                _cache.Add(path, new ImageBooking());
                a++;
            }

            b++;
        }

        var a_padded = a.ToString().PadLeft(b.ToString().Length);
        Log("ImagePool", $"BOOKING: {a_padded}/{b} (added/booked)",
            LogLevel.Debug, ConsoleColor.DarkGray);
    }

    /// Make sure path was booked!
    [MethodImpl(Synchronized)]
    public Task<Image<Rgba32>> Load(string path)
    {
        var booking = _cache[path];
        if (booking.Loading)
        {
            return _loadings[path];
        }
        else
        {
            booking.Loading = true;
            return _loadings[path] = Task.Run(async () =>
            {
                return booking.Image
                    = await Image.LoadAsync<Rgba32>(path);
            });
        }
    }

    /// Make sure path was booked!
    [MethodImpl(Synchronized)]
    public void Return(string path)
    {
        var booking = _cache[path];
        booking.Bookings--;

        if (booking.Bookings < 1)
        {
            _cache   .Remove(path);
            _loadings.Remove(path);
            booking.Image?.Dispose();
        }
    }
}

public class ImageBooking
{
    public Image<Rgba32>? Image;
    public int Bookings = 1;
    public bool Loading;
}