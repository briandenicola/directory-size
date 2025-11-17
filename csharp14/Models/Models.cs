namespace DirectorySize.Models;

/// <summary>
/// High-performance directory statistics using stack allocation and frozen collections
/// </summary>
public class DirectoryStatistics
{
    public required string Path { get; init; }
    public required long DirectorySize { get; set; }
    public required long FileCount { get; set; }
    public required HashSet<DirectoryStatistics> Subdirectories { get; init; }

    public DirectoryStatistics()
    {
        Subdirectories = [];
    }

    public DirectoryStatistics(string path, long directorySize, long fileCount)
    {
        Path = path;
        DirectorySize = directorySize;
        FileCount = fileCount;
        Subdirectories = [];
    }
}

/// <summary>
/// Error information with frozen collection
/// </summary>
public readonly record struct DirectoryErrorInfo
{
    public required string Path { get; init; }
    public required string ErrorDescription { get; init; }
}