namespace DirectorySize;

using DirectorySize.Common;
using DirectorySize.Models;

class DirectoryRepository
{
    private readonly string _rootPath;
    private readonly List<DirectoryErrorInfo> _errors = [];
    private readonly object _lock = new();

    private int _counter;
    private long _runtime;
    private DirectoryStatistics? _totalDirectoryStats;

    private static double ComputePercentage(long completed, long total) =>
        completed / (double)total * 100;

    private static void ReportProgress(int completed, int total) =>
        ProgressBar.Report(total, ComputePercentage(completed, total));

    private static DirectoryStatistics GetCurrentDirectoryStats(string path)
    {
        try
        {            
            long totalSize = 0;
            int fileCount = 0;

            var options = new EnumerationOptions 
            { 
                RecurseSubdirectories   = false, 
                MatchType               = MatchType.Simple,     
                AttributesToSkip        = FileAttributes.System
            };

            foreach (var file in Directory.EnumerateFileSystemEntries(path, "*", options))
            {
                try
                {
                    totalSize += new FileInfo(file).Length;
                    fileCount++;
                }
                catch { /* Skip inaccessible files */ }
            }

            return new DirectoryStatistics
            {
                Path            = path, 
                DirectorySize   = totalSize, 
                FileCount       = fileCount,
                Subdirectories  = []
            };
        }
        catch
        {
            return new DirectoryStatistics
            {
                Path            = path, 
                DirectorySize   = 0L, 
                FileCount       = 0L,
                Subdirectories  = []
            };
        }
    }

    public void Display() => DirectoryOutput.DisplayTable(_totalDirectoryStats ?? 
        new DirectoryStatistics{
            Path            = string.Empty, 
            DirectorySize   = 0L, 
            FileCount       = 0L,
            Subdirectories  = []
        });

    public long GetRuntime() => _runtime;

    public DirectoryRepository(string rootPath)
    {
        _rootPath = rootPath;
        if (!Directory.Exists(_rootPath))
            throw new DirectoryNotFoundException(_rootPath);
    }

    public void Analyze()
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();

        var subdirectories = Directory.EnumerateDirectories(_rootPath).ToList();
        int totalDirectoryCount = subdirectories.Count;
        _totalDirectoryStats = GetCurrentDirectoryStats(_rootPath);

        var parallelOptions = new ParallelOptions 
        { 
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = CancellationToken.None
        };

        Parallel.ForEach(subdirectories, parallelOptions, subdirectory =>
        {
            var subDirectoryStats = GetDirectorySize(subdirectory);

            lock (_lock)
            {
                _counter++;
                _totalDirectoryStats!.Subdirectories.Add(subDirectoryStats);
                _totalDirectoryStats.FileCount += subDirectoryStats.FileCount;
                _totalDirectoryStats.DirectorySize += subDirectoryStats.DirectorySize;
                ReportProgress(_counter, totalDirectoryCount);
            }
        });

        watch.Stop();
        _runtime = watch.ElapsedMilliseconds;
    }

    private DirectoryStatistics GetDirectorySize(string path)
    {
        var currentDirectoryStats = new DirectoryStatistics
            {
                Path            = path, 
                DirectorySize   = 0L, 
                FileCount       = 0L,
                Subdirectories = []
            };

        try
        {
            currentDirectoryStats = GetCurrentDirectoryStats(path);

            foreach (var subdirectory in Directory.EnumerateDirectories(path))
            {
                var stats = GetDirectorySize(subdirectory);
                currentDirectoryStats.FileCount += stats.FileCount;
                currentDirectoryStats.DirectorySize += stats.DirectorySize;
                currentDirectoryStats.Subdirectories.Add(stats);
            }
        }
        catch (Exception e)
        {
            lock (_errors)
                _errors.Add(new DirectoryErrorInfo { Path = path, ErrorDescription = e.Message });
        }

        return currentDirectoryStats;
    }
}