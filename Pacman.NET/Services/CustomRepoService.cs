using System.Text;
using CliWrap;
using Microsoft.Extensions.FileProviders;

namespace Pacman.NET.Services;

public class CustomRepoService : BackgroundService
{
    private readonly IOptions<RepositoryOptions> _options;
    private readonly ILogger<CustomRepoService> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly Dictionary<string, PhysicalFileProvider> _fileProviders = new();

    private const UnixFileMode USER_ONLY = UnixFileMode.UserExecute | UnixFileMode.UserExecute | UnixFileMode.UserExecute;
    
    public CustomRepoService(IOptions<RepositoryOptions> options, ILogger<CustomRepoService> logger, IWebHostEnvironment env)
    {
        _options = options;
        _logger = logger;
        _env = env;
    }
    
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _options.Value;
        foreach (var customRepo in options.RepositoryProvider.GetDirectoryContents(string.Empty))
        {
            var directoryInfo = Directory.CreateDirectory(Path.Combine(customRepo.PhysicalPath!, "os", "x86_64"));
            
            if (directoryInfo.Exists && File.Exists("/usr/bin/repo-add"))
            {
                var stringBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();
                var repo = await Cli.Wrap("/usr/bin/repo-add")
                    .WithArguments($"{customRepo.Name}.db.tar.gz")
                    .WithWorkingDirectory(directoryInfo.FullName)
                    .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stringBuilder))
                    .WithStandardErrorPipe(PipeTarget.ToStringBuilder(errorBuilder))
                    .ExecuteAsync(stoppingToken);
                _logger.LogInformation("{Out}", stringBuilder.ToString());
                _logger.LogInformation("{Error}", errorBuilder.ToString());
                _logger.LogInformation("Created directory for custom repo {Name}", customRepo);
            }
        }
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