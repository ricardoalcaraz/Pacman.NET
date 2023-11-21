using System.ComponentModel.DataAnnotations;

public record CliAppOptions
{
    [Required]
    public required string DbDir { get; init; }
    
    [Required]
    public required string CacheDir { get; init; }
    
    public string Gpg { get; init; }
}
