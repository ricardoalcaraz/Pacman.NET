using CliWrap;
using Microsoft.Extensions.Options;

namespace Pacman.NET.Mirror;

public class SyncMirrorService : BackgroundService
{
    private ILogger<SyncMirrorService> _logger;
    private IOptionsMonitor<SyncMirrorOptions> _options;
    public SyncMirrorService(ILogger<SyncMirrorService> logger, IOptionsMonitor<SyncMirrorOptions> options)
    {
        _logger = logger;
        _options = options;
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _options.CurrentValue;
        var rsyncCommand = Cli.Wrap("rsync")
            .WithArguments($"-rlptH --safe-links --delete-delay --delay-updates -P {options.SyncUrl} {options.SyncPath}")
            .WithStandardOutputPipe(PipeTarget.ToStream(Console.OpenStandardOutput()))
            .WithStandardErrorPipe(PipeTarget.ToStream(Console.OpenStandardError()));
        var adminCommand = rsyncCommand
            .WithTargetFile("sudo")
            .WithArguments($"{rsyncCommand.TargetFilePath} {rsyncCommand.Arguments}");

        var result = await adminCommand.ExecuteAsync(stoppingToken, stoppingToken);
        
        _logger.LogInformation("Finished syncing in {Time}", result.RunTime);
    }
}

public class SyncMirrorOptions
{
    public string SyncUrl { get; set; }
    public string SyncPath { get; set; }
}