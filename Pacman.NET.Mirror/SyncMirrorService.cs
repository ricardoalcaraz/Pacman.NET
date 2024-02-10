using System.Threading.Channels;
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
        while(!stoppingToken.IsCancellationRequested)
        {
            var options = _options.CurrentValue;
            var rsyncCommand = Cli.Wrap("rsync")
                .WithArguments(
                    $"-rlptH --safe-links --delete-delay --delay-updates {options.SyncUrl} {options.SyncPath}")
                .WithStandardOutputPipe(PipeTarget.ToStream(Console.OpenStandardOutput()))
                .WithStandardErrorPipe(PipeTarget.ToStream(Console.OpenStandardError()));
            var adminCommand = rsyncCommand
                .WithTargetFile("sudo")
                .WithArguments($"{rsyncCommand.TargetFilePath} {rsyncCommand.Arguments}");

            var result = await adminCommand.ExecuteAsync(stoppingToken, stoppingToken);

            _logger.LogInformation("Finished syncing in {Time}", result.RunTime);

            await Task.Delay(TimeSpan.FromMinutes(360 - Random.Shared.Next(30)), stoppingToken);
        }
    }
}



public class SyncMirrorOptions
{
    public string SyncUrl { get; set; }
    public string SyncPath { get; set; }
}