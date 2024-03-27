using MemeIndex_Core.Utils;
using Tesseract;

namespace MemeIndex_Core.Services;

public class TesseractOcrService : IOcrService
{
    private TesseractEngineFactory EngineFactory { get; set; } = new();

    public Task<string> GetTextRepresentation(string path, string lang)
    {
        try
        {
            var engine = EngineFactory.GetEngine(lang);

            using var image1 = Pix.LoadFromFile(path);
            using var image2 = image1.Invert();

            var text1 = GetPageText(engine, image1);
            var text2 = GetPageText(engine, image2);

            //Logger.Log(page.GetTsvText(0));

            return Task.FromResult($"{text1} {text2}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return Task.FromResult(string.Empty);
        }
    }

    private static string GetPageText(TesseractEngine engine, Pix image)
    {
        var page = engine.Process(image);

        var text = page.GetText().RemoveLineBreaks();

        Logger.Log(ConsoleColor.Yellow, "Mean confidence: {0}", page.GetMeanConfidence());
        Logger.Log(ConsoleColor.Yellow, "Text: {0}", text);

        page.Dispose();

        return text;
    }
}

public class TesseractEngineFactory
{
    private string DataPath { get; set; } = @"[deleted]";

    private Dictionary<string, TesseractEngine> Engines { get; set; } = new();

    public TesseractEngine GetEngine(string lang)
    {
        if (Engines.TryGetValue(lang, out var value)) return value;

        var engine = new TesseractEngine(DataPath, lang, EngineMode.Default);
        Engines.Add(lang, engine);
        
        Logger.Log(ConsoleColor.Magenta, "[Engine '{0}' created]", lang);
        
        return engine;
    }
}