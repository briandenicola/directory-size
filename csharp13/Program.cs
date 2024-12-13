using System.CommandLine;
using System.CommandLine.Invocation;

if (args.Length == 0)
    args = new[] { "-h" };

var directoryOption = new Option<DirectoryInfo>(
    new[] { "--path", "-p" },
    description: "The folder path to check size of"
);

var rootCommand = new RootCommand("A demo app to show the size of all subfolders under a directory")
{
    directoryOption
};

rootCommand.SetHandler((DirectoryInfo path) =>
{
    var repo = new DirectoryRepository(path.FullName);
    repo.Run();
    repo.Print();
}, directoryOption);

await rootCommand.InvokeAsync(args);