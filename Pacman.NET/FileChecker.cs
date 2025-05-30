namespace Pacman.NET;

public static class FileChecker
{
    /// <summary>
    /// Check if a file is a code file
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <returns>Whether the file is a code file</returns>
    public static bool IsCodeFile(string filePath)
    {
        string[] codeExtensions = 
        [
            ".js", ".jsx", ".ts", ".tsx", // JavaScript/TypeScript
            ".py",                        // Python
            ".java",                      // Java
            ".c", ".cpp", ".cc", ".cxx", ".h", ".hpp", // C/C++
            ".go",                        // Go
            ".rb",                        // Ruby
            ".php",                       // PHP
            ".cs",                        // C#
            ".scala",                     // Scala
            ".swift",                     // Swift
            ".rs",                        // Rust
            ".kt", ".kts",                // Kotlin
            ".sh", ".bash",               // Shell scripts
            ".sql",                       // SQL
        ];

        var ext = Path.GetExtension(filePath);
        return codeExtensions.Contains(ext, StringComparer.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Check if the file is a video file suitable for ffmpeg
    /// </summary>
    /// <param name="filePath">file path to check extension for</param>
    /// <returns>Whether file is video or not</returns>
    public static bool IsVideoFile(string filePath)
    {
        string[] videoExtensions = [".mp4", ".mov", ".mkv", ".qsv", ".avi", ".wmv", ".ts", ".hevc"];
        var ext = Path.GetExtension(filePath);
        return videoExtensions.Contains(ext, StringComparer.InvariantCultureIgnoreCase);
    }
    
    
}