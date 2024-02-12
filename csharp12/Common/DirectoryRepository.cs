namespace DirectorySize;
class DirectoryRepository
{
    ConcurrentDictionary<string,DirectoryStatistics> _repository = new ConcurrentDictionary<string, DirectoryStatistics>();
    List<DirectoryErrorInfo> _errors = new List<DirectoryErrorInfo>();

    readonly string _rootPath;
    int counter = 0;
    long runtime = 0L;
    (long total_size, long total_files) directoryStatistics = (0L, 0L);
    
    public DirectoryRepository(string path)
    {
        _rootPath = path;
        if (!Directory.Exists(_rootPath)) {
            throw new System.IO.DirectoryNotFoundException(_rootPath);
        }
    }

    public void Run() 
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();

        int totalSubDirectories = Directory.EnumerateDirectories(_rootPath).Count();
        directoryStatistics = getCurrentDirectoryFileSize(_rootPath);

        _repository.TryAdd<string, DirectoryStatistics>(_rootPath, new( _rootPath, directoryStatistics.total_size, directoryStatistics.total_files ));
                    
        Parallel.ForEach( Directory.EnumerateDirectories(_rootPath), async (subdirectory) => {
            
            (long size, long count) subDirectoryStatistics = (0L, 0L);
            subDirectoryStatistics = await getDirectorySize(subdirectory);
            _repository.TryAdd<string, DirectoryStatistics>(subdirectory,new( subdirectory, subDirectoryStatistics.size, subDirectoryStatistics.count ));
            
            lock (_repository)
            {
                counter++;
                directoryStatistics.total_files += subDirectoryStatistics.count;
                directoryStatistics.total_size += subDirectoryStatistics.size;
                reportProgress(counter, totalSubDirectories);
            }
        });

        watch.Stop();
        runtime = watch.ElapsedMilliseconds;
    }

    public void Print(){
        var display = new DirectoryOutput();
        display.DisplayResults(_repository, directoryStatistics.total_size, directoryStatistics.total_files, runtime); //, _errors.Count);
    }

    private void reportProgress(int completed, int total) 
    {
        ProgressBar.Report(total, ((double)completed / (double)total) * 100);
    }

    private (long,long) getCurrentDirectoryFileSize(string path)
    {
        var files = Directory.EnumerateFiles(path);
        return (files.Sum( file => new FileInfo(file).Length ), files.Count());
    }

    private async Task<(long,long)> getDirectorySize(string path)
    { 
        (long directory_size, long number_of_files) currentDirectoryStatistics = (0L, 0L);

        try 
        {
            currentDirectoryStatistics = getCurrentDirectoryFileSize(path);
            
            foreach (var subdirectory in Directory.EnumerateDirectories(path)) 
            {
                (long size, long count) = await getDirectorySize(subdirectory);
                currentDirectoryStatistics.number_of_files += count; 
                currentDirectoryStatistics.directory_size  += size; 
            }
        }
        catch (System.Exception e) 
        { 
            lock (_errors)
            {
                _errors.Add(new DirectoryErrorInfo(){ Path = path, ErrorDescription = e.Message.ToString() });
            }
        }
        
        return currentDirectoryStatistics;
    }
}