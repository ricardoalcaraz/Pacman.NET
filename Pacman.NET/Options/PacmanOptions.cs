using Microsoft.Extensions.FileProviders;

namespace Pacman.NET.Options
{
    public record PacmanOptions
    {
        public required string SyncDirectory { get; init; }
        public required bool EnableReverseProxy { get; init; }
        public required bool EnableCache { get; init; }
        public required string CacheDirectory { get; init; }
        public required string LogDirectory { get; init; }
        public required string MirrorUrl { get; init; }
        public required List<string> Repos { get; init; }
    }

    public record DatabaseOptions
    {
        public string Name { get; set; }
        public string Path { get; set; }
    }

    public record PackageCacheOptions
    {
        public string BasePath { get; set; }
        public IFileProvider FileProvider { get; set; }
        public string SavePath { get; set; }
    }
}