
if(args.Length == 0 ) 
    args = ["-h"];

var directoryOption = new Option<DirectoryInfo>(
    new [] {"--path", "-p"},
    description: "The folder path to check size of"
);

var rootCommand = new RootCommand
{
    directoryOption
};

rootCommand.Description = "A demo app to show the size of all subfolders under a directory";
rootCommand.SetHandler( (DirectoryInfo path) =>
{   
    var repo = new DirectoryRepository(path.FullName);
    repo.Run();
    repo.Print();
}, directoryOption);

return rootCommand.InvokeAsync(args).Result;