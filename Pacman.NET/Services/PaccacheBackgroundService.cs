namespace Pacman.NET.Services;

public class PaccacheBackgroundService : BackgroundService
{
    private readonly ILogger<PaccacheBackgroundService> _logger;
    private readonly IOptionsMonitor<PacmanOptions> _options;

    public PaccacheBackgroundService(ILogger<PaccacheBackgroundService> logger, IOptionsMonitor<PacmanOptions> options)
    {
        _logger = logger;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (File.Exists("/usr/bin/paccache"))
        {
            _logger.LogInformation("Paccache service starting");

            while (!stoppingToken.IsCancellationRequested)
            {
                var options = _options.CurrentValue;
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            
                var cli = new Command("/usr/bin/find")
                    .WithArguments(options.CacheDirectory + @" -type d -exec paccache -v -r -k 2 -c {} \;");

                _logger.LogDebug("{Value}", cli);
                var result = await cli.ExecuteAsync(stoppingToken);
                _logger.LogInformation("Executed command {Command}", result.RunTime);
                await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
            }
        }
        else
        {
            _logger.LogWarning("Paccache does not exist");
        }
    }
}