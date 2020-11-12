using System;
using System.IO;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace DirectorySize
{
    class Program
    {
        static int Main(string[] args) 
        {
            var rootCommand = new RootCommand
            {
                new Option<DirectoryInfo>("--path",description: "The folder path to check size of"),
                new Option<bool>("--quiet",description: "Quiet output"),
                new Option<bool>("--show-errors", description: "An option whose argument is parsed as a FileInfo")
            };

            rootCommand.Description = "A console app to show the size of all subfolders under itself";
            rootCommand.Handler = CommandHandler.Create<DirectoryInfo, bool, bool>(async (path, quiet, showErrors) =>
            {   
                var repo = new DirectoryRepository(path.FullName);
                await repo.Run();
                repo.Print(showErrors, quiet);
            });

            return rootCommand.InvokeAsync(args).Result;
        }
    }
}
