namespace Pacman.NET.Services;

public class InformationLoggingService : BackgroundService
{
    private readonly ILogger<InformationLoggingService> _logger;


    public InformationLoggingService(ILogger<InformationLoggingService> logger)
    {
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine($"Running on dotnet {Environment.Version}.{Environment.Version.Minor}");
        Console.WriteLine($"OS: {await Cli.Wrap("uname").ExecuteBufferedWithMetrics()}");
        await LogSystemDrives();
        
        var pageSize = System.Environment.SystemPageSize;
        Console.WriteLine($"Page size: {pageSize}");
    }
    
    private Task LogSystemDrives()
    {
        Console.WriteLine("Listing logical drives: ");
        foreach (var logicalDrive in Environment.GetLogicalDrives())
        {
            Console.WriteLine(logicalDrive);
        }

        return Task.CompletedTask;
    }
}
