using System.ComponentModel.DataAnnotations;
using CliWrap;
using Microsoft.Extensions.Logging;

namespace Pacman.NET.IntegrationTests.Services;

public class WakeOnLanService
{
    private readonly ILogger<WakeOnLanService> _logger;
    private readonly IOptions<WakeOnLanOptions> _options;
    private readonly Command _wol;


    public WakeOnLanService(ILogger<WakeOnLanService> logger, IOptions<WakeOnLanOptions> options)
    {
        _logger = logger;
        _options = options;
        _wol = new Command(_options.Value.ProgramPath);
    }

    protected async Task<bool> TryExecuteCommand(Command command, CancellationToken ctx = default)
    {
        _logger.LogInformation("Starting service to run {Command}", _options.Value.ProgramPath);

        
        var result = await _wol.ExecuteAsync(ctx);
        _logger.LogDebug("{Command} executed in {Time} with exit code: {Code}", command.ToString(), result.RunTime, result.ExitCode);
        
        return result.ExitCode == 0;
    }
    
    public async Task WakeByMacAddress(string macAddress, CancellationToken ctx = default)
    {
        var commandWithArgs = _wol.WithArguments(macAddress);
        if (await TryExecuteCommand(commandWithArgs, ctx))
        {
            _logger.LogInformation("Successfully woke {MacAddress}", macAddress);
        }
        else
        {
            _logger.LogWarning("Unable to wake {MacAddress}", macAddress);
        }
    }
}

public class WakeOnLanOptions
{
    [Required]
    public string ProgramPath { get; set; } = "/usr/bin/wol";
}