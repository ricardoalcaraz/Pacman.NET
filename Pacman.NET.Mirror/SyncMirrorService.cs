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
            if (!IsRepoSynced())
            {
                var options = _options.CurrentValue;
                var rsyncCommand = Cli.Wrap("rsync")
                    .WithArguments(
                        $"-rlptH --safe-links --delete-delay --delay-updates -P --exclude='archive/*' {options.SyncUrl} {options.SyncPath}")
                    .WithStandardOutputPipe(PipeTarget.ToStream(Console.OpenStandardOutput()))
                    .WithStandardErrorPipe(PipeTarget.ToStream(Console.OpenStandardError()));
                var adminCommand = rsyncCommand
                    .WithTargetFile("sudo")
                    .WithArguments($"{rsyncCommand.TargetFilePath} {rsyncCommand.Arguments}");

                var result = await adminCommand.ExecuteAsync(stoppingToken, stoppingToken);
                
                _logger.LogInformation("Finished syncing in {Time}", result.RunTime);
            }

            await Task.Delay(TimeSpan.FromMinutes(360 - Random.Shared.Next(60)), stoppingToken);
        }
    }

    private bool IsRepoSynced()
    {
        var lastSync = File.ReadAllText(Path.Combine(_options.CurrentValue.SyncPath, "lastsync"));
        if (int.TryParse(lastSync, out var lastSyncUtc))
        {
            var syncTime = DateTimeOffset.FromUnixTimeSeconds(lastSyncUtc);
            if (syncTime + TimeSpan.FromHours(5) <= DateTimeOffset.Now)
            {
                _logger.LogInformation("Sync required last sync was {Time}", syncTime);
                return false;
            }
            else
            {
                _logger.LogInformation("Repo was last synced at {Time}", syncTime);
                return true;
            }
        }

        return false;
    }
}



public class SyncMirrorOptions
{
    public string SyncUrl { get; set; }
    public string SyncPath { get; set; }
}