using MemeIndex_Core.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

namespace MemeIndex_Core.Services.ImageToText.ColorTag;

public class ColorTagService : IImageToTextService
{
    private readonly ColorSearchProfile _colorSearchProfile;

#if DEBUG
    private readonly JpegEncoder _defaultJpegEncoder = new() { Quality = 50 };
#endif

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

            Logger.Log(ConsoleColor.Blue, $"COLOR-TAG: words: {rankedWords.Count,4}");

            var result = new Dictionary<string, List<RankedWord>> { { path, rankedWords } };
            ImageProcessed?.Invoke(result);
        });

        await Task.WhenAll(tasks);
    }

    // todo:
    // - replace dot grid with line grid algorithm

    public async Task<List<RankedWord>?> GetTextRepresentation(string path)
    {
        try
        {
            // CHECK FILE
            var file = new FileInfo(path);
            if (file.Exists == false)
            {
                return null;
            }

            // GET DATA / MEASUREMENTS
            /*var info = await ImageHelpers.GetImageInfo(file.FullName);
            var isRgba = info.PixelType.BitsPerPixel == 32;*/

            //using var image = isRgba ? Image.Load<Rgba32>(file.FullName) : Image.Load<Rgb24>(file.FullName);
            using var image = await Image.LoadAsync<Rgba32>(file.FullName);

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

            var dots = new List<KeyValuePair<string, Rgba32>>();

            // SCAN IMAGE
            for (var x = (w - stepX * (chunksX - 1)) / 2; x < w - margin; x += stepX)
            for (var y = (h - stepY * (chunksY - 1)) / 2; y < h - margin; y += stepY)
            {
                var colors = new Rgba32[4];
                var index = 0;
                for (var i = -margin; i <= margin; i += 2 * margin)
                for (var j = -margin; j <= margin; j += 2 * margin)
                {
                    colors[index++] = image[x + i, y + j];
#if DEBUG
                    image[x + i, y + j] = Color.Red;
#endif
                }

                var color = FindClosestKnownColor(ColorHelpers.GetAverageColor(colors));
                dots.Add(color);
            }

#if DEBUG
            var ticks = DateTime.UtcNow.Ticks;
            var name = $"baka-{Math.Abs(path.GetHashCode())}-{ticks >> 32}";
            Directory.CreateDirectory("img");
            await image.SaveAsJpegAsync(Path.Combine("img", $"{name}-dots.jpg"), _defaultJpegEncoder);

            for (var x = 0; x < w; x++)
            for (var y = 0; y < h; y++)
            {
                var dot = x / stepX * chunksY + y / stepY;
                image[x, y] = dots[Math.Clamp(dot, 0, dots.Count - 1)].Value;
            }

            await image.SaveAsJpegAsync(Path.Combine("img", $"{name}-colors.jpg"), _defaultJpegEncoder);
#endif

            var keyCounts = dots
                .Select(x => x.Key)
                .GroupBy(x => x)
                .Select(x => (Text: x.Key, Count: x.Count()))
                .ToList();
            var colorsOnImage = (double)keyCounts.Sum(x => x.Count);
            var rankedWords = keyCounts
                .OrderByDescending(x => x.Count)
                .Select((x, i) => new RankedWord(x.Text, (int)(i * Math.Pow(1 - x.Count / colorsOnImage, 2))))
                .ToList();

            return rankedWords;
        }
        catch (Exception e)
        {
            Logger.LogError(nameof(ColorTagService), e);
            return null;
        }
    }

    private KeyValuePair<string, Rgba32> FindClosestKnownColor(Rgba32 color)
    {
        var chunk = color.IsGrayscale()
            ? _colorSearchProfile.ColorsGrayscale
            : _colorSearchProfile.ColorsFunny[_colorSearchProfile.Hues[color.Rgb.GetHue() / 15]];
        return chunk.MinBy(x => ColorHelpers.GetDifference(x.Value, color));
    }
}