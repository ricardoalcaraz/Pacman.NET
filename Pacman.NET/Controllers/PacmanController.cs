using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Pacman.NET.Options;
using Pacman.NET.Services;

namespace Pacman.NET.Controllers;

[ApiController]
[Route("[controller]")]
public class PacmanController : ControllerBase
{
    private readonly ILogger<PacmanController> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly IPacmanService _pacmanService;
    private readonly PacmanOptions _pacmanOptions;
    
    public PacmanController(IPacmanService pacmanService, 
        IOptions<PacmanOptions> options, 
        ILogger<PacmanController> logger,
        IWebHostEnvironment env)
    {
        _pacmanOptions = options.Value;
        _logger = logger;
        _env = env;
        _pacmanService = pacmanService;
    }

    [HttpPost("package")]
    public async Task<ActionResult> AddPackage(IFormFile packageFile)
    {
        var fileName = packageFile.FileName;
        var filePath = Path.Combine(_env.ContentRootPath, fileName);
        _logger.LogInformation("Received {Name} {Length}kB", fileName, packageFile.Length / 1024);
        
        await using (var stream = System.IO.File.Create(filePath))
        {
            await packageFile.CopyToAsync(stream);
        }

        var output = await _pacmanService.AddToRepository(new FileInfo(filePath));
        _logger.LogInformation("{Line}", output);
        return Ok(new { packageFile.Length, Path = filePath });
    }
}