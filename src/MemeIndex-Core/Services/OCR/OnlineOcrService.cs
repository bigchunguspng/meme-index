using IronSoftware.Drawing;
using MemeIndex_Core.Utils;
using Newtonsoft.Json.Linq;

namespace MemeIndex_Core.Services.OCR;

public class OnlineOcrService : IOcrService
{
    public OnlineOcrService()
    {
        ApiKey = ConfigRepository.GetConfig().OrcApiKey ?? string.Empty;
        Client = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5)
        };
    }

    private string ApiKey { get; set; }
    private string ApiURL { get; set; } = "https://api.ocr.space/parse/image";

    private HttpClient Client { get; set; }

    public async Task<IList<RankedWord>?> GetTextRepresentation(string path)
    {
        try
        {
            // CHECK FILE
            var file = new FileInfo(path);
            if (file.Exists == false)
            {
                return null;
            }

            // SEND REQUEST
            var form = new MultipartFormDataContent
            {
                { new StringContent(ApiKey), "apikey" },
                { new StringContent("eng"), "language" },
                { new StringContent("2"), "OCREngine" },
                { new StringContent("true"), "scale" },
                { new StringContent("true"), "isOverlayRequired" },
            };

            var bytes = await GetFileBytes(file);

            form.Add(new ByteArrayContent(bytes, 0, bytes.Length), "image", "image.jpg");

            var response = await Client.PostAsync(ApiURL, form);

            // PROCESS RESPONSE
            var json = await response.Content.ReadAsStringAsync();
            var obj = JObject.Parse(json);

            var exitCode = (int)obj.SelectToken("OCRExitCode")!;
            if (exitCode > 2)
            {
                return null;
            }

            var hasText = (bool)obj.SelectToken("ParsedResults[0].TextOverlay.HasOverlay")!;
            if (hasText)
            {
                List<TextLine> lines = obj.SelectToken("ParsedResults[0].TextOverlay.Lines")!
                    .Select(token => token.ToObject<TextLine>())
                    .OrderByDescending(line => line?.MaxHeight)
                    .ToList()!;

                if (lines.Count == 0) return null;

                var maxHeight = lines[0].MaxHeight;
                var countPenalty = (int)Math.Sqrt(lines.Count - 1);

                var ranked = new List<RankedWord>();
                foreach (var line in lines)
                {
                    var sizePenalty = (int)Math.Round(maxHeight / line.MaxHeight);

                    var words = line.LineText.RemoveLineBreaks().ToLower().Split();
                    var range = words.Select(word => new RankedWord(word, countPenalty + sizePenalty));
                    ranked.AddRange(range);
                }

                var result = ranked
                    .GroupBy(x => x.Word)
                    .Select(g =>
                    {
                        var count = g.Count();
                        return count == 1
                            ? g.First()
                            : new RankedWord
                            (
                                g.Key,
                                Math.Max(g.Min(x => x.Rank) - (int)Math.Round(Math.Sqrt(count)), 1)
                            );
                    })
                    .ToList();

                return result;
            }

            return null;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }

    private static async Task<byte[]> GetFileBytes(FileInfo file)
    {
        if (file.Length < 1024 * 1024)
        {
            return await File.ReadAllBytesAsync(file.FullName);
        }

        var divider = Math.Sqrt(file.Length / 500_000F);

        using var image = AnyBitmap.FromFile(file.FullName);

        var w = (int)(image.Width  / divider);
        var h = (int)(image.Height / divider);

        using var bitmap = new AnyBitmap(image, w, h);

        return bitmap.ExportBytes(AnyBitmap.ImageFormat.Jpeg, 25);
    }

    public record TextLine(string LineText, double MaxHeight);
}