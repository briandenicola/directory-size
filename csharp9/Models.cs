namespace DirectorySize
{
    public record DirectoryStatistics 
    {
        public long DirectorySize { get; init; }
        public string Path { get; init; }
        public long FileCount { get; init; }
    }

    public record DirectoryErrorInfo
    {
        public string Path { get; init; }
        public string ErrorDescription { get; init; }
    }
}
