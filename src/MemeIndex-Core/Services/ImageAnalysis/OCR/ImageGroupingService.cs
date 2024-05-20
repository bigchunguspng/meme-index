using MemeIndex_Core.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MemeIndex_Core.Services.ImageAnalysis.OCR;

/// <summary>
/// Is used to bypass OCR API rate limit
/// by grouping images into collages before sending them to the service.
/// </summary>
public class ImageGroupingService
{
    private readonly JpegEncoder _defaultJpegEncoder = new()
    {
        Quality = 60,
        ColorType = JpegEncodingColor.Luminance
    };

    public event Action<CollageInfo>? CollageCreated;

    public async Task ProcessFiles(IEnumerable<FileInfo> files)
    {
        var tasks = files.Select(GetImageInfo);
        var images = await Task.WhenAll(tasks);

        var collages = DistributeImages(images);

        foreach (var collage in collages)
        {
            var collageInfo = await RenderCollage(collage);
            CollageCreated?.Invoke(collageInfo);
        }
    }


    private static async Task<FileImageInfo> GetImageInfo(FileInfo file)
    {
        return new(file, await FileHelpers.GetImageInfo(file.FullName));
    }

    private static List<CollageRequest> DistributeImages(IEnumerable<FileImageInfo> infos)
    {
        var imagesByWidthDesc = infos
            .Select(x => x.CapImageByWidth(1000).CapImageByHeight(3000))
            .OrderByDescending(x => x.Image.Width).ToList();

        var totalArea = imagesByWidthDesc.Sum(x => x.Image.Width * x.Image.Height);

        var count = Math.Ceiling(totalArea / 9_000_000D);
        var limit = count > 1 ? 3000 : (int)Math.Ceiling(Math.Sqrt(2 * totalArea / count));
        var collageMax = new Size(limit, limit);

        // PHASE 1

        var columns = new List<ColumnRequest>();

        foreach (var image in imagesByWidthDesc)
        {
            var index = columns.FindIndex(x => collageMax.Height - x.Size.Height >= image.Image.Height);
            if (index < 0)
            {
                var list = new List<FileImageInfo> { image };
                columns.Add(new(image.Image.Size, list));
            }
            else
            {
                var column = columns[index];
                column.AddHeight(image.Image.Height);
                column.Images.Add(image);
            }
        }

        // PHASE 2

        var columnsByHeightDesc = columns.OrderByDescending(x => x.Size.Height).ToList();

        var collages = new List<CollageRequest>();

        foreach (var column in columnsByHeightDesc)
        {
            var index = collages.FindIndex(x => collageMax.Width - x.Size.Width >= column.Size.Width);
            if (index < 0)
            {
                var list = new List<ColumnRequest> { column };
                collages.Add(new(column.Size, list));
            }
            else
            {
                var collage = collages[index];
                collage.AddWidth(column.Size.Width);
                collage.Columns.Add(column);
            }
        }

        return collages;
    }

    private class ColumnRequest
    {
        public ColumnRequest(Size size, List<FileImageInfo> images)
        {
            Size = size;
            Images = images;
        }

        public Size Size { get; private set; }
        public List<FileImageInfo> Images { get; }

        public void AddHeight(int height)
        {
            Size = Size with { Height = Size.Height + height };
        }
    }

    private class CollageRequest
    {
        public CollageRequest(Size size, List<ColumnRequest> columns)
        {
            Size = size;
            Columns = columns;
        }

        public Size Size { get; private set; }
        public List<ColumnRequest> Columns { get; }

        public void AddWidth(int width)
        {
            Size = Size with { Width = Size.Width + width };
        }
    }


