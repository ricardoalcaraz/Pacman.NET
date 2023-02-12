using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Pacman.NET.ConfigurationProvider;

public static class PacmanConfigProviderExtensions
{
    public static IConfigurationBuilder AddLinuxConfigFile(this IConfigurationBuilder builder, string path, bool optional = true, bool reloadOnChange = false)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentException("Path is required", nameof(path));
        }

        builder.Add<PacmanConfigurationSource>(s =>
        {
            s.FileProvider = new PhysicalFileProvider(Directory.GetCurrentDirectory());
            s.Path = path;
            s.Optional = optional;
            s.ReloadOnChange = reloadOnChange;
            s.ResolveFileProvider();
        });
        
        return builder;
    }
}