using MemeIndex_Core.Utils;
using Newtonsoft.Json.Linq;

namespace MemeIndex_Core.Services;

public class OnlineOcrService : IOcrService
{
    public OnlineOcrService()
    {
        Client = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5)
        };
    }

    private string ApiKey { get; set; } = "PASTE_YOUR_KEY_HERE";
    private string ApiURL { get; set; } = "https://api.ocr.space/parse/image";

    private HttpClient Client { get; set; }

    public async Task<string> GetTextRepresentation(string path, string lang = "eng")
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

            if (!string.IsNullOrEmpty(path))
            {
                var bytes = await File.ReadAllBytesAsync(path);
                form.Add(new ByteArrayContent(bytes, 0, bytes.Length), "image", "image.jpg");
            }

            var response = await Client.PostAsync(ApiURL, form);

            var json = await response.Content.ReadAsStringAsync();

            var text = JObject.Parse(json).SelectToken("ParsedResults[0].ParsedText")!.ToString();

            return text.RemoveLineBreaks();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return string.Empty;
        }
    }
}