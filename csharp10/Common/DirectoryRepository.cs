namespace DirectorySize;
class DirectoryRepository
{
    ConcurrentDictionary<string,DirectoryStatistics> _repository = new ConcurrentDictionary<string, DirectoryStatistics>();
    List<DirectoryErrorInfo> _errors = new List<DirectoryErrorInfo>();

    string _rootPath;
    int counter = 0;
    long runtime = 0L;
    long total_size = 0L;
    long total_count = 0L;
    
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
        (total_count, total_size) = getCurrentDirectoryFileSize(_rootPath);
        _repository.TryAdd<string, DirectoryStatistics>(_rootPath, new( _rootPath, total_size, total_count ));
                    
        Parallel.ForEach( Directory.EnumerateDirectories(_rootPath), async (subdirectory) => {
            
            (long count, long size) = await getDirectorySize(subdirectory);
            _repository.TryAdd<string, DirectoryStatistics>(subdirectory,new( subdirectory, size, count ));
            
            lock (_repository)
            {
                counter++; total_size += size; total_count += count;
                reportProgress(counter, totalSubDirectories);
            }
        });

        watch.Stop();
        runtime = watch.ElapsedMilliseconds;
    }

    public void Print(){
        var display = new DirectoryOutput();
        display.DisplayResults(_repository, total_count, total_size, runtime, _errors.Count);
    }

    private void reportProgress(int completed, int total) 
    {
        ProgressBar.Report(total, ((double)completed / (double)total) * 100);
    }

    private (long,long) getCurrentDirectoryFileSize(string path)
    {
        var files = Directory.EnumerateFiles(path);
        return (files.Count(), files.Sum( file => new FileInfo(file).Length ));
    }

    private async Task<(long,long)> getDirectorySize(string path)
    { 
        long directory_size = 0;
        long number_of_files = 0;

        try 
        {
            (number_of_files, directory_size) = getCurrentDirectoryFileSize(path);
            
            foreach (var subdirectory in Directory.EnumerateDirectories(path)) 
            {
                (long count, long size) = await getDirectorySize(subdirectory);
                number_of_files += count; directory_size += size; 
            }
        }
        catch (System.Exception e) 
        { 
            lock (_errors)
            {
                _errors.Add(new DirectoryErrorInfo(){ Path = path, ErrorDescription = e.Message.ToString() });
            }
        }
        
        return (number_of_files, directory_size);
    }
}