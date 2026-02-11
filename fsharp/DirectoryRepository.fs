namespace DirectorySize

open System
open System.Collections.Generic
open System.IO
open System.Threading
open System.Threading.Tasks
open DirectorySize.Common
open DirectorySize.Models

[<AllowNullLiteral>]
type DirectoryRepository(rootPath: string) =
    let errors = List<DirectoryErrorInfo>()
    let syncRoot = obj()
    let errorLock = obj()

    let mutable counter = 0
    let mutable runtime = 0L
    let mutable totalDirectoryStats: DirectoryStatistics option = None

    do
        if not (Directory.Exists(rootPath)) then
            raise (DirectoryNotFoundException(rootPath))

    let computePercentage (completed: int) (total: int) =
        float completed / float total * 100.0

    let reportProgress (completed: int) (total: int) =
        ProgressBar.report total (computePercentage completed total)

    let createEmptyStats (path: string) =
        let stats = DirectoryStatistics()
        stats.Path <- path
        stats.DirectorySize <- 0L
        stats.FileCount <- 0L
        stats.LastModified <- DateTime.MinValue
        stats

    let getCurrentDirectoryStats (path: string) : DirectoryStatistics =
        let stats = createEmptyStats path

        try
            let mutable totalSize = 0L
            let mutable fileCount = 0

            let options = EnumerationOptions(
                RecurseSubdirectories = false,
                MatchType = MatchType.Simple,
                AttributesToSkip = FileAttributes.System
            )

            for file in Directory.EnumerateFileSystemEntries(path, "*", options) do
                try
                    totalSize <- totalSize + FileInfo(file).Length
                    fileCount <- fileCount + 1
                with
                | _ -> ()

            stats.DirectorySize <- totalSize
            stats.FileCount <- int64 fileCount
            stats.LastModified <- Directory.GetLastWriteTime(path)
        with
        | _ -> ()

        stats

    let rec getDirectorySize (path: string) : DirectoryStatistics =
        let currentDirectoryStats = getCurrentDirectoryStats path

        try
            for subdirectory in Directory.EnumerateDirectories(path) do
                let stats: DirectoryStatistics = getDirectorySize subdirectory
                currentDirectoryStats.FileCount <- currentDirectoryStats.FileCount + stats.FileCount
                currentDirectoryStats.DirectorySize <- currentDirectoryStats.DirectorySize + stats.DirectorySize
                currentDirectoryStats.Subdirectories.Add(stats) |> ignore
        with
        | (ex: exn) ->
            lock errorLock (fun () ->
                errors.Add({ Path = path; ErrorDescription = ex.Message })
            )

        currentDirectoryStats

    member _.Analyze() =
        let watch = Diagnostics.Stopwatch.StartNew()

        let subdirectories = Directory.EnumerateDirectories(rootPath) |> Seq.toList
        let totalDirectoryCount = subdirectories.Length
        let rootStats = getCurrentDirectoryStats rootPath
        totalDirectoryStats <- Some rootStats

        let parallelOptions = ParallelOptions(
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = CancellationToken.None
        )

        Parallel.ForEach(subdirectories, parallelOptions, fun subdirectory ->
            let subDirectoryStats = getDirectorySize subdirectory

            lock syncRoot (fun () ->
                counter <- counter + 1
                rootStats.Subdirectories.Add(subDirectoryStats) |> ignore
                rootStats.FileCount <- rootStats.FileCount + subDirectoryStats.FileCount
                rootStats.DirectorySize <- rootStats.DirectorySize + subDirectoryStats.DirectorySize
                reportProgress counter totalDirectoryCount
            )
        ) |> ignore

        watch.Stop()
        runtime <- watch.ElapsedMilliseconds

    member _.Display() =
        let stats = totalDirectoryStats |> Option.defaultValue (createEmptyStats "")
        DirectoryOutput.DisplayTable stats

    member _.GetRuntime() = runtime
