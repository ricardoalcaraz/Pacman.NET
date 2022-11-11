namespace Pacman.NET.Services;


public record PacmanConfigSection(string Name, List<string> Settings);
public class PacmanConfigParser
{
    private readonly ILogger<PacmanConfigParser> _logger;

    public PacmanConfigParser(ILogger<PacmanConfigParser> logger)
    {
        _logger = logger;
    }
    public async Task<Dictionary<string, string>> ParseConfig()
    {
        var configFileLocation = "/etc/pacman.conf";
        var configStream = File.OpenText(configFileLocation);
        var pacmanConfigSetting = new PacmanOptions
        {
            SyncDirectory = null,
            EnableReverseProxy = false,
            EnableCache = false,
            CacheDirectory = null,
            LogDirectory = null,
            MirrorUrl = null,
            Repos = null
        };
        _logger.LogInformation("Found setting for {Section} of {Setting}", "", pacmanConfigSetting);
        var currentState = "ParseOption";
        var allSettings = new Dictionary<string, Dictionary<string, string>>();

        while (!configStream.EndOfStream)
        {
            var configLine = await configStream.ReadLineAsync();
            configLine = configLine?.Trim() ?? string.Empty;
            var sectionName = string.Empty;
            var configSetting = string.Empty;
            
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
                    UnixCreateMode = null
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

    public string GetSectionName(string sectionName) => sectionName[1..^1];
}

public record PacmanConfigOption(string Name, string? Value);

public enum ConfigSettingType
{
    Empty,
    Comment,
    SectionHeader,
    ConfigValue
}