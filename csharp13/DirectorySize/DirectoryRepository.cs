namespace DirectorySize;

class DirectoryRepository
{
    private readonly string _rootPath;
    private readonly List<DirectoryErrorInfo> _errors = [];

    private int counter     = 0;
    private long runtime    = 0L;

    private DirectoryStatistics total_directory_stats = new(string.Empty, 0L, 0L);
    
    private double ComputePercentage(long completed, long total) => 
        ((double)completed / (double)total) * 100;
        
    private void ReportProgress(int completed, int total) =>  
        ProgressBar.Report(total, ComputePercentage(completed, total));

    private DirectoryStatistics GetCurrentDirectoryFileSize(string path) =>
        Directory.EnumerateFiles(path) is var files
            ? new DirectoryStatistics(path, files.Sum(file => new FileInfo(file).Length), files.Count())
            : new DirectoryStatistics(path, 0L, 0L);

    public void Display() => new DirectoryOutput().DisplayTable(total_directory_stats); 

    public DirectoryRepository(string rootPath)
    {
        _rootPath = rootPath;
        if (!Directory.Exists(_rootPath)) 
            throw new System.IO.DirectoryNotFoundException(_rootPath);
    }

    public void Analyze() 
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();

        var total_directory_count = Directory.EnumerateDirectories(_rootPath).Count();
        total_directory_stats = GetCurrentDirectoryFileSize(_rootPath);
                    
        Parallel.ForEach( 
            Directory.EnumerateDirectories(_rootPath), 
            async (subdirectory) => 
            {
                var sub_directory_stats = await GetDirectorySize(subdirectory);
                
                lock (total_directory_stats)
                {
                    counter++;
                    total_directory_stats.Subdirectories.Add(sub_directory_stats);
                    total_directory_stats.FileCount += sub_directory_stats.FileCount;
                    total_directory_stats.DirectorySize += sub_directory_stats.DirectorySize;
                    ReportProgress(counter, total_directory_count);
                }
            }
        );

        watch.Stop();
        runtime = watch.ElapsedMilliseconds;
    }

    private async Task<DirectoryStatistics> GetDirectorySize(string path)
    { 
        var current_directory_stats = new DirectoryStatistics(path, 0L, 0L);

        try 
        {
            current_directory_stats = GetCurrentDirectoryFileSize(path);
            
            foreach (var subdirectory in Directory.EnumerateDirectories(path)) 
            {
                var stats = await GetDirectorySize(subdirectory);
                current_directory_stats.FileCount += stats.FileCount; 
                current_directory_stats.DirectorySize  += stats.DirectorySize;
                current_directory_stats.Subdirectories.Add(stats); 
            }
        }
        catch (System.Exception e) 
        { 
            lock (_errors)
                _errors.Add(new DirectoryErrorInfo(){ Path = path, ErrorDescription = e.Message.ToString() });
        }
        
        return current_directory_stats;
    }
}