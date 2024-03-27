using System.Diagnostics;
using System.Globalization;
using MemeIndex_Core.Services;
using MemeIndex_Core.Utils;

namespace MemeIndex_Console;

internal static class Program
{
    public static void Main(string[] args)
    {
        Logger.Log("[Start]", ConsoleColor.Magenta);

        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

        try
        {
            // init
            var onlineApi = new OnlineOcrService();
            var tesseract = new TesseractOcrService();

            while (true)
            {
                var input = Console.ReadLine()?.Trim().Trim('"');
                if (input?.Length == 1) break;

                if (!File.Exists(input))
                {
                    Logger.Log("File don't exist");
                    continue;
                }

                var timer = new Stopwatch();

                // online ocr eng eng-2
                timer.Start();
                var text = onlineApi.GetTextRepresentation(input).Result;
                Logger.Log(ConsoleColor.Blue, "Text: {0}", text);
                Logger.Log(ConsoleColor.Cyan, "Time: {0:F3}", timer.ElapsedMilliseconds / 1000F);

                // tesseract ocr eng
                //timer.Restart();
                //tesseract.GetTextRepresentation(input, "eng");
                //Logger.Log(ConsoleColor.Cyan, "Time: {0:F3}", timer.ElapsedMilliseconds / 1000F);

                // tesseract ocr rus
                //timer.Restart();
                //tesseract.GetTextRepresentation(input, "rus");
                //Logger.Log(ConsoleColor.Cyan, "Time: {0:F3}", timer.ElapsedMilliseconds / 1000F);

                // tesseract ocr ukr
                //timer.Restart();
                //tesseract.GetTextRepresentation(input, "ukr");
                //Logger.Log(ConsoleColor.Cyan, "Time: {0:F3}", timer.ElapsedMilliseconds / 1000F);
            }
        }
        catch (Exception e)
        {
            Trace.TraceError(e.ToString());
            Logger.Log("Unexpected Error: " + e.Message);
            Logger.Log("Details: ");
            Logger.Log(e.ToString());
        }

        Logger.Log("[Fin]", ConsoleColor.Magenta);
    }
}