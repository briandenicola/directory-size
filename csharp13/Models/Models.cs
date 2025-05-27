namespace DirectorySize.Models;

public record DirectoryStatistics
{
    public long DirectorySize { get; set; } = 0L;
    public string Path { get; set; } = string.Empty;
    public long FileCount { get; set; } = 0L;
    public List<DirectoryStatistics> Subdirectories { get; } = new();

    public DirectoryStatistics(string _path, long _directorySize, long _fileCount)
    {
        DirectorySize = _directorySize;
        FileCount = _fileCount;
        Path = _path;
    }
}

public record DirectoryErrorInfo
{
    public string? Path { get; init; }
    public string? ErrorDescription { get; init; }
}