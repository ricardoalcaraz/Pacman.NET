using System.ComponentModel.DataAnnotations;
using CliWrap;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Pacman.NET.IntegrationTests.Services;

public class InitializationService : BackgroundService
{
    private readonly ILogger<InitializationService> _logger;
    private readonly IOptionsMonitor<InitializationServiceOptions> _options;
    private readonly IWebHostEnvironment _env;
    private Task? _updateTasks;


    public InitializationService(ILogger<InitializationService> logger, IOptionsMonitor<InitializationServiceOptions> options, IWebHostEnvironment env)
    {
        _logger = logger;
        _options = options;
        _env = env;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Initialization Service");
        var cacheRefreshToken = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

        _options.OnChange((opt, name) =>
        {
            _logger.LogInformation("{Name} options changed: {Options}", name?? nameof(InitializationService), opt);
            var oldToken = cacheRefreshToken;

                try
                {
                    cacheRefreshToken = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    oldToken.Cancel();
                    _updateTasks = Task.Run(() => MirrorRefresh(cacheRefreshToken.Token), stoppingToken);
                }
                finally
                {
                    oldToken.Dispose();
                }
            //restart service
        });

    }

    private void InitializeDirectories()
    {
        var dbFileProvider = CreateFileProvider("db");
        var dbProviders = dbFileProvider
            .GetDirectoryContents(string.Empty)
            .Where(f => f.IsDirectory && !string.IsNullOrWhiteSpace(f.PhysicalPath))
            .Select(f =>
            {
                _logger.LogInformation("Found custom repo {Name}", f.Name);
                return new PhysicalFileProvider(f.PhysicalPath!) as IFileProvider;
            }).ToArray();
        
        var db = new CompositeFileProvider(dbProviders);
        var packageProvider = new CompositeFileProvider(new List<IFileProvider>(dbProviders){ CreateFileProvider("cache") });
        var file = new FileInfo("ricardoalcaraz.dev.db");
        if(file.Extension.EndsWith("db"))
        {
            if (file.ResolveLinkTarget(true) is FileInfo physicalPath)
            {
                file = physicalPath;
            }
            var isModified = file.LastWriteTimeUtc > DateTime.UtcNow.AddMinutes(-1);
            _logger.LogInformation("Found db file {Name}", file.Name);
        }
        else
        {
            _logger.LogInformation("No db file found");
        }
    }


    private IFileProvider CreateFileProvider(string name)
    {
        var fileProviderPath = _env.IsDevelopment()
            ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            : _env.ContentRootPath;

        var directoryInfo = Directory.CreateDirectory(Path.Combine(fileProviderPath, name));
        if (directoryInfo.Exists)
        {
            _logger.LogInformation("Found directory {Name}", directoryInfo);
        }

        return new PhysicalFileProvider(directoryInfo.FullName);
    }
    protected async Task MirrorRefresh(CancellationToken cancellationToken)
    {
        if (_options.CurrentValue.IsEnabled)
        {
            try
            {
                _logger.LogInformation("Refreshing Mirror Cache in {Time}", _options.CurrentValue.MirrorRefreshInterval);
                var lastUpdatedFile = _options.CurrentValue.CacheFileProvider.GetFileInfo("last_modified");
                _logger.LogInformation("Refreshing Mirror Cache last updated {Time}", lastUpdatedFile.LastModified);

                while (!cancellationToken.IsCancellationRequested)
                {

                    var lastUpdate = lastUpdatedFile.Exists 
                        ? DateTimeOffset.UtcNow.ToUnixTimeSeconds() - lastUpdatedFile.LastModified.ToUnixTimeSeconds() 
                        : DateTimeOffset.MaxValue.ToUnixTimeSeconds();
                    
                        if (lastUpdate > _options.CurrentValue.MirrorRefreshInterval.TotalSeconds)
                        {
                            _logger.LogInformation("Mirror Cache was last modified on {Date} and will be refreshed", lastUpdatedFile.LastModified);
                            //refresh cache
                        }
                        else
                        {
                            _logger.LogInformation("Mirror Cache is up to date");
                        }
                        await using var info = File.CreateText(DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
                        _logger.LogInformation("Refreshed mirror cache at {Date}", DateTime.Now);

                    var mirrorRefreshInterval = _options.CurrentValue.MirrorRefreshInterval;
                    
                    _logger.LogInformation("Successfully refreshed mirrors. Next update is in {Time}", mirrorRefreshInterval);
                    await Task.Delay(mirrorRefreshInterval, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Stopping mirror updates");
            }
            finally
            {
                _logger.LogInformation("Mirror updates have stopped running");
            }
        }
    }
}

public record InitializationServiceOptions
{
    [Required]
    public IFileProvider CacheFileProvider { get; set; }
    
    [Required]
    public IFileProvider RepositoryProvider { get; set; }
    
    [Required]
    public TimeSpan MirrorRefreshInterval { get; set; }
    
    [Required]
    public TimeSpan CacheRefreshInterval { get; set; }

    public bool IsEnabled { get; set; }
}