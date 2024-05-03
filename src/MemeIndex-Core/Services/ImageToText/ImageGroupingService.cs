using System.Diagnostics;
using MemeIndex_Core.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MemeIndex_Core.Services.ImageToText;

/// <summary>
/// Is used to bypass OCR API rate limit
/// by grouping images into collages before sending them to the service.
/// </summary>
public class ImageGroupingService
{
    private readonly JpegEncoder _defaultJpegEncoder;

    public ImageGroupingService()
    {
        _defaultJpegEncoder = new JpegEncoder
        {
            Quality = 60,
            ColorType = JpegEncodingColor.Luminance
        };
    }

    public event Action<CollageInfo>? CollageCreated;

    public async Task ProcessFiles(IEnumerable<FileInfo> files)
    {
        /*
        var infos = files
            .Select(GetImageInfo)
            .ToList();

        var groups = infos
            .GroupBy(x => Math.Min(1000, (x.Image.Width / 64 + 1) * 64))
            .OrderByDescending(g => g.Key)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Image.Width).ToList());

        foreach (var group in groups)
        {
            Console.WriteLine($"{group.Key}\t{group.Value.Count}");
        }
        */

        var tasks = files.Select(GetImageInfo);
        var infos = await Task.WhenAll(tasks);

        var sorted = infos.OrderByDescending(x => x.Image.Width).ToList();

        var unused = new List<FileImageInfo>();
        var skip = 0;
        while (unused.Count > 0 && skip < sorted.Count)
        {
            var maxWidth = sorted[skip].Image.Width;
            var columns = Math.Clamp((int)Math.Round(3000 / (double)maxWidth), 3, 8);
            var take = columns * columns - unused.Count;

            var images = sorted
                .Skip(skip)
                .Take(take)
                .Union(unused)
                .OrderByDescending(x => x.Image.Height)
                .ToList();

            var result = MakeCollage(images, columns);

            CollageCreated?.Invoke(result.CollageInfo);

            unused = result.UnusedImages;
            skip = take;
        }
    }

    private (CollageInfo CollageInfo, List<FileImageInfo> UnusedImages) MakeCollage(List<FileImageInfo> infos, int columns)
    {
        var placements = new List<ImagePlacement>(infos.Count);
        var unused = new List<FileImageInfo>();

        var maxW = infos.Max(x => x.Image.Width);
        var maxH = infos.Max(x => x.Image.Height);
        var sumH = infos.Sum(x => x.Image.Height);

        Console.WriteLine(infos.Sum(x => x.File.Length));

        // 3000 - max collage width (for better results)
        // 1000 - max width of image in collage
        //    3 - min column count

        var columnW = Math.Min(maxW, 1000);

        var offsetsByColumn = new int[columns];

        var collageW = columnW * columns;
        var collageH = 2 * Math.Max(maxH, sumH / columns);

        var timer = new Stopwatch();
        timer.Start();

        using var collage = new Image<Rgb24>(collageW, collageH, Color.White);

        foreach (var info in infos)
        {
            var highEnoughColumns = offsetsByColumn.Where(x => collageH - x >= info.Image.Height).ToArray();
            if (highEnoughColumns.Length == 0)
            {
                unused.Add(info);
                continue;
            }

            var minOffset = highEnoughColumns.Min();
            var column = Array.IndexOf(offsetsByColumn, minOffset);

            var offset = new Point(columnW * column, minOffset);

            var isRgba32 = info.Image.PixelType.BitsPerPixel == 32;
            var placeImage = isRgba32.Switch<PlaceImage>(PlaceImage32, PlaceImage24);
            var placement = placeImage(info.File, collage, offset);

            placements.Add(placement);
            offsetsByColumn[column] += placement.Rectangle.Height;
            Console.WriteLine(info.File.FullName);
        }

        Console.WriteLine(timer.Elapsed.TotalSeconds + "\tcollage");
        timer.Restart();

        var maxOffset = offsetsByColumn.Max();
        collage.Mutate(x => x.Crop(new Rectangle(0, 0, collageW, maxOffset)));

        Console.WriteLine(timer.Elapsed.TotalSeconds + "\tcrop");
        timer.Restart();

        using var stream = new MemoryStream();
        collage.SaveAsJpeg(stream, _defaultJpegEncoder);

        EnsureImageTakesLessThan1MB(stream);

        Console.WriteLine(timer.Elapsed.TotalSeconds + "\tcollage.jpg");

        return (new CollageInfo(stream.ToArray(), placements), unused);
    }

    public void EnsureImageTakesLessThan1MB(Stream stream)
    {
        while (stream.Length >= 1024 * 1024)
        {
            var divider = Math.Sqrt(stream.Length / 500_000F);

            Console.WriteLine($"Dividing image by {divider}");

            using var image = Image.Load(stream);

            var w = (int)(image.Width  / divider);
            var h = (int)(image.Height / divider);

            image.Mutate(x => x.Resize(w, h));

            image.SaveAsJpeg(stream, _defaultJpegEncoder);
        }
    }


    private static async Task<FileImageInfo> GetImageInfo(FileInfo file)
    {
        return new(File: file, Image: await GetImageInfo(file.FullName));
    }

    private static async Task<ImageInfo> GetImageInfo(string path)
    {
        try
        {
            return await Image.IdentifyAsync(path);
        }
        catch
        {
            Logger.LogError($"[{nameof(ImageGroupingService)}][{path}]: Can't get image identity");
            return new ImageInfo(new PixelTypeInfo(24), new Size(720, 720), null);
        }
    }


    private delegate void CopyPixelAction<T>(Image<T> image, int x, int y) where T : unmanaged, IPixel<T>; 

    private delegate ImagePlacement PlaceImage(FileInfo file, Image<Rgb24> collage, Point offset);

    private static ImagePlacement PlaceImage24(FileInfo file, Image<Rgb24> collage, Point offset)
    {
        void CopyPixel(Image<Rgb24> image, int x, int y)
        {
            collage[offset.X + x, offset.Y + y] = image[x, y];
        }

        return PlaceImageGeneric<Rgb24>(file, offset, CopyPixel);
    }

    private static ImagePlacement PlaceImage32(FileInfo file, Image<Rgb24> collage, Point offset)
    {
        void CopyPixel(Image<Rgba32> image, int x, int y)
        {
            var color = image[x, y];
            if (color.A > 16) // is opaque enough
            {
                collage[offset.X + x, offset.Y + y] = color.Rgb;
            }
        }

        return PlaceImageGeneric<Rgba32>(file, offset, CopyPixel);
    }

    private static ImagePlacement PlaceImageGeneric<T>
    (
        FileInfo file,
        Point offset,
        CopyPixelAction<T> copyPixel
    )
        where T : unmanaged, IPixel<T>
    {
        using var image = Image.Load<T>(file.FullName);

        for (var x = 0; x < image.Width; x++)
        for (var y = 0; y < image.Height; y++)
        {
            copyPixel(image, x, y);
        }

        return new ImagePlacement(file, new Rectangle(offset, image.Size));
    }
}

public record FileImageInfo(FileInfo File, ImageInfo Image);

public record ImagePlacement(FileInfo File, Rectangle Rectangle);

public record CollageInfo(byte[] Collage, List<ImagePlacement> Placements)
{
    public IEnumerable<string> ImagePaths => Placements.Select(x => x.File.FullName);
}