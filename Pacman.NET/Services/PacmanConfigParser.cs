using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;

namespace Pacman.NET.Services;

public record PacmanConfigSection(string Name, List<string> Settings);
public class PacmanConfigParser
{
    private readonly ILogger<PacmanConfigParser> _logger;

    public PacmanConfigParser(ILogger<PacmanConfigParser>? logger)
    {
        _logger = logger ?? NullLogger<PacmanConfigParser>.Instance;
    }
    
    
    public async Task<Dictionary<string, string>> ParseConfig(string filePath)
    {
        var configStream = File.OpenText(filePath);
        var allSettings = new Dictionary<string, Dictionary<string, string>>();

        while (!configStream.EndOfStream)
        {
            var configLine = await configStream.ReadLineAsync();
            configLine = configLine?.Trim() ?? string.Empty;
            var sectionName = string.Empty;
            
            if (configLine is ['#', ..])
            {
                _logger.LogDebug("Parsed comment");
            }
            else if (configLine is ['#'] or [])
            {  
                _logger.LogDebug("Parsed empty line");
            }
            else if (configLine is ['[', .., ']'])
            {
                sectionName = configLine[1..^1];
                allSettings.TryAdd(sectionName, new Dictionary<string, string>());
            }
            else
            {
                var settings = allSettings[sectionName];
                var configValues = configLine.Split('=', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                var keyValueEntry = configValues switch
                {
                    [var boolKeyName] => new KeyValuePair<string, string>(boolKeyName, string.Empty),
                    [var keyName, var val] => new KeyValuePair<string, string>(keyName, val),
                    _ => new KeyValuePair<string, string>(string.Empty, string.Empty)
                };

                if (settings.TryAdd(keyValueEntry.Key, keyValueEntry.Value))
                {
                    _logger.LogInformation("Added {Entry}", keyValueEntry);
                }
            }
        }

        return new Dictionary<string, string>();
    }

    public async IAsyncEnumerable<string> ParseMirrorlist(string filePath, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var streamReader = File.OpenText(filePath);
        while (!streamReader.EndOfStream)
        {
            var configLine = (await streamReader.ReadLineAsync(cancellationToken))?.Trim();
            if (string.IsNullOrWhiteSpace(configLine) || configLine.StartsWith('#'))
            {
                _logger.LogDebug("Ignored empty line");
            }
            else
            {
                var configVal = configLine.Split("=", StringSplitOptions.TrimEntries);
                if (configVal is ["Server", var url])
                {
                    _logger.LogDebug("Parsed url {Url}", url);
                    yield return url;
                }
                else
                {
                    _logger.LogWarning("Ignoring invalid line {Content}", configLine);
                }
            }
        }
    }
    
    public string GetSectionName(string sectionName) => sectionName[1..^1];
}

public record PacmanConfig(string Name, string? Value);

public enum ConfigSettingType
{
    Empty,
    Comment,
    SectionHeader,
    ConfigValue
}