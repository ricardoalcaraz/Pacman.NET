using Microsoft.AspNetCore.Mvc;

namespace Pacman.NET.Controllers;

[ApiController]
[Route("[controller]")]
public class PacmanController : ControllerBase
{
    private readonly ILogger<PacmanController> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly IPacmanService _pacmanService;
    
    public PacmanController(IPacmanService pacmanService, 
        ILogger<PacmanController> logger,
        IWebHostEnvironment env)
    {
        _logger = logger;
        _env = env;
        _pacmanService = pacmanService;
    }

    [HttpPost("package")]
    public async Task<ActionResult> AddPackage(IFormFile packageFile, [FromQuery] string packageName, [FromQuery] string architecture)
    {
        var fileName = packageFile.FileName;
        var filePath = Path.Combine(_env.ContentRootPath, fileName);
        _logger.LogInformation("Received {Name} {Length}kB", fileName, packageFile.Length / 1024);
        
        await using (var stream = System.IO.File.Create(filePath))
        {
            await packageFile.CopyToAsync(stream);
        }

        var output = _pacmanService.AddPackage(packageFile.OpenReadStream());
        _logger.LogInformation("{Line}", output);
        
        //return 201
        return Ok(new
        {
            packageFile.Length, 
            Path = filePath, 
            Output = await output.Output.ReadToEndAsync()
        });
    }

    [HttpGet("/archlinux/{repo}/{file}")]
    public ActionResult ServeFile(string repo, string file)
    {
        if (_pacmanService.TryGetFile(repo, file, out var fileStream))
        {
            _logger.LogDebug("Found {File} in {Repo}", repo, file);
            return File(fileStream, "application/octet-stream");
        }

        return NotFound();
    }
}

public record PackageRequest
{
    public IFormFile Package { get; set; }
    public string PackageName { get; set; }
    public string Architecture { get; set; }
}