var directoryOption = new Option<DirectoryInfo>("--path", "-p")
{
    Description = "The root folder path to check the size of",
    Required = true
};

RootCommand rootCommand = new("An application that displays the size of all subfolders under a directory")
{
    directoryOption
};

rootCommand.SetAction(parseResult =>
{
    var path = parseResult.GetValue(directoryOption);
    if (path is null || !path.Exists)
    {
        Console.Error.WriteLine("Please provide a valid directory path.");
        return;
    }

    var repo = new DirectoryRepository(path.FullName);
    repo.Analyze();
    repo.Display();
});

var parseResult = rootCommand.Parse(args.Length == 0 ? ["--help"] : args);
return await parseResult.InvokeAsync();