    private async Task<CollageInfo> RenderCollage(CollageRequest collageInfo)
    {
        var sw = Helpers.GetStartedStopwatch();

        var placements = new List<ImagePlacement>(collageInfo.Columns.Sum(x => x.Images.Count));

        using var collage = new Image<Rgb24>(collageInfo.Size.Width, collageInfo.Size.Height, new Rgb24(255, 255, 255));

        var offset = new Point(0, 0);

        foreach (var column in collageInfo.Columns)
        {
            offset.Y = 0;

            foreach (var image in column.Images)
            {
                var isRgba32 = image.Image.PixelType.BitsPerPixel == 32;
                var placeImage = isRgba32.Switch<PlaceImage>(PlaceImage32, PlaceImage24);
                var placement = placeImage(image.File, collage, offset, image.Image.Size);

                placements.Add(placement);
                offset.Y += image.Image.Height;
#if DEBUG
                Console.WriteLine(image.File.FullName);
#endif
            }

            offset.X += column.Images.Max(x => x.Image.Width);
        }

        Console.WriteLine(sw.Elapsed.TotalSeconds + "\tcollage filled");
        sw.Restart();

        var stream = new MemoryStream();
        await collage.SaveAsJpegAsync(stream, _defaultJpegEncoder);

        stream = await CapImageTo1MB(stream);

        Console.WriteLine(sw.Elapsed.TotalSeconds + "\tcollage.jpg");

#if DEBUG
        Directory.CreateDirectory("img-c");
        var path = Path.Combine("img-c", $"collage-{DateTime.UtcNow.Ticks}.jpg");
        await collage.SaveAsJpegAsync(path, _defaultJpegEncoder);
#endif

        return new CollageInfo(stream.ToArray(), placements);
    }

    public async Task<MemoryStream> CapImageTo1MB(Stream stream)
    {
        if (stream.Length >= 1024 * 1024)
        {
            var divider = Math.Sqrt(stream.Length / 500_000F);
            Console.WriteLine($"Dividing image by {divider}");

            stream.Position = 0; // <- for proper image loading;

            using var image = await Image.LoadAsync(stream);

            await stream.DisposeAsync();

            var w = (int)(image.Width  / divider);
            var h = (int)(image.Height / divider);

            image.Mutate(x => x.Resize(w, h));

            var memory = new MemoryStream();
            await image.SaveAsJpegAsync(memory, _defaultJpegEncoder);

            return await CapImageTo1MB(memory);
        }

        return stream as MemoryStream ?? await stream.ToMemoryStreamAsync();
    }


    private delegate void CopyPixelAction<T>(Image<T> image, int x, int y) where T : unmanaged, IPixel<T>; 

    private delegate ImagePlacement PlaceImage(FileInfo file, Image<Rgb24> collage, Point offset, Size size);

    private static ImagePlacement PlaceImage24(FileInfo file, Image<Rgb24> collage, Point offset, Size size)
    {
        void CopyPixel(Image<Rgb24> image, int x, int y)
        {
            collage[offset.X + x, offset.Y + y] = image[x, y];
        }

        return PlaceImageGeneric<Rgb24>(file, size, offset, CopyPixel);
    }

    private static ImagePlacement PlaceImage32(FileInfo file, Image<Rgb24> collage, Point offset, Size size)
    {
        void CopyPixel(Image<Rgba32> image, int x, int y)
        {
            var color = image[x, y];
            if (color.A > 16) // is opaque enough
            {
                collage[offset.X + x, offset.Y + y] = color.Rgb;
            }
        }

        return PlaceImageGeneric<Rgba32>(file, size, offset, CopyPixel);
    }

    private static ImagePlacement PlaceImageGeneric<T>
    (
        FileInfo file,
        Size size,
        Point offset,
        CopyPixelAction<T> copyPixel
    )
        where T : unmanaged, IPixel<T>
    {
        using var image = Image.Load<T>(file.FullName);

        if (image.Size.Width > size.Width || image.Size.Height > size.Height)
            image.Mutate(x => x.Resize(size));

        for (var x = 0; x < image.Width; x++)
        for (var y = 0; y < image.Height; y++)
        {
            copyPixel(image, x, y);
        }

        return new ImagePlacement(file, new Rectangle(offset, image.Size));
    }
}

public record FileImageInfo(FileInfo File, ImageInfo Image)
{
    public FileImageInfo CapImageByWidth(int widthLimit)
    {
        if (Image.Width <= widthLimit) return this;

        var size = new Size(widthLimit, widthLimit * Image.Height / Image.Width);
        return this with { Image = new ImageInfo(Image.PixelType, size, null) };
    }

    public FileImageInfo CapImageByHeight(int heightLimit)
    {
        if (Image.Height <= heightLimit) return this;

        var size = new Size(heightLimit * Image.Width / Image.Height, heightLimit);
        return this with { Image = new ImageInfo(Image.PixelType, size, null) };
    }
}

public record ImagePlacement(FileInfo File, Rectangle Rectangle);

public record CollageInfo(byte[] Collage, List<ImagePlacement> Placements)
{
    public IEnumerable<string> ImagePaths => Placements.Select(x => x.File.FullName);
}