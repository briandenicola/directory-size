using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DirectorySize
{
    class DirectoryRepository
    {
        const int MAXPARALLEL = 20;

        List<DirectoryStatistics> _repository = new List<DirectoryStatistics>();
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

        public async Task Run() 
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            (total_size, total_count) = await getDirectorySize(_rootPath, false);
            _repository.Add(new DirectoryStatistics(){ Path = _rootPath, DirectorySize = total_size, FileCount = total_count});

            int totalSubDirectories = Directory.EnumerateDirectories(_rootPath).Count();

            Parallel.ForEach
            (
                Directory.EnumerateDirectories(_rootPath),
                new ParallelOptions { MaxDegreeOfParallelism = MAXPARALLEL },
                async (subdirectory) =>  
                {
                    (long size, long count) = await getDirectorySize(subdirectory, true);
                    lock (_repository)
                    {
                        _repository.Add(new DirectoryStatistics(){ Path = subdirectory, DirectorySize = size, FileCount = count});
                        counter++; total_size += size; total_count += count;
                        reportProgress(counter, totalSubDirectories);
                    }
                }
            );

            watch.Stop();
            runtime = watch.ElapsedMilliseconds;
        }

        public void Print(bool showErrors, bool quiet){
            DirectoryOutput.DisplayResults(_repository, total_count, total_size, runtime, quiet);

            if(showErrors)
                DirectoryOutput.DisplayErrors(_errors, quiet);
        }

        private void reportProgress(int completed, int total) 
        {
            ProgressBar.Report(total, ((double)completed / (double)total) * 100);
        }

        private async Task<(long,long)> getDirectorySize(string path, bool recurse)
        { 
            long directory_size = 0;
            long number_of_files = 0;

            try 
            {
                var files = Directory.EnumerateFiles(path);
                number_of_files = files.Count();
                directory_size  = files.Sum( file => new FileInfo(file).Length );
            }
            catch (System.Exception e) 
            {
                lock (_errors)
                {
                    _errors.Add(new DirectoryErrorInfo(){ Path = path, ErrorDescription = e.Message.ToString() });
                }
            }
            
            if (recurse) 
            {
                try
                {
                    foreach (var subdirectory in Directory.EnumerateDirectories(path)) 
                    {
                        (long size, long count) = await getDirectorySize(subdirectory, true);
                        directory_size += size; number_of_files += count;
                    }
                }
                catch (System.Exception e) 
                { 
                    lock (_errors)
                    {
                        _errors.Add(new DirectoryErrorInfo(){ Path = path, ErrorDescription = e.Message.ToString() });
                    }
                }
            }
            return (directory_size,number_of_files);
        }
    }
}
