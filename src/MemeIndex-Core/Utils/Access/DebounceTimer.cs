namespace MemeIndex_Core.Utils.Access;

public class DebounceTimer
{
    private DateTime _lastTime;

    public async Task<bool> Wait(int milliseconds)
    {
        var thisTime = DateTime.UtcNow;
        _lastTime = thisTime;
        await Task.Delay(milliseconds);
        return _lastTime == thisTime;
    }
}