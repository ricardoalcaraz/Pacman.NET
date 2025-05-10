using System.Threading.Channels;
using CliWrap;
using Microsoft.Extensions.Options;

namespace Pacman.NET.Mirror;

public class SyncMirrorService(ILogger<SyncMirrorService> logger, IOptionsMonitor<SyncMirrorOptions> options)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            if (!IsRepoSynced())
            {
                var options1 = options.CurrentValue;
                var rsyncCommand = Cli.Wrap("rsync")
                    .WithArguments(
                        $"-rlptH --safe-links --delete-delay --delay-updates -P --exclude='archive/*' {options1.SyncUrl} {options1.SyncPath}")
                    .WithStandardOutputPipe(PipeTarget.ToStream(Console.OpenStandardOutput()))
                    .WithStandardErrorPipe(PipeTarget.ToStream(Console.OpenStandardError()));
                var adminCommand = rsyncCommand
                    .WithTargetFile("sudo")
                    .WithArguments($"{rsyncCommand.TargetFilePath} {rsyncCommand.Arguments}");

                var result = await adminCommand.ExecuteAsync(stoppingToken, stoppingToken);
                
                logger.LogInformation("Finished syncing in {Time}", result.RunTime);
            }

            await Task.Delay(TimeSpan.FromMinutes(360 - Random.Shared.Next(60)), stoppingToken);
        }
    }

    private bool IsRepoSynced()
    {
        var lastSync = File.ReadAllText(Path.Combine(options.CurrentValue.SyncPath, "lastsync"));
        if (int.TryParse(lastSync, out var lastSyncUtc))
        {
            var syncTime = DateTimeOffset.FromUnixTimeSeconds(lastSyncUtc);
            if (syncTime + TimeSpan.FromHours(5) <= DateTimeOffset.Now)
            {
                logger.LogInformation("Sync required last sync was {Time}", syncTime);
                return false;
            }
            else
            {
                logger.LogInformation("Repo was last synced at {Time}", syncTime);
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