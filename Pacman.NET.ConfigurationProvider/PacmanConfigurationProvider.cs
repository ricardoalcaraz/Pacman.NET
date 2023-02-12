using Microsoft.Extensions.Configuration;

namespace Pacman.NET.ConfigurationProvider;

//based on libalpm
//Comments are only supported by beginning a line with the hash (#) symbol. Comments cannot begin in the middle of a line.
public class PacmanConfigurationProvider : FileConfigurationProvider
{
    public PacmanConfigurationProvider(FileConfigurationSource source) : base(source)
    {
    }

    public override void Load(Stream stream)
    {
        using var streamReader = new StreamReader(stream);
        var prefixName = string.Empty;

        while (!streamReader.EndOfStream)
        {
            var configLine = streamReader.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(configLine) || configLine.StartsWith('#'))
            {
                continue;
            }

            if (configLine.Contains('#'))
            {
                throw new InvalidOperationException($"Unexpected comment encountered when parsing {configLine}");
            }

            switch (configLine)
            {
                case "" or null or ['#'] or ['#', ..]:
                    break;
                case ['[', .. var sectionName, ']']:
                    prefixName = sectionName.Equals("options", StringComparison.OrdinalIgnoreCase) ? "Pacman:" : $"{sectionName}:";
                    break;
                case [..] when !configLine.Contains('#'):
                    Data.Add(configLine.Split('=', StringSplitOptions.RemoveEmptyEntries) switch
                    {
                        [var key] => new KeyValuePair<string, string?>($"{prefixName}{key}", true.ToString()),
                        //[var key, var value] when Data.ContainsKey(key) => new KeyValuePair<string, string?>($"{prefixName}{key}", true.ToString()),
                        [var key, var value] => new KeyValuePair<string, string?>(key, value),
                        _ => throw new InvalidOperationException("Unable to parse line")
                    });
                    break;
            }

        }
    }
}