using Microsoft.Extensions.FileProviders;

namespace Pacman.NET.Options;

public record PacmanOptions
{
    public required string BaseAddress { get; init; }
    public required string Configuration { get; init; }
    public required string CacheDirectory { get; set; }
    public required string DbDirectory { get; set; }
}

public record ApplicationOptions
{
    public required string BaseAddress { get; set; }
    public required bool CacheDb { get; set; }
    public required bool CachePackages { get; init; }
    public required bool VerifySignature { get; init; }
    public required string LogDirectory { get; init; }
    public required string MirrorUrl { get; init; }
    public required int CacheRefreshInterval { get; init; }
    public IEnumerable<CustomRepo> CustomRepos { get; set; } = Enumerable.Empty<CustomRepo>();
}


public record CustomRepo
{
    public required string Name { get; init; }
    public required string? SourceDirectory { get; init; }
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