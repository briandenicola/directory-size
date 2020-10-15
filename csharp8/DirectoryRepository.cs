using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DirectorySize
{
    class DirectoryRepository
    {
        const int MB = 1048576;
        const int PADDING = 60;
        const int MAXPARALLEL = 20;
        const int MAXCHAR = 50;

        List<DirectoryInfo> _repository = new List<DirectoryInfo>();
        string root = string.Empty;
        long runtime = 0L;
    
        int counter = 0;
        long total_size = 0L;
        long total_count = 0L;
        
        public DirectoryRepository(string path)
        {
            root = path;
            if (!Directory.Exists(root)) {
                throw new System.IO.DirectoryNotFoundException(root);
            }
        }

        private string Truncate(string value, int maxChars) => value.Length <= maxChars ? value : "..." + value.Substring((value.Length-maxChars), maxChars);

        public async Task Run() 
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            (total_size, total_count) = await getDirectorySize(root, false);
            _repository.Add(new DirectoryInfo(){ Path = root, DirectorySize = total_size, FileCount = total_count});

            var subdirectories = Directory.EnumerateDirectories(root);
            Parallel.ForEach(
                subdirectories,
                new ParallelOptions { MaxDegreeOfParallelism = MAXPARALLEL },
                async (subdirectory) =>  {
                    (long size, long count) = await getDirectorySize(subdirectory, true);
                    lock (_repository)
                    {
                        _repository.Add(new DirectoryInfo(){ Path = subdirectory, DirectorySize = size, FileCount = count});
                        counter++; total_size += size; total_count += count;
                        reportProgress(counter, subdirectories.Count());
                    }
                }
            );

            watch.Stop();
            runtime = watch.ElapsedMilliseconds;
        }

        public void Print() 
        {
            Console.WriteLine();
            Console.WriteLine("{0}{1}{2}", "Directory".PadRight(PADDING), "Number of Files".PadRight(PADDING), " Size (MB)".PadRight(PADDING));  

            foreach (DirectoryInfo directory in _repository.OrderByDescending(o => o.DirectorySize)) {
                Console.WriteLine(
                    "{0}{1,15:n0}{2}{3,10:##,###.##}", 
                    Truncate(directory.Path, MAXCHAR).PadRight(PADDING), 
                    directory.FileCount, 
                    "".PadRight(PADDING-15),
                    Math.Round(((double)directory.DirectorySize / MB), 2 )
                );
            }

            Console.WriteLine();
            Console.WriteLine("{0}{1,15:n0}{2}{3,10:##,###.##} ", 
                "Totals:".PadRight(PADDING), 
                total_count,
                "".PadRight(PADDING-15),
                Math.Round((double) total_size / MB), 2 );
            Console.WriteLine("{0}{1,15:n0}(ms) ",
                "Total Time Taken:".PadRight(PADDING), 
                runtime);
            Console.WriteLine();
        }

        private void reportProgress(int completed, int total) {
            Console.Clear();
            var percentComplete = Math.Round((((double)completed / (double)total) * 100), 2);
            Console.WriteLine("{0} of {1} Directories Processed. Completed: {2}%", completed, total, percentComplete );
        }

        private async Task<(long,long)> getDirectorySize(string path, bool recurse)
        { 
            long directory_size = 0;
            long number_of_files = 0;

            try {
                var files = Directory.EnumerateFiles(path);
                number_of_files = files.Count();
                directory_size += files.Sum( file => new FileInfo(file).Length );
            }
            catch (System.Exception) {}
            
            if (recurse) {
                try {
                    var subdirectories = Directory.EnumerateDirectories(path);
                    foreach (string subdirectory in subdirectories) {
                        (long size, long count) = await getDirectorySize(subdirectory, true);
                        directory_size += size; number_of_files += count;
                    }
                }
                catch (System.Exception) { }
            }
            return (directory_size,number_of_files);
        }
    }
}
