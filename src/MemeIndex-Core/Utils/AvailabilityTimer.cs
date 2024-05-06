namespace MemeIndex_Core.Utils;

/// <summary>
/// Can be used to make a specific recourse unavailable
/// for a specific amount of time.
/// </summary>
public class AvailabilityTimer
{
    public bool IsAvailable { get; private set; } = true;
    public bool Unavailable => !IsAvailable;

    public void MakeUnavailableFor(int seconds)
    {
        if (IsAvailable)
        {
            Count(seconds);
        }
    }

    private async void Count(int seconds)
    {
        IsAvailable = false;
        await Task.Delay(seconds * 1000);
        IsAvailable = true;
    }
}