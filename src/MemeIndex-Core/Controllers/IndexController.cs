using MemeIndex_Core.Model;

namespace MemeIndex_Core.Controllers;

public class IndexController
{
    public Task<IEnumerable<MonitoringOptions>> GetMonitoringDirectories()
    {
        // ms.GetDirectories()
        // *convert*
        throw new NotImplementedException();
    }

    public Task UpdateMonitoringDirectories(IEnumerable<MonitoringOptions> options)
    {
        throw new NotImplementedException();
    }

    public Task AddDirectory(MonitoringOptions options)
    {
        // ms.AddDirectory(ops)
        // fs.AddFiles(ops.path, ops.rec)
        // fws.StartWatching(ops.path, ops.rec)
        // Task.Run(is.ProcessPendingFiles)

        throw new NotImplementedException();
    }

    public Task UpdateDirectory(MonitoringOptions options)
    {
        // var x = ms.UpdateDirectory(ops) recursive changed => true
        // if (x)
        // {
        //     fs.UpdateFiles(ops.path)
        //     fws.UpdateWatcher(ops.path, ops.rec)     or StartWatching(,)
        //     Task.Run(is.ProcessPendingFiles)

        throw new NotImplementedException();
    }

    public Task RemoveDirectory(string path)
    {
        // var dir = ms.GetDirectory(path)
        // ms.RemoveDirectory(ops)          files will be removed by cascade delete
        // fws.StopWatching(path)

        throw new NotImplementedException();
    }

    public Task UpdateFileSystemKnowledge()
    {
        // ot.Overtake(path? null)
        throw new NotImplementedException();
    }

    public Task StartIndexing()
    {
        // fws.Start()
        throw new NotImplementedException();
    }
    
    public void StopIndexing()
    {
        // fws.Stop()
        throw new NotImplementedException();
    }
}

/*
ms  monitoringService
fws fileWatchService
is  indexingService
*/