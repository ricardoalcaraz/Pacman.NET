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
                    [_] => new KeyValuePair<string, string>(configValues[0], string.Empty),
                    [_, _] => new KeyValuePair<string, string>(configValues[0], configValues[1]),
                    _ => new KeyValuePair<string, string>(string.Empty, string.Empty)
                };
                var file = new FileStream("", new FileStreamOptions
                {
                    Access = (FileAccess)0,
                    BufferSize = 0,
                    Mode = (FileMode)0,
                    Options = FileOptions.None,
                    PreallocationSize = 0,
                    Share = FileShare.None,
                });

                      var configSettingName = keyValueEntry.Key;
                if (string.IsNullOrWhiteSpace(configSettingName))
                {
                    
                }
                else if (settings.TryAdd(keyValueEntry.Key, keyValueEntry.Value))
                {
                    _logger.LogInformation("Added {Entry}", keyValueEntry);

                }
                else
                {
                    settings[keyValueEntry.Key] = keyValueEntry.Value;
                }
            }
            // optionType = configOption[1..^1]; //return parsed settings
            // else if (configOption is ['#'] or ['#', ..] or [])
            //     optionType = string.Empty;
            // else
            //     optionType = configSetting = configOption; //parse and add setting
            //
            // var pacmanSetting = configSetting
            // if (allSettings.TryGetValue(sectionName, out var configSection))
            // {
            //     ParseSection(configSection);
            // }
            // else
            // {
            //     allSettings[sectionName] = new PacmanConfigSection(sectionName, new List<string>());
            // }
            //
            // if (configOption.TrimStart().StartsWith('#'))
            // {
            //     _logger.LogDebug("ConfigValue is ignored {Line}", configOption);
            // }
            // else if(configOption.Any())
            // {
            //     var configValue = configOption.Split('=', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            //     if (configValue is [_] or [_, _])
            //     {
            //         var pacmanConfigSetting = new PacmanConfigOption(configValue[0], configValue.LastOrDefault());
            //         
            //     }
            //     else
            //     {
            //         _logger.LogWarning("Invalid config option for {Line}", configOption);
            //     }
            // }
        }

        return new Dictionary<string, string>();
    }

    public async IAsyncEnumerable<string> ParseMirrorlist(string filePath, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var streamReader = File.OpenText(filePath);
        while (!streamReader.EndOfStream)
        {
            var configLine = await streamReader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(configLine) || configLine.StartsWith("#"))
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