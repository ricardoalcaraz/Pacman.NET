using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;

namespace Pacman.NET.Controllers;

[ApiController]
[Route("[controller]")]
public class PacmanController : ControllerBase
{
    private readonly ILogger<PacmanController> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly IOptions<RepositoryOptions> _options;
    private readonly IPacmanService _pacmanService;
    
    public PacmanController(IPacmanService pacmanService, 
        ILogger<PacmanController> logger,
        IWebHostEnvironment env,
        IOptions<RepositoryOptions> options)
    {
        _logger = logger;
        _env = env;
        _options = options;
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

    [HttpGet("/archlinux/{repo}/os/{arch}/{file}")]
    public ActionResult ServeFile(string repo, string arch, string file)
    {
        var fileInfo = _options.Value.RepositoryProvider.GetFileInfo($"/{repo}/{file}");
        if (fileInfo.Exists)
        {
            _logger.LogInformation("Looking for {Repo}", repo);
            try
            {
                var linkTarget = System.IO.File.ResolveLinkTarget(fileInfo.PhysicalPath!, true);
                if (linkTarget is FileInfo linkFileInfo)
                {
                    fileInfo = new PhysicalFileInfo(linkFileInfo);
                    _logger.LogInformation("Resolved link for {File} to {Path}", fileInfo.PhysicalPath, linkFileInfo.FullName);
                }

            }
            catch (Exception e)
            {
                _logger.LogTrace("Not a linked file");
            }
            
            return File(fileInfo.PhysicalPath!, "application/octet-stream");
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