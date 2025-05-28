namespace DirectorySize.Models;

public record DirectoryStatistics(string Path, long DirectorySize, long FileCount)
{
    public long DirectorySize { get; set; } = DirectorySize;
    public string Path { get; set; } = Path;
    public long FileCount { get; set; } = FileCount;
    public List<DirectoryStatistics> Subdirectories { get; } = [];
}

public record DirectoryErrorInfo
{
    public string? Path { get; init; }
    public string? ErrorDescription { get; init; }
}