using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DirectorySize
{
    class DirectoryRepository
    {
        const int MB = 1048576;
        const int PADDING = 60;
        const int MAXPARALLEL = 20;

        List<DirectoryInfo> _repository = new List<DirectoryInfo>();
        string root = string.Empty;
        long runtime = 0L;
    
        int counter = 0;
        
        public DirectoryRepository(string path)
        {
            root = path;
            if (!Directory.Exists(root)) {
                throw new System.IO.DirectoryNotFoundException(root);
            }
        }

        public void Traverse() 
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            _repository.Add(getDirectorySize(root, false));
            var subdirectories = Directory.EnumerateDirectories(root);
            Parallel.ForEach(
                subdirectories,
                new ParallelOptions { MaxDegreeOfParallelism = MAXPARALLEL },
                subdirectory => {
                    var directoryInfo = getDirectorySize(subdirectory, true);
                    lock (_repository)
                    {
                        _repository.Add(directoryInfo);
                        counter++;
                        ReportProgress(counter, subdirectories.Count());
                    }
                }
            );

            watch.Stop();
            runtime = watch.ElapsedMilliseconds;
        }

        public void Print() 
        {
            Console.WriteLine();
            Console.WriteLine("{0}{1}{2}", "Directory".PadRight(PADDING), "Number of Files".PadRight(PADDING), "Size (MB)".PadRight(PADDING));                
            foreach (DirectoryInfo directory in _repository.OrderByDescending(o => o.DirectorySize)) {
                Console.WriteLine("{0}{1}{2,10:0.00} ", 
                    directory.Path.PadRight(PADDING), 
                    directory.FileCount.ToString().PadRight(PADDING), 
                    Math.Round(((double)directory.DirectorySize / MB), 2 )
                );
            }
            Console.WriteLine();
            Console.WriteLine("Total Time Taken {0} ms", runtime);
            Console.WriteLine();
        }

        private void ReportProgress(int completed, int total) {
            Console.Clear();
            var percentComplete = Math.Round((((double)completed / (double)total) * 100), 2);
            Console.WriteLine("{0} of {1} Directories Processed. Completed: {2}%", completed, total, percentComplete );
        }

        private DirectoryInfo getDirectorySize(string path, bool recurse)
        { 
            long directory_size = 0;
            long number_of_files = 0;

            try {

                var files = Directory.EnumerateFiles(path);
                number_of_files = files.Count();

                foreach (string file in files) {
                    directory_size += (new FileInfo(file)).Length;
                }
            }
            catch (System.Exception) {}
            
            if (recurse) {
                try {
                    var subdirectories = Directory.EnumerateDirectories(path);
                    foreach (string subdirectory in subdirectories) {
                        var subDirectoryInfo = getDirectorySize(subdirectory, true);
                        directory_size += subDirectoryInfo.DirectorySize;
                        number_of_files += subDirectoryInfo.FileCount; 
                    }
                }
                catch (System.Exception) { }
            }
            return ( new DirectoryInfo(){ Path = path, DirectorySize = directory_size, FileCount = number_of_files } );
        }
    }
}
