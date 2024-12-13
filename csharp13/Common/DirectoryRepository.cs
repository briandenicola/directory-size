namespace DirectorySize;

class DirectoryRepository
{
    private readonly ConcurrentDictionary<string, DirectoryStatistics> _repository = new();
    private readonly List<DirectoryErrorInfo> _errors = new();
    private readonly string _rootPath;
    private int _counter = 0;
    private long _runtime = 0L;
    private (long total_size, long total_files) _directoryStatistics = (0L, 0L);

    public DirectoryRepository(string path)
    {
        _rootPath = path ?? throw new ArgumentNullException(nameof(path));
        if (!Directory.Exists(_rootPath))
        {
            throw new DirectoryNotFoundException(_rootPath);
        }
    }

    public void Run()
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();

        int totalSubDirectories = Directory.EnumerateDirectories(_rootPath).Count();
        _directoryStatistics = GetCurrentDirectoryFileSize(_rootPath);

        _repository.TryAdd(_rootPath, new DirectoryStatistics(_rootPath, _directoryStatistics.total_size, _directoryStatistics.total_files));

        Parallel.ForEach(Directory.EnumerateDirectories(_rootPath), async subdirectory =>
        {
            var subDirectoryStatistics = await GetDirectorySize(subdirectory);
            _repository.TryAdd(subdirectory, new DirectoryStatistics(subdirectory, subDirectoryStatistics.size, subDirectoryStatistics.count));

            lock (_repository)
            {
                _counter++;
                _directoryStatistics.total_files += subDirectoryStatistics.count;
                _directoryStatistics.total_size += subDirectoryStatistics.size;
                ReportProgress(_counter, totalSubDirectories);
            }
        });

        watch.Stop();
        _runtime = watch.ElapsedMilliseconds;
    }

    public void Print()
    {
        var display = new DirectoryOutput();
        display.DisplayResults(_repository, _directoryStatistics.total_size, _directoryStatistics.total_files, _runtime);
    }

    private void ReportProgress(int completed, int total)
    {
        ProgressBar.Report(total, ((double)completed / total) * 100);
    }

    private static (long size, long count) GetCurrentDirectoryFileSize(string path)
    {
        var files = Directory.EnumerateFiles(path);
        return (files.Sum(file => new FileInfo(file).Length), files.Count());
    }

    private async Task<(long size, long count)> GetDirectorySize(string path)
    {
        var currentDirectoryStatistics = (size: 0L, count: 0L);

        try
        {
            currentDirectoryStatistics = GetCurrentDirectoryFileSize(path);

            foreach (var subdirectory in Directory.EnumerateDirectories(path))
            {
                var (size, count) = await GetDirectorySize(subdirectory);
                currentDirectoryStatistics.count += count;
                currentDirectoryStatistics.size += size;
            }
        }
        catch (Exception e)
        {
            lock (_errors)
            {
                _errors.Add(new DirectoryErrorInfo { Path = path, ErrorDescription = e.Message });
            }
        }

        return currentDirectoryStatistics;
    }
}