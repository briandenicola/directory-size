using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DirectorySize
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1) {
                Console.WriteLine("You must provide a directory argument at the command line.");
                return;
            }

            var repo = new DirectoryRepository(args[0].ToString());
            repo.Traverse();
            repo.Print();
            Console.WriteLine("Press <ENTER> to continue");
            Console.Read();  
        }
    }
}
