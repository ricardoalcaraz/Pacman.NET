using System.Diagnostics;

namespace Pacman.NET.Services;

public class CustomRepoService : BackgroundService
{
    private readonly IOptions<PacmanOptions> _options;
    private readonly ILogger<CustomRepoService> _logger;

    private const UnixFileMode USER_ONLY = UnixFileMode.UserExecute | UnixFileMode.UserExecute | UnixFileMode.UserExecute;
    
    public CustomRepoService(IOptions<PacmanOptions> options, ILogger<CustomRepoService> logger)
    {
        _options = options;
        _logger = logger;
    }
    
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _options.Value;
        if (string.IsNullOrWhiteSpace(options.DbDirectory))
        {
            _logger.LogInformation("No custom repos will be created");
        }

        var dbDir = new DirectoryInfo(options.DbDirectory);
        if (dbDir.Exists)
        {
            _logger.LogInformation("Found Db directory at {Dir}", dbDir.FullName);
        }
        else
        {
            dbDir.Create();
        }

        return Task.CompletedTask;
        // foreach (var database in options.CustomRepos)
        // {
        //     var dbDirectoryInfo = Directory.CreateDirectory($"./{database.Name}", USER_ONLY);
        //     var dbFileName = $"{database.Name}.db";
        //     var dbFileInfo = new FileInfo(Path.Combine(dbDirectoryInfo.FullName, dbFileName));
        //     if (dbFileInfo.Exists)
        //     {
        //         _logger.LogInformation("Found existing repo for {Name}", database);
        //     }
        //     else
        //     {
        //         var process = new Process();
        //         process.StartInfo = new ProcessStartInfo
        //         {
        //             Arguments = $"{dbFileInfo.Name}.tar.gz",
        //             CreateNoWindow = true,
        //             FileName = "/usr/bin/repo-add",
        //             RedirectStandardOutput = true,
        //             UseShellExecute = false,
        //             WorkingDirectory = dbDirectoryInfo.FullName
        //         };
        //         process.Start();
        //         await process.WaitForExitAsync(stoppingToken);
        //         if (File.Exists(dbFileName))
        //         {
        //             var standardOut = await process.StandardOutput.ReadToEndAsync(stoppingToken);
        //             _logger.LogInformation("Created folder for {Name}", dbFileName);
        //             _logger.LogDebug("{Console}", standardOut);
        //         }
        //         else
        //         {
        //             var errorOut = await process.StandardError.ReadToEndAsync(stoppingToken);
        //             _logger.LogWarning("Unable to create directory for {Name}", dbFileName);
        //             _logger.LogDebug("{Console}", errorOut);
        //         }
        //     }
        // }
    }
}