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

        private string Truncate(string value, int maxChars) => value.Length <= maxChars ? value : "..." + value.Substring((value.Length-maxChars), maxChars);

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

        public void Print(bool displayErrors, bool quiet) 
        {
            int c = 0;

            void Pause()
            {
                if( !quiet && c == Console.WindowHeight - (Console.WindowHeight/2) ) 
                {
                    Console.WriteLine("Press Enter to Continue");
                    Console.ReadKey(true);
                    c = 0;
                }
            }

            Console.WriteLine(Environment.NewLine);
            Console.WriteLine("{0}{1}{2}", "Directory".PadRight(PADDING), "Number of Files".PadRight(PADDING), " Size (MB)".PadRight(PADDING));  

            foreach (var directory in _repository.OrderByDescending(o => o.DirectorySize)) 
            {
                Pause();
                Console.WriteLine(
                    "{0}{1,15:n0}{2}{3,10:##,###.##}", 
                    Truncate(directory.Path, MAXCHAR).PadRight(PADDING), 
                    directory.FileCount, 
                    "".PadRight(PADDING-15),
                    Math.Round(((double)directory.DirectorySize / MB), 2 )
                );
                c++;
            }
            Console.WriteLine();

            Console.WriteLine(
                "{0}{1,15:n0}{2}{3,10:##,###.##} ", 
                "Totals:".PadRight(PADDING), 
                total_count,
                "".PadRight(PADDING-15),
                Math.Round((double) total_size / MB), 2 );
            
            Console.WriteLine(
                "{0}{1,15:n0}(ms) ",
                "Total Time Taken:".PadRight(PADDING), 
                runtime);
            
            Console.WriteLine(
                "{0}{1,15:n0} ",
                "Total Errors:".PadRight(PADDING), 
                _errors.Count()
            );
            Console.WriteLine();

            if(displayErrors && _errors.Count > 0)
            {
                Console.WriteLine("{0}{1}", "Directory".PadRight(PADDING), "Error".PadRight(PADDING));  

                c = 0;
                foreach( var error in _errors ) 
                {
                    Pause();
                    Console.WriteLine(
                        "{0}{1}", 
                        Truncate(error.Path, MAXCHAR).PadRight(PADDING), 
                        error.ErrorDescription
                    );

                    c++;
                }
                Console.WriteLine();
            }
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
