
namespace Pacman.NET.Client.Blazor;

public record PackageRequest
{
    //public IFormFile File { get; set; }
    public string? Name { get; set; }
    public string? Architecture { get; set; }
}