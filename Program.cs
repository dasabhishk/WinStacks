using DesktopStacksService;
using DesktopStacksService.Configuration;
using DesktopStacksService.Services;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Configure Serilog
builder.Services.AddSerilog((services, lc) => lc
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/desktop-stacks-.txt", rollingInterval: RollingInterval.Day));

// Add configuration with validation
builder.Services.Configure<DesktopStacksOptions>(options =>
{
    var section = builder.Configuration.GetSection("DesktopStacks");
    section.Bind(options);

    // Validate configuration after binding
    try
    {
        ValidateDesktopStacksOptions(options);
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Invalid configuration for DesktopStacks");
        throw;
    }
});

// Add configuration validation
builder.Services.AddOptions<DesktopStacksOptions>()
    .Configure(options => builder.Configuration.GetSection("DesktopStacks").Bind(options))
    .ValidateDataAnnotations() 
    .Validate(options => ValidateDesktopStacksOptions(options), "Invalid DesktopStacks configuration")
    .ValidateOnStart();

// Add services
builder.Services.AddSingleton<IFileMonitorService, FileMonitorService>();
builder.Services.AddSingleton<IFileOrganizationService, FileOrganizationService>();
builder.Services.AddHostedService<DesktopStacksWorker>();

// Configure as Windows Service
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Desktop Stacks Service";
});

var host = builder.Build();

try
{
    Log.Information("Starting Desktop Stacks Service");
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
static bool ValidateDesktopStacksOptions(DesktopStacksOptions options)
{
    try
    {
        // This will trigger the validation in the setter
        var path = options.MonitorPath;
        return Directory.Exists(path);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Configuration validation failed for MonitorPath: {Path}", options.MonitorPath);
        return false;
    }
}