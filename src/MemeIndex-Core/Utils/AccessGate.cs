namespace MemeIndex_Core.Utils;

/// <summary>
/// Can be used to limit access to a specific recourse (e.g. database context)
/// for only a single user at a time.
/// </summary>
public class AccessGate
{
    private object? _accessKey;

    public async Task Take()
    {
        var key = new object();

        var accessGranted = false;

        while (accessGranted == false)
        {
            while (_accessKey != null) // access is taken
            {
                await Task.Delay(25); // wait
            }

            _accessKey = key; // insert our key

            await Task.Delay(10);

            accessGranted = _accessKey == key; // make sure no one has overriden our key
        }

        var hash = _accessKey?.GetHashCode();
        Logger.Log(ConsoleColor.Yellow, $"[AccessGate / {GetHashCode()}]: [{hash}] KEY INSERTED");
    }

    public void Release()
    {
        var hash = _accessKey?.GetHashCode();
        _accessKey = null;
        Logger.Log(ConsoleColor.Yellow, $"[AccessGate / {GetHashCode()}]: [{hash}] KEY RELEASED");
    }
}