using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace DirectorySize
{
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
            (total_size, total_count) = getCurrentDirectoryFileSize(_rootPath);

            _repository.TryAdd<string, DirectoryStatistics>(_rootPath, 
                new DirectoryStatistics(){ Path = _rootPath, DirectorySize = total_size, FileCount = total_count});
            
            Parallel.ForEach( Directory.EnumerateDirectories(_rootPath), async (subdirectory) => {
                (long size, long count) = await getDirectorySize(subdirectory);

                _repository.TryAdd<string, DirectoryStatistics>(subdirectory, 
                    new DirectoryStatistics(){ Path = subdirectory, DirectorySize = size, FileCount = count});

                lock (_repository)
                {
                    counter++; total_size += size; total_count += count;
                    reportProgress(counter, totalSubDirectories);
                }
            });

            watch.Stop();
            runtime = watch.ElapsedMilliseconds;
        }

        public void Print(bool showErrors, bool quiet){
            DirectoryOutput.DisplayResults(_repository, total_count, total_size, runtime, _errors.Count(), quiet);

            if(showErrors)
                DirectoryOutput.DisplayErrors(_errors, quiet);
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
                (number_of_files,directory_size) = getCurrentDirectoryFileSize(path);
                
                foreach (var subdirectory in Directory.EnumerateDirectories(path)) 
                {
                    (long size, long count) = await getDirectorySize(subdirectory);
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
            return (directory_size,number_of_files);
        }
    }
}
