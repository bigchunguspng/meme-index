using IronSoftware.Drawing;
using MemeIndex_Core.Utils;
using Newtonsoft.Json.Linq;

namespace MemeIndex_Core.Services;

public class OnlineOcrService : IOcrService
{
    public OnlineOcrService(Config config)
    {
        ApiKey = config.OrcApiKey ?? string.Empty;
        Client = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5)
        };
    }

    private string ApiKey { get; set; }
    private string ApiURL { get; set; } = "https://api.ocr.space/parse/image";

    private HttpClient Client { get; set; }

    public async Task<string?> GetTextRepresentation(string path, string lang = "eng")
    {
        try
        {
            var form = new MultipartFormDataContent
            {
                { new StringContent(ApiKey), "apikey" },
                { new StringContent(lang), "language" },
                { new StringContent("2"), "OCREngine" },
                { new StringContent("true"),  "scale" },
            };

            byte[] bytes;

            var file = new FileInfo(path);
            if (file.Exists == false)
            {
                return null;
            }

            if (file.Length >= 1024 * 1024)
            {
                var divider = Math.Sqrt(file.Length / 500_000F);

                using var image = AnyBitmap.FromFile(path);

                var w = (int)(image.Width  / divider);
                var h = (int)(image.Height / divider);

                using var bitmap = new AnyBitmap(image, w, h);

                bytes = bitmap.ExportBytes(AnyBitmap.ImageFormat.Jpeg, 25);
            }
            else
            {
                bytes = await File.ReadAllBytesAsync(path);
            }

            form.Add(new ByteArrayContent(bytes, 0, bytes.Length), "image", "image.jpg");

            var response = await Client.PostAsync(ApiURL, form);

            var json = await response.Content.ReadAsStringAsync();

            var text = JObject.Parse(json).SelectToken("ParsedResults[0].ParsedText")!.ToString();

            return text.RemoveLineBreaks().ToLower();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }
}