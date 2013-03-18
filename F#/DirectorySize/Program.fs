open System
open System.IO

let display ( n:string, s:int64 ) = 
    let mb = 1048576.0
    System.Console.WriteLine("{0} => \t{1,10:0.00} MBs", n.PadRight(60), ( Convert.ToDouble(s) / mb ) )

let rec size ( fi : FileSystemInfo ) = 
    match fi with 
    | :? FileInfo as fileInfo -> fileInfo.Length
    | :? DirectoryInfo as dirInfo -> dirInfo.GetFileSystemInfos() |> Seq.sumBy(fun f -> size(f))
    | _ -> 0L

[<EntryPointAttribute>]
let main args =
    try
        let source = new DirectoryInfo(args.[0])

        for directory in source.GetDirectories() do
            display( directory.FullName, size(directory) )

        let file_size = source.GetFiles() |> Seq.sumBy(fun f -> f.Length) 
        display( source.FullName, file_size )
    with
        | :? System.IndexOutOfRangeException -> printfn "Must pass a directory name"
        | :? System.IO.DirectoryNotFoundException as exn  -> printfn "Could not find the directory passed - %A" exn.Message
        | :? System.AccessViolationException as exn -> printfn "Access Denied - %A" exn.Message
        | :? System.UnauthorizedAccessException as exn -> printfn "Access Denied - %A" exn.Message
        | _ -> printfn "Unknown Exception"
    0
