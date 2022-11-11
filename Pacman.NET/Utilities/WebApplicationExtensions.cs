using System.Diagnostics;
using Microsoft.Extensions.Options;
using Pacman.NET.Options;

namespace Pacman.NET.Utilities;

public static class WebApplicationExtensions
{
    /// <summary>
    /// Create a directory from which to serve pacman files from
    /// </summary>
    /// <exception cref="ApplicationException">If unable to create directory</exception>
    public static WebApplication UsePackageFileServer(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<PacmanOptions>>().Value;
        var rootDirectory = Path.Combine(app.Environment.ContentRootPath, "repos");
        var rootDirectoryInfo = Directory.CreateDirectory(rootDirectory);
        if (rootDirectoryInfo.Exists)
        {
            foreach (var database in options.Repos)
            {
                var databaseFileName = $"{database}.tar.gz";
                var dbPath = Path.Combine(rootDirectoryInfo.FullName, database, databaseFileName);
                var dbFileInfo = new FileInfo(dbPath);
                if (dbFileInfo.Exists)
                {
                    app.Logger.LogInformation("Found existing repo for {Name}", database);
                }
                else
                {
                    var directoryInfo = Directory.CreateDirectory(dbFileInfo.Directory!.FullName);
                    var process = new Process();
                    process.StartInfo = new ProcessStartInfo
                    {
                        Arguments = databaseFileName,
                        CreateNoWindow = true,
                        FileName = "/usr/bin/repo-add",
                        RedirectStandardOutput = true,
                        UseShellExecute = true,
                        WorkingDirectory = directoryInfo.FullName
                    };
                    process.Start();
                    process.WaitForExit();
                    app.Logger.LogInformation("Created folder for {Name}", process.StandardOutput.ReadToEnd());
                    app.Logger.LogInformation("Created folder for {Name}", database);

                }
            }

            
            
        }
        else
        {
            throw new ApplicationException($"Unable to create folders at {rootDirectory}");
        }
        
        return app;
    }
}