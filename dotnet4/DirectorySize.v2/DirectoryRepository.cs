using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DirectorySize.v2
{
    class DirectoryRepository
    {
        List<DirectoryData> _repository = new List<DirectoryData>();
        string _root_path;

        public DirectoryRepository(string path)
        {
            _root_path = path;
            if (!Directory.Exists(_root_path)) {
                throw new System.IO.DirectoryNotFoundException(_root_path);
            }
        }

        public void Traverse() 
        {
            _repository.Add(_get_dir_stats(_root_path, false));
            var subdirectories = Directory.EnumerateDirectories(_root_path);
            Parallel.ForEach(subdirectories, subdirectory => { _repository.Add(_get_dir_stats(subdirectory, true)); });
        }

        public void Print() 
        {
            Console.WriteLine("{0}{1}{2}", "Directory".PadRight(60), "Number of Files".PadRight(60), "Size (MB)".PadRight(60));                
            foreach (DirectoryData directory in _repository.OrderByDescending(o => o.Size)) {
                Console.WriteLine("{0}{1}{2,10:0.00} ", directory.Path.PadRight(60), directory.Files.ToString().PadRight(60), Convert.ToDouble(directory.Size / 1048576));
            }
        }

        private DirectoryData _get_dir_stats(string _path, bool recurse)
        { 
            long size_of_directory = 0;
            long number_of_files = 0;

            try {
                var files = Directory.EnumerateFiles(_path);
                number_of_files = files.Count();

                foreach (string file in files) {
                    FileInfo fileInfo = new FileInfo(file);
                    size_of_directory += fileInfo.Length;
                }
            }
            catch (System.Exception) {}
            
            if (recurse) {
                try {
                    var subdirectories = Directory.EnumerateDirectories(_path);
                    foreach (string subdirectory in subdirectories) {
                        DirectoryData tmp = _get_dir_stats(subdirectory, true);
                        size_of_directory += tmp.Size;
                        number_of_files += tmp.Files; 
                    }
                }
                catch (System.Exception) { }
            }
            return (new DirectoryData(_path, size_of_directory, number_of_files));
        }
    }
}
