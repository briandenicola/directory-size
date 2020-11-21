namespace DirectorySize
{
    public record DirectoryStatistics 
    {
        public long DirectorySize { get; init; }
        public string Path { get; init; }
        public long FileCount { get; init; }

        public DirectoryStatistics( string _path, long _directorySize, long _fileCount) {
            DirectorySize = _directorySize;
            FileCount = _fileCount;
            Path = _path;
        }
    }

    public record DirectoryErrorInfo
    {
        public string Path { get; init; }
        public string ErrorDescription { get; init; }
    }
}
