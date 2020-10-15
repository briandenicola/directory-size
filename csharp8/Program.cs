using System;
using System.Threading.Tasks;

namespace DirectorySize
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length != 1) {
                Console.WriteLine("You must provide a directory argument at the command line.");
                System.Environment.Exit(-1);  
            }

            var repo = new DirectoryRepository(args[0].ToString());
            await repo.Run();
            repo.Print();
        }
    }
}
