using Microsoft.AspNetCore.SignalR;

namespace Pacman.NET;

public class PackageHub : Hub
{
    private readonly IPacmanService _pacmanService;

    public PackageHub(IPacmanService pacmanService)
    {
        _pacmanService = pacmanService;
    }
    public async IAsyncEnumerable<string> UploadPackage(IFormFile formFile)
    {
        var packageResponse = _pacmanService.AddPackage(formFile.OpenReadStream());
        while (!packageResponse.Output.EndOfStream)
        {
            var output = await packageResponse.Output.ReadLineAsync();
            
            //_logger.LogDebug("{Output}");
            yield return output ?? string.Empty;
            
        }
        
        
    }
}