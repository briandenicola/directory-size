using System;
using System.IO;
using System.Threading.Tasks;

namespace DirectorySize
{
    class Program
    {
        static async Task Main(DirectoryInfo path, bool showErrors = false)
        {
            if (!path.Exists) {
                Console.WriteLine($"{path} does not exist.");
                System.Environment.Exit(-1);  
            }

            var repo = new DirectoryRepository(path.FullName);
            await repo.Run();
            repo.Print(showErrors);
        }

    }
}
