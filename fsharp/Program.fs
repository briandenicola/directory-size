open System
open System.CommandLine
open System.IO
open System.CommandLine.Parsing
open DirectorySize

[<EntryPoint>]
let main argv =
    let directoryOption: Option<DirectoryInfo> =
        Option<DirectoryInfo>("--path", [| "-p" |])
    directoryOption.Description <- "The root folder path to check the size of"
    directoryOption.Required <- true

    let rootCommand =
        RootCommand("An application that displays the size of all subfolders under a directory")
    rootCommand.Options.Add(directoryOption :> Option)

    rootCommand.SetAction(
        Action<ParseResult>(fun parseResult ->
            let path = parseResult.GetValue(directoryOption)
            match path with
            | null ->
                Console.Error.WriteLine("Please provide a valid directory path.")
            | p when not p.Exists ->
                Console.Error.WriteLine("Please provide a valid directory path.")
            | p ->
                let repo = DirectoryRepository(p.FullName)
                repo.Analyze()
                repo.Display()
        )
    )

    let parseResult = rootCommand.Parse(if argv.Length = 0 then [| "--help" |] else argv)
    parseResult.Invoke()
