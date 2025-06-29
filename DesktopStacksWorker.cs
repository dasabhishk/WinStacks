using DesktopStacksService.Configuration;
using DesktopStacksService.Services;
using Microsoft.Extensions.Options;

namespace DesktopStacksService;

public class DesktopStacksWorker : BackgroundService
{
    private readonly SemaphoreSlim _organizationSemaphore = new(1, 1);
    private readonly CancellationTokenSource _internalCts = new();
    private readonly ILogger<DesktopStacksWorker> _logger;
    private readonly DesktopStacksOptions _options;
    private readonly IFileMonitorService _fileMonitor;
    private readonly IFileOrganizationService _organizationService;

    public DesktopStacksWorker(
        ILogger<DesktopStacksWorker> logger,
        IOptions<DesktopStacksOptions> options,
        IFileMonitorService fileMonitor,
        IFileOrganizationService organizationService)
    {
        _logger = logger;
        _options = options.Value;
        _fileMonitor = fileMonitor;
        _organizationService = organizationService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Combine tokens
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, _internalCts.Token);
        var combinedToken = linkedCts.Token;

        _logger.LogInformation("Desktop Stacks Service starting...");
        _logger.LogInformation("Monitoring path: {Path}", _options.MonitorPath);
        _logger.LogInformation("Organization strategy: {Strategy}", _options.OrganizationStrategy);
        _logger.LogInformation("Check interval: {Interval}", _options.CheckInterval);

        // Validate that the monitor path exists and is accessible
        if (!Directory.Exists(_options.MonitorPath))
        {
            _logger.LogError("Monitor path does not exist: {Path}", _options.MonitorPath);
            throw new DirectoryNotFoundException($"Monitor path does not exist: {_options.MonitorPath}");
        }

        await _fileMonitor.StartMonitoringAsync(_options.MonitorPath, combinedToken);

        _fileMonitor.FileCreated += OnFileEvent;
        _fileMonitor.FileChanged += OnFileEvent;

        try
        {
            if (_options.EnableAutoOrganization)
            {
                await OrganizeDesktopFiles(combinedToken);
            }

            while (!combinedToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_options.CheckInterval, combinedToken);

                    if (_options.EnableAutoOrganization)
                    {
                        await OrganizeDesktopFiles(combinedToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in main service loop");
                    await Task.Delay(TimeSpan.FromMinutes(1), combinedToken);
                }
            }
        }
        finally
        {
            _logger.LogInformation("Desktop Stacks Service stopping...");
            await _fileMonitor.StopMonitoringAsync();
        }
    }

    private void OnFileEvent(object? sender, FileSystemEventArgs e)
    {
        _logger.LogDebug("File event detected: {EventType} - {Path}", e.ChangeType, e.FullPath);

        // Fire and forget with proper error handling
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), _internalCts.Token);

                if (_options.EnableAutoOrganization)
                {
                    await OrganizeDesktopFiles(_internalCts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in file event handler");
            }
        });
    }

    private async Task OrganizeDesktopFiles(CancellationToken cancellationToken)
    {
        // Prevent concurrent organization
        if (!await _organizationSemaphore.WaitAsync(0, cancellationToken))
        {
            _logger.LogDebug("Organization already in progress, skipping");
            return;
        }

        try
        {
            _logger.LogDebug("Starting desktop organization cycle");

            var files = await _organizationService.GetDesktopFilesAsync(_options.MonitorPath, cancellationToken);

            if (!files.Any())
            {
                _logger.LogDebug("No files found to organize");
                return;
            }

            var stacks = await _organizationService.OrganizeFilesAsync(files, cancellationToken);
            await _organizationService.CreateStacksAsync(stacks, cancellationToken);

            _logger.LogInformation("Desktop organization cycle completed successfully");
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during desktop organization");
        }
        finally
        {
            _organizationSemaphore.Release();
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Desktop Stacks Service received stop signal");

        // Signal internal cancellation
        _internalCts.Cancel();

        // Unsubscribe from events
        _fileMonitor.FileCreated -= OnFileEvent;
        _fileMonitor.FileChanged -= OnFileEvent;

        // Wait for ongoing operations
        await _organizationSemaphore.WaitAsync(cancellationToken);

        await base.StopAsync(cancellationToken);
    }
    protected void Dispose(bool disposing)
    {
        if (disposing)
        {
            _organizationSemaphore?.Dispose();
            _internalCts?.Dispose();
        }
        base.Dispose();
    }
}