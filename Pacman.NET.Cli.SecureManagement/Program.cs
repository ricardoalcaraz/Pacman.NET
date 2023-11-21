// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

Console.OpenStandardInput(4096);
var builder = Host.CreateDefaultBuilder(args);

builder.UseConsoleLifetime();

var host = builder.Build();
Extensions.Logger = host.Services.GetRequiredService<ILogger<Program>>();

var logger = host.Services.GetRequiredService<ILogger<Program>>();

var keyId = args.LastOrDefault() ?? string.Empty;
var hostingEnvironment = host.Services.GetRequiredService<IHostEnvironment>();

if(!hostingEnvironment.IsDevelopment() && Environment.IsPrivilegedProcess )
{
    logger.LogError("This program must be run as root");
    return;
}


await Extensions.AddPubKey(keyId);

var config = host.Services.GetRequiredService<IConfiguration>();
var cliOptions = config.Get<CliAppOptions>()!;

logger.LogInformation("Starting app with {Options}", cliOptions);

await host.RunAsync();

internal static class Extensions
{
    public static ILogger<Program>? Logger { get; set; }
    public static async Task AddPubKey(string id)
    {
        var gpgCommand = Cli.Wrap("gpg")
            .WithArguments($"--armor --export {id}");
        
        Logger?.LogInformation("{Gpg}", gpgCommand);
        
        await gpgCommand.ExecuteBufferedWithMetrics();
    }

    public static Command CreateCliCommand(this string command, params string[] args)
    {
        var cliCommand = Cli.Wrap(command)
            .WithArguments(argsBuilder =>
            {
                foreach (var arg in args)
                {
                    argsBuilder.Add(arg, false);
                }
            });
        
        return cliCommand;
    }
    
    private static void ProcessArgs()
    {
        Logger?.LogInformation("Running with privileges as {User}", Environment.UserName);

        foreach (var arg in Environment.GetCommandLineArgs().SkipLast(1))
        {
            switch (arg)
            {
                case "-i" or "--dbDir":
                    Logger?.LogInformation("Database directory: {DbDir}", arg);
                    break;
                case "-f" or "--file":
                    Logger?.LogInformation("Cache dir: {ConfigFile}", arg);
                    break;
                default:
                    Logger?.LogInformation("Invalid argument: {Arg}", arg);
                    break;
            }
            Logger?.LogInformation("Argument: {Arg}", arg);
        }
    }
}

