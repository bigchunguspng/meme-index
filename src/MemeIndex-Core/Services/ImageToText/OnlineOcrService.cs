using MemeIndex_Core.Utils;
using Newtonsoft.Json.Linq;
using Point = SixLabors.ImageSharp.Point;

namespace MemeIndex_Core.Services.ImageToText;

public class OnlineOcrService : IImageToTextService
{
    private const string EMPTY_WORD = "`";

    private readonly ImageGroupingService _groupingService;

    public OnlineOcrService(ImageGroupingService imageGroupingService)
    {
        _groupingService = imageGroupingService;
        _groupingService.CollageCreated += OnCollageCreated;

        ApiKey = ConfigRepository.GetConfig().OrcApiKey ?? string.Empty;
        Client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    private string ApiKey { get; }
    private string ApiURL { get; } = "https://api.ocr.space/parse/image";

    private HttpClient Client { get; }

    private AvailabilityTimer Availability { get; } = new();

    private List<RankedWord> EmptyResponse { get; } = new() { new(EMPTY_WORD, 1) };

    /*

    Ideas for cheating over API rate limit:

    1. Send images of similar size and ratio combined in 2x2, 3x3, 4x4 grids. (done)
    2. Filter out images with potentially no text using edge detection algorithm.

    */

    public event Action<Dictionary<string, List<RankedWord>>>? ImageProcessed;

    public async Task ProcessFiles(IEnumerable<string> paths)
    {
        var fileInfos = paths.Select(Helpers.GetFileInfo).OfType<FileInfo>();

        await _groupingService.ProcessFiles(fileInfos);
    }

    private async void OnCollageCreated(CollageInfo collageInfo)
    {
        var rankedWordsByPath = await GetTextRepresentation(collageInfo);
        if (rankedWordsByPath is null) return;

        var imagesPlaced = collageInfo.Placements.Count;
        var wordsSum = rankedWordsByPath.Sum    (x => x.Value.Count);
        var wordsMin = rankedWordsByPath.Min    (x => x.Value.Count);
        var wordsMax = rankedWordsByPath.Max    (x => x.Value.Count);
        var wordsAvg = rankedWordsByPath.Average(x => x.Value.Count);
        var message = $"SPACE-OCR: images: {imagesPlaced}\twords: {wordsMin}\t{wordsMax}\t{wordsAvg}\t{wordsSum}";
        Logger.Log(ConsoleColor.Blue, message);

        ImageProcessed?.Invoke(rankedWordsByPath);
    }

    public async Task<Dictionary<string, List<RankedWord>>?> GetTextRepresentation(CollageInfo collageInfo)
    {
        try
        {
            if (Availability.Unavailable) return null;

            var words = await ProcessImageWithApi(collageInfo.Collage);

            return words is null
                ? null
                : words.Count == 0
                    ? collageInfo.ImagePaths.ToDictionary(x => x, _ => EmptyResponse)
                    : GroupWordsByImage(words, collageInfo).ToDictionary(x => x.Key, x => RankWords(x.Value));
        }
        catch (Exception e)
        {
            Logger.LogError(nameof(OnlineOcrService), e);
            return null;
        }
    }

    public async Task<List<RankedWord>?> GetTextRepresentation(string path)
    {
        try
        {
            if (Availability.Unavailable) return null;

            var file = new FileInfo(path);
            if (file.Exists == false) return null;

            var bytes = await GetFileBytes(file);
            var words = await ProcessImageWithApi(bytes);

            return words is null ? null : words.Count == 0 ? EmptyResponse : RankWords(words);
        }
        catch (Exception e)
        {
            Logger.LogError(nameof(OnlineOcrService), e);
            return null;
        }
    }

    // MAKING API REQUEST

    private MultipartFormDataContent CreateRequestForm(byte[] image)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(ApiKey), "apikey" },
            { new StringContent("eng"), "language" },
            { new StringContent("2"), "OCREngine" },
            { new StringContent("true"), "scale" },
            { new StringContent("true"), "isOverlayRequired" },
        };

        form.Add(new ByteArrayContent(image, 0, image.Length), "image", "image.jpg");

        return form;
    }

    private async Task<string> SendRequest(HttpContent form)
    {
        var response = await Client.PostAsync(ApiURL, form);

        return await response.Content.ReadAsStringAsync();
    }

    private async Task<byte[]> GetFileBytes(FileInfo file)
    {
        await using var stream = file.OpenRead();
        _groupingService.EnsureImageTakesLessThan1MB(stream);

        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);

        return memory.ToArray();
    }

    // API RESPONSE PROCESSING

    private async Task<List<Word>?> ProcessImageWithApi(byte[] bytes)
    {
        var request = CreateRequestForm(bytes);
            
        var response = await SendRequest(request);
        if (response.StartsWith('{') == false)
        {
            Logger.Status("API is unavailable x_x");
            Availability.MakeUnavailableFor(seconds: 60 * 5);
            return null;
        }

        var jo = JObject.Parse(response);

        var exitCode = (int)jo.SelectToken("OCRExitCode")!;
        if (exitCode > 2)
        {
            return null;
        }

        var hasText = (bool)jo.SelectToken("ParsedResults[0].TextOverlay.HasOverlay")!;
        if (hasText == false)
        {
            return new List<Word>();
        }

        var words = jo.SelectToken("ParsedResults[0].TextOverlay.Lines")!
            .Select(token => token.ToObject<TextLine>())
            .OfType<TextLine>()
            .SelectMany(x => x.Words)
            .ToList();

        return words;
    }

    private static Dictionary<string, List<Word>> GroupWordsByImage(IEnumerable<Word> words, CollageInfo collageInfo)
    {
        var wordsByImage = words
            .GroupBy(word =>
            {
                var x = word.Left + 0.5 * word.Width;
                var y = word.Top + 0.5 * word.Height;
                var wordMidPoint = new Point((int)x, (int)y);
                var placement =
                    collageInfo.Placements.FirstOrDefault(p => wordMidPoint.IsInside(p.Rectangle)) ??
                    collageInfo.Placements.MinBy(p => wordMidPoint.GetDistanceToRectangleCenter(p.Rectangle));
                return placement?.File.FullName;
            })
            .OfType<IGrouping<string, Word>>()
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var path in collageInfo.ImagePaths.Where(x => !wordsByImage.ContainsKey(x)))
        {
            wordsByImage.Add(path, new List<Word>());
        }

        return wordsByImage;
    }

    private List<RankedWord> RankWords(IReadOnlyCollection<Word> words)
    {
        if (words.Count == 0) return EmptyResponse;

        var maxHeight = words.Max(x => x.Height);
        var countPenalty = (int)Math.Sqrt(words.Count) - 1; // [0..]

        var rankedWords = words
            .Select(word =>
            {
                var sizePenalty = (int)Math.Round(maxHeight / word.Height); // [1..]
                var text = word.WordText.ToLower();
                return new RankedWord(text, countPenalty + sizePenalty);
            })
            .ToList();

        var uniqueRankedWords = rankedWords
            .GroupBy(x => x.Word)
            .Select(g =>
            {
                var count = g.Count();
                return count == 1
                    ? g.First()
                    : new RankedWord
                    (
                        g.Key,
                        Math.Max(g.Min(x => x.Rank) - (int)Math.Round(Math.Sqrt(2 * count)), 1)
                    );
            })
            .ToList();

        return uniqueRankedWords;
    }

    public record TextLine(Word[] Words);

    public record Word(string WordText, double Left, double Top, double Height, double Width);
}
