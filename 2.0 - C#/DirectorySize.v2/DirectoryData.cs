using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectorySize.v2
{
    class DirectoryData
    {
        public long Size { get; set; }
        public string Path { get; set; }
        public long Files { get; set; }

        public DirectoryData(string _path, long _size, long _files)
        {
            Size = _size;
            Path = _path;
            Files = _files; 
        }
    }
}
