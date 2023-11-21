using CliWrap;
using CliWrap.Buffered;
using Microsoft.Extensions.Logging.Abstractions;

namespace Pacman.NET.Utilities;

public static class CliUtilities
{
    private static ILogger Logger { get; set; } = new NullLogger<Program>();
    
    public static async Task<string> ExecuteBufferedWithMetrics(this Command command)
    {
        var result = await command.ExecuteBufferedAsync();
        result.LogOutput();
        
        Logger.LogInformation("Command executed in {Time}", result.RunTime);
        Logger.LogInformation("Command finished execution at {Time}", result.ExitTime);

        return result.StandardOutput;
    }
    public static void LogOutput(this BufferedCommandResult result)
    {
        if(result.StandardOutput.IsNotEmpty())
            Logger.LogInformation("{Out}", result.StandardOutput);
        if (result.StandardError.IsNotEmpty())
            Logger.LogWarning("{Error}", result.StandardError);
    }
    public static bool IsNotEmpty(this string s) => !string.IsNullOrEmpty(s);

    public static string GetLocalFolder()
    {
        var localData = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles, Environment.SpecialFolderOption.Create);
        if(Directory.Exists(localData))
            return localData;
        
        var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userFolder, "share", "pkgs");
    }
    public static string GetDevelopmentLocalFolder()
    {
        var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var localDir = Path.Combine(userFolder, ".local", "pkgs");
        var dir = Directory.CreateDirectory(localDir);
        return dir.FullName;
    }
}
