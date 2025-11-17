var directoryOption = new Option<DirectoryInfo>(
    aliases: ["--path", "-p"],
    description: "The folder path to check the size of"
)
{
    IsRequired = true
};

RootCommand rootCommand = new("A demo app to show the size of all sub-folders under a directory")
{
    directoryOption
};

rootCommand.SetHandler(path =>
{
    if (path is null || !path.Exists)
    {
        Console.Error.WriteLine("Please provide a valid directory path.");
        return;
    }

    var repo = new DirectoryRepository(path.FullName);
    repo.Analyze();
    repo.Display();
}, directoryOption);

return await rootCommand.InvokeAsync(args.Length == 0 ? ["--help"] : args);