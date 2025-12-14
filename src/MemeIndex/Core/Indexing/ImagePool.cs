using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MemeIndex.Core.Indexing;

/// Don't load the same image twice!
public class ImagePool
{
    private readonly Dictionary<string, ImageBooking> _cache    = new();
    private readonly Dictionary<string, Task<Image<Rgba32>>>  _loadings = new();

    [MethodImpl(Synchronized)]
    public void Book(IEnumerable<string> paths)
    {
        int i = 0, a = 0;
        foreach (var path in paths)
        {
            if (_cache.TryGetValue(path, out var booking))
                booking.Bookings++;
            else
            {
                _cache.Add(path, new ImageBooking());
                a++;
            }

            i++;
        }
        Log("ImagePool", $"BOOKING: {a,5}/{i,5} (added/booked)");
    }

    /// Make sure path was booked!
    [MethodImpl(Synchronized)]
    public Task<Image<Rgba32>> Load(string path)
    {
        var booking = _cache[path];
        if (booking.Loading)
        {
            Log("ImagePool", "Load -> existing");
            return _loadings[path];
        }
        else
        {
            booking.Loading = true;
            var task = Task.Run(async () => await LoadImage(booking, path));
            _loadings.Add(path, task);
            Log("ImagePool", "Load -> new");
            return task;
        }
    }

    private async Task<Image<Rgba32>> LoadImage(ImageBooking booking, string path)
    {
        return booking.Image = await Image.LoadAsync<Rgba32>(path);
    }

    /// Make sure path was booked!
    [MethodImpl(Synchronized)]
    public void Return(string path)
    {
        var booking = _cache[path];
        booking.Bookings--;

        if (booking.Bookings < 1)
        {
            booking.Image?.Dispose();
            _cache.Remove(path);
        }
    }
}

public class ImageBooking
{
    public Image<Rgba32>? Image;
    public int Bookings;
    public bool Loading;
}