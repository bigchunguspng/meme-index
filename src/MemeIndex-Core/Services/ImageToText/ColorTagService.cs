using IronSoftware.Drawing;
using MemeIndex_Core.Utils;

namespace MemeIndex_Core.Services.ImageToText;

public class ColorTagService : IImageToTextService
{
    private readonly ColorSearchProfile _colorSearchProfile;

    public ColorTagService(ColorSearchProfile colorSearchProfile)
    {
        _colorSearchProfile = colorSearchProfile;
    }

    public event Action<Dictionary<string, List<RankedWord>>>? ImageProcessed;

    public async Task ProcessFiles(IEnumerable<string> paths)
    {
        var tasks = paths.Select(async path =>
        {
            var rankedWords = await GetTextRepresentation(path);
            if (rankedWords is null) return;

            Logger.Log(ConsoleColor.Blue, $"COLOR-TAG: words: {rankedWords.Count}");

            ImageProcessed?.Invoke(new Dictionary<string, List<RankedWord>> { { path, rankedWords }});
        });

        await Task.WhenAll(tasks);
    }

    public Task<List<RankedWord>?> GetTextRepresentation(string path)
    {
        return Task.Run(() => GetImageColorInfo(path));
    }

    private List<RankedWord>? GetImageColorInfo(string path)
    {
        // CHECK FILE
        var file = new FileInfo(path);
        if (file.Exists == false)
        {
            return null;
        }

        // GET DATA / MEASUREMENTS
        using var image = AnyBitmap.FromFile(path);

        var w = image.Width;
        var h = image.Height;

        var margin = Math.Min(w, h) < 80 ? 1 : 2;

        var w2 = w - 2 * margin;
        var h2 = h - 2 * margin;

        var stepX = GetStep(w2);
        var stepY = GetStep(h2);

        int GetStep(int side) =>
            side < 80
                ? Math.Max(side >> 3, 4)
                : side < 720
                    ? side >> 4
                    : side >> 5;

        var chunksX = w2 / stepX;
        var chunksY = h2 / stepY;

        var dots = new List<KeyValuePair<string, Color>>();

        var pixels = image.GetRGBBuffer();
        var alphas = image.GetAlphaBuffer();

        // SCAN IMAGE
        for (var x = (w - stepX * (chunksX - 1)) / 2; x < w - margin; x += stepX)
        for (var y = (h - stepY * (chunksY - 1)) / 2; y < h - margin; y += stepY)
        {
            var colors = new Color[4];
            var index = 0;
            for (var i = -margin; i <= margin; i += 2 * margin)
            for (var j = -margin; j <= margin; j += 2 * margin)
            {
                colors[index++] = image.GetPixelColor(x + i, y + j, pixels, alphas);
#if DEBUG
                image.SetPixel(x + i, y + j, Color.Red);
#endif
            }

            var color = FindClosestKnownColor(ColorHelpers.GetAverageColor(colors));
            dots.Add(color);
        }

#if DEBUG
        var ticks = DateTime.UtcNow.Ticks;
        Directory.CreateDirectory("img");
        image.SaveAs(Path.Combine("img", $"baka-{ticks}-dots.jpg"), AnyBitmap.ImageFormat.Jpeg, 50);

        for (var x = 0; x < w; x++)
        for (var y = 0; y < h; y++)
        {
            var dot = x / stepX * chunksY + y / stepY;
            image.SetPixel(x, y, dots[Math.Clamp(dot, 0, dots.Count - 1)].Value);
        }

        image.SaveAs(Path.Combine("img", $"baka-{ticks}-colors.jpg"), AnyBitmap.ImageFormat.Jpeg, 50);
#endif

        var data = dots
            .Select(x => x.Key)
            .GroupBy(x => x)
            .OrderByDescending(g => g.Count())
            .Select((x, i) => new RankedWord(x.Key, i))
            .ToList();

        return data;
    }

    private KeyValuePair<string, Color> FindClosestKnownColor(Color color)
    {
        var chunk = color.IsGrayscale()
            ? _colorSearchProfile.ColorsGrayscale
            : _colorSearchProfile.ColorsFunny[_colorSearchProfile.Hues[color.GetHue() / 15]];
        return chunk.MinBy(x => ColorHelpers.GetDifference(x.Value, color));
    }
}