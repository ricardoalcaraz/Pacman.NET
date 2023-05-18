// See https://aka.ms/new-console-template for more information

using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

Console.WriteLine("Hello, World!");
var builder = Host.CreateDefaultBuilder(args);

builder.UseConsoleLifetime();

var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();

var keyId = args.LastOrDefault() ?? string.Empty;

foreach (var arg in args)
{
    switch (arg)
    {
        case "-i" or "--dbDir":
            logger.LogInformation("Database directory: {DbDir}", arg);
            break;
        case "-f" or "--file":
            logger.LogInformation("Cache dir: {ConfigFile}", arg);
            break;
        default:
            logger.LogInformation("Invalid argument: {Arg}", arg);
            break;
    }
    logger.LogInformation("Argument: {Arg}", arg);
}

AddPubKey(keyId);

var config = host.Services.GetRequiredService<IConfiguration>();
var user = Environment.UserName;
var cliOptions = config.Get<CliAppOptions>()!;

void AddPubKey(string keyId)
{
    using var process = new Process();
    process.StartInfo = new ProcessStartInfo("/usr/bin/gpg", $"--armor --export {keyId}")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
    };
    
    process.Start();
    var tmpFileName = Path.GetTempFileName();

    using var tmpFileStream = new FileStream(tmpFileName, new FileStreamOptions
    {
        Access = FileAccess.Write,
        BufferSize = 0,
        Mode = FileMode.Create,
        Options = FileOptions.DeleteOnClose,
        Share = FileShare.Delete
    });
    process.StandardOutput.BaseStream.CopyTo(tmpFileStream);

    logger.LogDebug("{Key}", File.ReadAllText(tmpFileName));
    logger.LogDebug("Wrote key to {File}", tmpFileName);
    using var addKeyProcess = Process.Start("/usr/bin/pacman-key", $"--add {tmpFileName}");
    
    addKeyProcess.WaitForExit();

    using var signKeyProcess = new Process();//Process.Start("/usr/bin/pacman-key", $"-u ralcaraz --lsign {keyId}");
    signKeyProcess.StartInfo = new ProcessStartInfo("/usr/bin/gpg", $"--armor --export {keyId}")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
    };
    signKeyProcess.Start();
    signKeyProcess.WaitForExit();
}

var compositeFileProvider = new CompositeFileProvider(
    new PhysicalFileProvider(cliOptions.DbDir),
    new PhysicalFileProvider(cliOptions.CacheDir)
);

compositeFileProvider.GetFileInfo("");
logger.LogInformation("Starting app with {Options}", cliOptions);

await host.RunAsync();

public record CliAppOptions
{
    [Required]
    public required string DbDir { get; init; }
    
    [Required]
    public required string CacheDir { get; init; }
    
    public string Gpg { get; init; }
}


