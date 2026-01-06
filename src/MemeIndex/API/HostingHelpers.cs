using System.Net;
using System.Net.Sockets;

namespace MemeIndex.API;

public static class HostingHelpers
{
    public  const int DYNAMIC_PORT = 0;

    public  static readonly IPAddress IP = IPAddress.Any;
    private static readonly int[] _ports =
    [
        7373, 3737,
        3131, 1313,
        5928, 1488,
        3003, 3313,
        2021, 2025,
    ];

    public static async Task<int> GetFreePort()
    {
        Dir_AppData.EnsureDirectoryExist();

        // OPEN FILE
        await using var fs = new FileStream
            (File_Ports, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

        // READ
        var mappings = new Dictionary<string, int>();
        using var reader = new StreamReader(fs, leaveOpen: true);
        while (await reader.ReadLineAsync() is { } line)
        {
            var eqi = line.IndexOf('=');

            var user = line.Remove(eqi).Trim();
            var port = line.Substring(eqi + 1);
            if (int.TryParse(port, out var port_int))
                mappings.Add(user, port_int);
        }

        var this_user = Environment.UserName;
        if (mappings.TryGetValue(this_user, out var mapped_port))
        {
            // USER=PORT MAPPED
            return PortIsFree(mapped_port)
                ? mapped_port   // typical for first process instance
                : DYNAMIC_PORT; // typical for other instances
        }

        // NO USER=PORT MAPPING
        foreach (var free_port in _ports.Except(mappings.Values))
        {
            if (PortIsFree(free_port))
            {
                mappings.Add(this_user, free_port);

                // REWIND
                fs.SetLength (0);
                fs.Position = 0;

                // SAVE CHANGES
                await using var writer = new StreamWriter(fs);
                foreach (var l in mappings)
                {
                    await writer.WriteLineAsync($"{l.Key}={l.Value}");
                }

                return free_port;
            }
        }

        // NO FREE PORTS (unlikely)
        return DYNAMIC_PORT;
    }

    private static bool PortIsFree(int port)
    {
        var result = false;
        var swx = Stopwatch.StartNew();
        try
        {
            using var listener = new TcpListener(IP, port);
            listener.Start();
            listener.Stop();
            return result = true;
        }
        catch
        {
            return result = false;
        }
        finally
        {
            var color = result
                ? ConsoleColor.Green
                : ConsoleColor.Red;
            swx.Log($"Check port - TCP {port}", color);
        }
    }
}