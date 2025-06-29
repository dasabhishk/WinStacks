using DesktopStacksService.Services;

namespace DesktopStacksService.Services;

public class FileMonitorService : IFileMonitorService
{
    private readonly ILogger<FileMonitorService> _logger;
    private FileSystemWatcher? _watcher;
    private bool _disposed;

    public event EventHandler<FileSystemEventArgs>? FileCreated;
    public event EventHandler<FileSystemEventArgs>? FileChanged;
    public event EventHandler<FileSystemEventArgs>? FileDeleted;
    public event EventHandler<RenamedEventArgs>? FileRenamed;

    public bool IsMonitoring => _watcher?.EnableRaisingEvents == true;

    public FileMonitorService(ILogger<FileMonitorService> logger)
    {
        _logger = logger;
    }

    public Task StartMonitoringAsync(string path, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(FileMonitorService));

        if (IsMonitoring)
        {
            _logger.LogWarning("File monitoring is already active");
            return Task.CompletedTask;
        }

        try
        {
            // Validate path exists and is accessible
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException($"Directory not found: {path}");

            var dirInfo = new DirectoryInfo(path);
            if (!dirInfo.Exists)
                throw new DirectoryNotFoundException($"Directory not accessible: {path}");

            _watcher = new FileSystemWatcher(path)
            {
                NotifyFilter = NotifyFilters.CreationTime |
                              NotifyFilters.LastWrite |
                              NotifyFilters.FileName |
                              NotifyFilters.Size,
                IncludeSubdirectories = false,
                EnableRaisingEvents = true
            };

            _watcher.Created += OnFileCreated;
            _watcher.Changed += OnFileChanged;
            _watcher.Deleted += OnFileDeleted;
            _watcher.Renamed += OnFileRenamed;
            _watcher.Error += OnWatcherError;

            _logger.LogInformation("Started monitoring desktop path: {Path}", path);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start file monitoring for path: {Path}", path);
            throw;
        }
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        _logger.LogError(e.GetException(), "FileSystemWatcher error occurred");
    }

    public Task StopMonitoringAsync()
    {
        if (_watcher != null)
        {
            try
            {
                _watcher.EnableRaisingEvents = false;

                // Unsubscribe from events before disposing
                _watcher.Created -= OnFileCreated;
                _watcher.Changed -= OnFileChanged;
                _watcher.Deleted -= OnFileDeleted;
                _watcher.Renamed -= OnFileRenamed;

                _watcher.Dispose();
                _watcher = null;
                _logger.LogInformation("Stopped file monitoring");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping file monitoring");
            }
        }

        return Task.CompletedTask;
    }



    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        _logger.LogDebug("File created: {Path}", e.FullPath);
        FileCreated?.Invoke(this, e);
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        _logger.LogDebug("File changed: {Path}", e.FullPath);
        FileChanged?.Invoke(this, e);
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        _logger.LogDebug("File deleted: {Path}", e.FullPath);
        FileDeleted?.Invoke(this, e);
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        _logger.LogDebug("File renamed: {OldPath} -> {NewPath}", e.OldFullPath, e.FullPath);
        FileRenamed?.Invoke(this, e);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                StopMonitoringAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during disposal");
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}