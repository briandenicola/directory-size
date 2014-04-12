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
        DirectoryInfo _root_path;

        public DirectoryRepository(string path)
        {
            _root_path = new DirectoryInfo(path);
            if (!_root_path.Exists) {
                throw new System.IO.DirectoryNotFoundException(path);
            }
        }

        public void Traverse() 
        {
            _repository.Add(_get_dir_stats(_root_path, false));

            DirectoryInfo[] subdirectories =  _root_path.GetDirectories();
            Parallel.ForEach(subdirectories, subdirectory => {
                try {
                    _repository.Add(_get_dir_stats(subdirectory, true));
                }
                catch {
                    _repository.Add(new DirectoryData(subdirectory.FullName, 0, -1));
                }
            });
        }

        public void Print() 
        {
            Console.WriteLine("{0}{1}{2}", "Directory".PadRight(60), "Number of Files".PadRight(60), "Size (MB)".PadRight(60));                
            foreach (DirectoryData directory in _repository.OrderByDescending(o => o.Size)) {
                Console.WriteLine("{0}{1}{2,10:0.00} ", directory.Path.PadRight(60), directory.Files.ToString().PadRight(60), Convert.ToDouble(directory.Size / 1048576));
            }
        }

        private DirectoryData _get_dir_stats(DirectoryInfo _path, bool recurse)
        { 
            long size_of_directory = 0;
            long number_of_files = 0;

            FileInfo[] files = _path.GetFiles();
            number_of_files = files.Count();

            foreach (FileInfo file in files) {
                size_of_directory += file.Length;
            }

            if (recurse) {
                DirectoryInfo[] subdirectories = _path.GetDirectories();
                foreach (DirectoryInfo subdirectory in subdirectories) {
                    DirectoryData tmp = _get_dir_stats(subdirectory, true);
                    size_of_directory += tmp.Size;
                    number_of_files += tmp.Files; 
                }
            }

            return (new DirectoryData(_path.FullName, size_of_directory, number_of_files));
        }
    }
}
