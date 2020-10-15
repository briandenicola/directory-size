namespace DirectorySize
{
    class DirectoryStatistics 
    {
        public long DirectorySize { get; set; }
        public string Path { get; set; }
        public long FileCount { get; set; }
    }

    class DirectoryErrorInfo
    {
        public string Path { get; set; }
        public string ErrorDescription { get; set; }
    }
}
