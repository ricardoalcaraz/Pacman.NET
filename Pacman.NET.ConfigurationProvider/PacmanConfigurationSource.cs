using Microsoft.Extensions.Configuration;

namespace Pacman.NET.ConfigurationProvider;

public class PacmanConfigurationSource : FileConfigurationSource
{
    public override IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new PacmanConfigurationProvider(this);
    }
}