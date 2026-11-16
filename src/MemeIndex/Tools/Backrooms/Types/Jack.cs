namespace MemeIndex.Tools.Backrooms.Types;

/// Job synchronizer.
public class Jack
    (Func<Task> execute, Action<Exception>? exceptionHandler = null)
{
    private readonly Lock _lock = new();

    private bool _busy;
    private bool _recheck;

    /// Trigger, while idle, starts work. <br/>
    /// Trigger, while busy, makes it start work once again when done.
    public void Trigger()
    {
        var start = false;

        lock (_lock)
        {
            if (_busy == false)
                _busy = start = true;
            else
                _recheck = true;
        }

        if (start)
            _ = Execute();
    }

    private async Task Execute()
    {
        while (true)
        {
            try
            {
                await execute();
            }
            catch (Exception e)
            {
                exceptionHandler?.Invoke(e);
            }

            lock (_lock)
            {
                if (_recheck)
                    _recheck = false;
                else
                {
                    _busy = false;
                    break;
                }
            }
        }
    }
}