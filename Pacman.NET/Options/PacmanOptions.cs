using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;

namespace Pacman.NET.Options;

public record PacmanOptions
{
    [Required]
    public required string BaseAddress { get; init; } = "archlinux";
    
    [Required]
    [AbsoluteFilePath]
    public required string CacheDirectory { get; init; }
    
    [Required]
    public string? DbDirectory { get; set; }
    
    [Required]
    public required string SaveDirectory { get; set; } 
}

public record ApplicationOptions
{
    public required string BaseAddress { get; set; }
    public required bool CacheDb { get; set; }
    public required bool CachePackages { get; init; }
    public required bool VerifySignature { get; init; }
    public required string CustomRepoDir { get; set; }
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
    public required string Name { get; set; }
    public required string Path { get; set; }
}


public record PackageCacheOptions
{
    public required string BasePath { get; set; }
    public required IFileProvider FileProvider { get; set; }
    public required string SavePath { get; set; }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
    AllowMultiple = false)]
public sealed class AbsoluteFilePath : ValidationAttribute
{
    private readonly ILogger<AbsoluteFilePath> _logger;


    public AbsoluteFilePath()
    {
        ErrorMessage = "The {0} field must be an absolute file path.";
        _logger = NullLogger<AbsoluteFilePath>.Instance;
    }

    public override bool IsValid(object? value)
    {
        if (value == null)
        {
            return true;
        }

        if (value is string filePath)
        {
            try
            {
                _logger.LogDebug("Validating absolute file path: {Path}", filePath);
                var fileUri = new Uri(filePath, UriKind.Absolute);
                return fileUri.IsAbsoluteUri;
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "{Path} is not an absolute path", filePath);
            }
        }
        
        return false;
    }
}