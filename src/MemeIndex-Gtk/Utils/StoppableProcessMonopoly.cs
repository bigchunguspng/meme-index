namespace MemeIndex_Gtk.Utils;

/// Use this class to restrict execution of a specific process to these rules:
/// <ul>
/// <li>The process can have only one running instance.</li>
/// <li>Instance started later should stop any running one, if such exists.</li>
/// <li>The process execution can be stopped by an external request.</li>
/// </ul>
public class StoppableProcessMonopoly
{
    private bool _rightsAreTaken, _executionAllowed, _executionAllowedExternal = true;

    /// Call this method inside of a process, before the actual work.
    public async Task TakeRights()
    {
        _executionAllowed = false;
        while (_executionAllowedExternal == false || _rightsAreTaken) await Task.Delay(10);
        _rightsAreTaken = true;
        _executionAllowed = true;
    }

    /// Call this method inside of a process, when done (use "finally" block).
    public void ReleaseRights() => _rightsAreTaken = false;

    /// Check this inside of a process, and stop the process if you have to.
    public bool ExecutionDisallowed => !_executionAllowed || !_executionAllowedExternal;

    /// Use this method to disallow the execution, and stop the process if any is running.
    public async Task StopExternally()
    {
        _executionAllowedExternal = false;
        while (_rightsAreTaken) await Task.Delay(10);
    }

    /// Use this method to allow the execution of the process.
    public void AllowExternally()
    {
        _executionAllowedExternal = true;
    }
}