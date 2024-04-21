using MemeIndex_Core.Model;

namespace MemeIndex_Core.Controllers;

public class ManageController
{
    public IEnumerable<DirectoryMonitoringOptions> GetMonitoringDirectories()
    {
        // ms.GetDirectories()
        // *convert*
        throw new NotImplementedException();
    }

    public void UpdateMonitoringDirectories(IEnumerable<DirectoryMonitoringOptions> options)
    {
        throw new NotImplementedException();
    }

    public void AddDirectory(DirectoryMonitoringOptions options)
    {
        // ms.AddDirectory(ops)
        // fs.AddFiles(ops.path, ops.rec)
        // fws.StartWatching(ops.path, ops.rec)
        // Task.Run(is.ProcessPendingFiles)

        throw new NotImplementedException();
    }

    public void UpdateDirectory(DirectoryMonitoringOptions options)
    {
        // var x = ms.UpdateDirectory(ops) recursive changed => true
        // if (x)
        // {
        //     fs.UpdateFiles(ops.path)
        //     fws.UpdateWatcher(ops.path, ops.rec)     or StartWatching(,)
        //     Task.Run(is.ProcessPendingFiles)

        throw new NotImplementedException();
    }

    public void RemoveDirectory(string path)
    {
        // var dir = ms.GetDirectory(path)
        // ms.RemoveDirectory(ops)          files will be removed by cascade delete
        // fws.StopWatching(path)

        throw new NotImplementedException();
    }
}

/*
ms  monitoringService
fws fileWatchService
is  indexingService
*/