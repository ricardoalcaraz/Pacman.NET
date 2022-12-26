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


builder.UseSystemd();
builder.UseConsoleLifetime();
builder.ConfigureLogging(c =>
{
    c.AddConsole();
    c.AddDebug();
});
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
    var tmpFileName = Path.GetTempFileName();
    using var process = new Process();
    process.StartInfo = new ProcessStartInfo("/usr/bin/gpg", $"--armor --output {tmpFileName} --export {keyId}")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
    };
    process.Start();
    process.WaitForExit();

    process.StartInfo = new ProcessStartInfo("/usr/bin/pacman-key", $"--add {tmpFileName}")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
    };
    process.Start();
    process.WaitForExit();

    process.StartInfo = new ProcessStartInfo("/usr/bin/pacman-key", $"--lsign {keyId}")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
    };
    process.Start();
    process.WaitForExit();
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


