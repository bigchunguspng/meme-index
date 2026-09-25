using System.Net;
using System.Net.Sockets;

namespace MemeIndex.API;

public static class HostingHelpers
{
    public  const int DYNAMIC_PORT = 0;

    public  static readonly IPAddress IP = IPAddress.Any;
    private static readonly int[] _ports =
    [
        7373, 5928, // IANA safe, 4-digits
        31313, 13131, 13301, 33301, 33401,
        // https://www.iana.org/assignments/service-names-port-numbers
    ];

    public static async Task<int> GetFreePort()
    {
        Dir_AppData.EnsureDirectoryExist();

        var this_user = Environment.UserName;

        // OPEN FILE
        await using var fs = new FileStream
            (File_Ports, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

        // READ FILE, FIND USER=PORT MAPPING
        var mappings = await ReadFile_Ports(fs);
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
                // ADD & SAVE MAPPING
                mappings.Add(this_user, free_port);
                await SaveFile_Ports(fs, mappings);
                return free_port;
            }
        }

        // NO FREE PORTS (unlikely)
        return DYNAMIC_PORT;
    }

    private static async Task<Dictionary<string, int>> 
        ReadFile_Ports
        (FileStream fs)
    {
        var mappings = new Dictionary<string, int>();
        using var reader = new StreamReader(fs, leaveOpen: true);
        while (await reader.ReadLineAsync() is { } line)
        {
            var bits = line.Split('=', 2);
            if (bits.Length < 2)
                throw new UnexpectedException($"FILE CONTENT IS CORRUPTED: {File_Ports}");

            var user = bits[0];
            var port = bits[1];
            if (int.TryParse(port, out var port_int))
                mappings.Add(user, port_int);
        }

        return mappings;
    }

    private static async Task 
        SaveFile_Ports
        (FileStream fs, Dictionary<string, int> mappings)
    {
        // REWIND
        fs.SetLength (0);
        fs.Position = 0;

        // SAVE CHANGES
        await using var writer = new StreamWriter(fs);
        foreach (var l in mappings)
        {
            await writer.WriteLineAsync($"{l.Key}={l.Value}");
        }
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