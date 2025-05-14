using System.CommandLine;
using System.CommandLine.Invocation;

var directoryOption = new Option<DirectoryInfo>(
    aliases: new[] { "--path", "-p" },
    description: "The folder path to check the size of"
)
{
    IsRequired = true
};

var rootCommand = new RootCommand("A demo app to show the size of all sub-folders under a directory")
{
    directoryOption
};

rootCommand.SetHandler((DirectoryInfo? path) =>
{
    if (path is null || !path.Exists)
    {
        Console.Error.WriteLine("Please provide a valid directory path.");
        return;
    }

    var repo = new DirectoryRepository(path.FullName);
    repo.Run();
    repo.Print();
}, directoryOption);

return await rootCommand.InvokeAsync(args.Length == 0 ? new[] { "--help" } : args);