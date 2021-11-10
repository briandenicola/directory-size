﻿using System.IO;
using System.CommandLine;
using System.CommandLine.Invocation;
using DirectorySize;

if(args.Length == 0 ) 
    args = new string[1] { "-h" };

var rootCommand = new RootCommand
{
    new Option<DirectoryInfo>(new [] {"--path", "-p"},description: "The folder path to check size of"),
    new Option<bool>(new [] {"--quiet", "-q"},description: "Quiet output"),
    new Option<bool>("--show-errors", description: "An option whose argument is parsed as a FileInfo")
};

rootCommand.Description = "A console app to show the size of all subfolders under itself";
rootCommand.Handler = CommandHandler.Create<DirectoryInfo, bool, bool>( (path, quiet, showErrors) =>
{   
    var repo = new DirectoryRepository(path.FullName);
    repo.Run();
    repo.Print(showErrors, quiet);
});

return rootCommand.InvokeAsync(args).Result;