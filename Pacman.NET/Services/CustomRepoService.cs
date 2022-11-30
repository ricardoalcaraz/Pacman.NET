using Microsoft.Extensions.FileProviders;

namespace Pacman.NET.Services;

public class CustomRepoService : BackgroundService
{
    private readonly IOptions<ApplicationOptions> _options;
    private readonly ILogger<CustomRepoService> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly Dictionary<string, PhysicalFileProvider> _fileProviders = new();

    private const UnixFileMode USER_ONLY = UnixFileMode.UserExecute | UnixFileMode.UserExecute | UnixFileMode.UserExecute;
    
    public CustomRepoService(IOptions<ApplicationOptions> options, ILogger<CustomRepoService> logger, IWebHostEnvironment env)
    {
        _options = options;
        _logger = logger;
        _env = env;
    }
    
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _options.Value;
        foreach (var customRepo in options.CustomRepos)
        {
            var directoryInfo = Directory.CreateDirectory(Path.Combine(_env.ContentRootPath, customRepo.Name));
            if (directoryInfo.Exists)
            {
                _fileProviders.Add(customRepo.Name, new PhysicalFileProvider(directoryInfo.FullName));
                _logger.LogInformation("Created directory for custom repo {Name}", customRepo);
            }
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

    public bool TryGetFile(string repo, string fileName, out Stream fileStream)
    {
        var fileProvider = _fileProviders[repo];
        var file = fileProvider.GetFileInfo(fileName);
        fileStream = file.Exists ? file.CreateReadStream() : Stream.Null;
        return file.Exists;
    }
}