namespace DesktopStacksService.Services;

public interface IFileMonitorService : IDisposable
{
    event EventHandler<FileSystemEventArgs>? FileCreated;
    event EventHandler<FileSystemEventArgs>? FileChanged;
    event EventHandler<FileSystemEventArgs>? FileDeleted;
    event EventHandler<RenamedEventArgs>? FileRenamed;

    Task StartMonitoringAsync(string path, CancellationToken cancellationToken = default);
    Task StopMonitoringAsync();
    bool IsMonitoring { get; }
}
