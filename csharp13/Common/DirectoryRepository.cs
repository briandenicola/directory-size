namespace DirectorySize;

class DirectoryRepository
{
    readonly string _rootPath;
    readonly ConcurrentDictionary<string,DirectoryStatistics> _repository = [];
    readonly List<DirectoryErrorInfo> _errors = [];

    int counter     = 0;
    long runtime    = 0L;
    (long total_size, long total_files) directory_stats = (0L, 0L);
    
    private double ComputePercentage(long completed, long total) => 
        ((double)completed / (double)total) * 100;
        
    private void ReportProgress(int completed, int total) =>  
        ProgressBar.Report(total, ComputePercentage(completed, total));

    private (long size, long count) GetCurrentDirectoryFileSize(string path) =>
        Directory.EnumerateFiles(path) is var files
            ? (files.Sum(file => new FileInfo(file).Length), files.Count())
            : (0L, 0L);

    public void Print() => new DirectoryOutput().DisplayResults(_repository, 
                                                                directory_stats.total_size, 
                                                                directory_stats.total_files, 
                                                                runtime);                   

    public DirectoryRepository(string rootPath)
    {
        _rootPath = rootPath;
        if (!Directory.Exists(_rootPath)) 
            throw new System.IO.DirectoryNotFoundException(_rootPath);
    }

    public void Run() 
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();

        var total_directory_count = Directory.EnumerateDirectories(_rootPath).Count();
        directory_stats = GetCurrentDirectoryFileSize(_rootPath);

        _repository.TryAdd<string, DirectoryStatistics>(_rootPath, new( _rootPath, directory_stats.total_size, directory_stats.total_files ));
                    
        Parallel.ForEach( 
            Directory.EnumerateDirectories(_rootPath), 
            async (subdirectory) => 
            {
                (long size,long count) sub_directory_stats = (0L, 0L);
                sub_directory_stats = await GetDirectorySize(subdirectory);
                _repository.TryAdd<string, DirectoryStatistics>(subdirectory,
                                                                new( subdirectory, sub_directory_stats.size, sub_directory_stats.count ));
                
                lock (_repository)
                {
                    counter++;
                    directory_stats.total_files += sub_directory_stats.count;
                    directory_stats.total_size += sub_directory_stats.size;
                    ReportProgress(counter, total_directory_count);
                }
            }
        );

        watch.Stop();
        runtime = watch.ElapsedMilliseconds;
    }

    private async Task<(long,long)> GetDirectorySize(string path)
    { 
        (long directory_size, long number_of_files) current_directory_stats = (0L, 0L);

        try 
        {
            current_directory_stats = GetCurrentDirectoryFileSize(path);
            
            foreach (var subdirectory in Directory.EnumerateDirectories(path)) 
            {
                (long size, long count) = await GetDirectorySize(subdirectory);
                current_directory_stats.number_of_files += count; 
                current_directory_stats.directory_size  += size; 
            }
        }
        catch (System.Exception e) 
        { 
            lock (_errors)
            {
                _errors.Add(new DirectoryErrorInfo(){ Path = path, ErrorDescription = e.Message.ToString() });
            }
        }
        
        return current_directory_stats;
    }
}