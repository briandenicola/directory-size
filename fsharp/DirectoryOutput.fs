namespace DirectorySize

open System
open System.Collections.Generic
open System.IO
open System.Linq
open Spectre.Console
open DirectorySize.Common
open DirectorySize.Models

module DirectoryOutput =
    let private DirectoryTitle = "Directory"
    let private CountTitle = "Count"
    let private SizeTitle = "Size"
    let private ModifiedTitle = "Modified"
    let private DirectoryColumnWidth = 56
    let private CountColumnWidth = 12
    let private SizeColumnWidth = 10
    let private ModifiedColumnWidth = 9
    let private PageSize = 20
    let private FilterChoicePath = "__filter__"
    let private SortChoicePath = "__sort__"

    type private SortMode =
        | SizeDesc
        | SizeAsc
        | CountDesc
        | CountAsc
        | NameDesc
        | NameAsc

    let private padRight width (value: string) = value.PadRight(width)
    let private padLeft width (value: string) = value.PadLeft(width)

    let private formatDirectoryLabel (stats: DirectoryStatistics) (now: DateTime) =
        let pathLabel = Utils.escapeMarkup(Utils.trimPath(stats.Path.AsSpan())) |> padRight DirectoryColumnWidth
        let countLabel = Utils.toNumberFormat(stats.FileCount) |> padLeft CountColumnWidth
        let sizeLabel = Utils.toMB(stats.DirectorySize) |> padLeft SizeColumnWidth
        let modifiedLabel = Utils.toRelativeTime stats.LastModified now |> padLeft ModifiedColumnWidth
        $"[white]{pathLabel}[/] | [cyan]{countLabel}[/] | [green]{sizeLabel}[/] | [magenta]{modifiedLabel}[/]"

    let private applySortAndFilter (directories: HashSet<DirectoryStatistics>) sortMode filterText =
        let query: seq<DirectoryStatistics> =
            if String.IsNullOrWhiteSpace(filterText) then
                directories :> seq<DirectoryStatistics>
            else
                directories |> Seq.filter (fun d -> d.Path.Contains(filterText, StringComparison.OrdinalIgnoreCase))

        let compareName (a: DirectoryStatistics) (b: DirectoryStatistics) =
            StringComparer.OrdinalIgnoreCase.Compare(a.Path, b.Path)

        match sortMode with
        | SortMode.NameAsc -> query |> Seq.sortWith compareName |> Seq.toList
        | SortMode.NameDesc -> query |> Seq.sortWith (fun a b -> compareName b a) |> Seq.toList
        | SortMode.CountAsc -> query |> Seq.sortBy (fun d -> d.FileCount) |> Seq.toList
        | SortMode.CountDesc -> query |> Seq.sortByDescending (fun d -> d.FileCount) |> Seq.toList
        | SortMode.SizeAsc -> query |> Seq.sortBy (fun d -> d.DirectorySize) |> Seq.toList
        | SortMode.SizeDesc -> query |> Seq.sortByDescending (fun d -> d.DirectorySize) |> Seq.toList

    let private getSortLabel mode =
        match mode with
        | SortMode.SizeDesc -> "Size ↓"
        | SortMode.SizeAsc -> "Size ↑"
        | SortMode.CountDesc -> "Count ↓"
        | SortMode.CountAsc -> "Count ↑"
        | SortMode.NameDesc -> "Name ↓"
        | SortMode.NameAsc -> "Name ↑"

    let private nextSortMode mode =
        match mode with
        | SortMode.SizeDesc -> SortMode.SizeAsc
        | SortMode.SizeAsc -> SortMode.CountDesc
        | SortMode.CountDesc -> SortMode.CountAsc
        | SortMode.CountAsc -> SortMode.NameDesc
        | SortMode.NameDesc -> SortMode.NameAsc
        | SortMode.NameAsc -> SortMode.SizeDesc

    let DisplayTable (stats: DirectoryStatistics) =
        let stack = Stack<DirectoryStatistics>()
        stack.Push(stats)

        let labelCache = Dictionary<string, (string * int64)>(StringComparer.OrdinalIgnoreCase)
        let topFilesCache = Dictionary<string, (string * int64) list>(StringComparer.OrdinalIgnoreCase)

        let mutable lastPath: string option = None
        let mutable sortMode = SortMode.SizeDesc
        let mutable filterText = ""

        let headerLine =
            "[bold]" + (padRight DirectoryColumnWidth DirectoryTitle) + "[/] | " +
            "[bold]" + (padLeft CountColumnWidth CountTitle) + "[/] | " +
            "[bold]" + (padLeft SizeColumnWidth SizeTitle) + "[/] | " +
            "[bold]" + (padLeft ModifiedColumnWidth ModifiedTitle) + "[/]\n" +
            "[grey]" + String('-', DirectoryColumnWidth) + "[/] | " +
            "[grey]" + String('-', CountColumnWidth) + "[/] | " +
            "[grey]" + String('-', SizeColumnWidth) + "[/] | " +
            "[grey]" + String('-', ModifiedColumnWidth) + "[/]\n"

        let getLabel (directory: DirectoryStatistics) (now: DateTime) (bucket: int64) =
            match labelCache.TryGetValue(directory.Path) with
            | true, cached when snd cached = bucket -> fst cached
            | _ ->
                let label = formatDirectoryLabel directory now
                labelCache[directory.Path] <- (label, bucket)
                label

        let getBreadcrumbs (pathStack: Stack<DirectoryStatistics>) =
            let items = pathStack.ToArray()
            Array.Reverse(items)
            items
            |> Seq.map (fun s -> Utils.escapeMarkup(Utils.trimPath(s.Path.AsSpan())))
            |> String.concat " [grey]>[/] "

        let promptForFilter currentFilter =
            let prompt =
                TextPrompt<string>("[yellow]Filter (substring, empty to clear):[/]")
                    .AllowEmpty()
                    .DefaultValue(currentFilter)
            AnsiConsole.Prompt(prompt).Trim()

        let renderTopFilesIfLeaf (current: DirectoryStatistics) =
            if current.Subdirectories.Count <> 0 then
                ()
            else
                let topFiles =
                    match topFilesCache.TryGetValue(current.Path) with
                    | true, cached -> cached
                    | _ ->
                        let mutable results = ResizeArray<string * int64>(5)
                        try
                            for file in Directory.EnumerateFiles(current.Path) do
                                try
                                    let info = FileInfo(file)
                                    results.Add(info.Name, info.Length)
                                with
                                | _ -> ()
                        with
                        | _ -> ()

                        let sorted =
                            results
                            |> Seq.sortByDescending snd
                            |> Seq.truncate 5
                            |> Seq.toList

                        topFilesCache[current.Path] <- sorted
                        sorted

                if topFiles.Length > 0 then
                    AnsiConsole.MarkupLine("[grey]Top files:[/]")
                    for (name, size) in topFiles do
                        let fileLabel = Utils.escapeMarkup(Utils.trimPath(name.AsSpan())) |> padRight DirectoryColumnWidth
                        let sizeLabel = Utils.toMB(size) |> padLeft SizeColumnWidth
                        let label =
                            $"{fileLabel} | [green]{sizeLabel}[/]"
                        AnsiConsole.MarkupLine(label)
                    AnsiConsole.WriteLine()

        while stack.Count > 0 do
            let current = stack.Peek()
            let menu = List<MenuChoice>()
            let now = DateTime.Now
            let bucket = now.Ticks / TimeSpan.FromMinutes(1.0).Ticks

            match lastPath with
            | Some path when String.Equals(path, current.Path, StringComparison.Ordinal) |> not ->
                AnsiConsole.Clear()
            | None ->
                AnsiConsole.Clear()
            | _ -> ()

            AnsiConsole.MarkupLine($"[bold yellow]Directory:[/] [blue]{Utils.escapeMarkup(current.Path)}[/]")
            AnsiConsole.MarkupLine(
                $"[grey]Total:[/] [green]{Utils.toNumberFormat(current.FileCount)} files[/]  " +
                $"[grey]Size:[/] [green]{Utils.toMB(current.DirectorySize)}[/]\n")
            AnsiConsole.MarkupLine($"[grey]Path:[/] {getBreadcrumbs stack}\n")
            renderTopFilesIfLeaf current

            if stack.Count > 1 then
                menu.Add(MenuChoice.Up)

            let subDirectories = applySortAndFilter current.Subdirectories sortMode filterText

            let sortLabel = getSortLabel sortMode
            let filterLabel = if String.IsNullOrWhiteSpace(filterText) then "Off" else Utils.escapeMarkup(filterText)

            menu.Add(MenuChoice(SortChoicePath, $"[yellow]Sort:[/] {Utils.escapeMarkup(sortLabel)}"))
            menu.Add(MenuChoice(FilterChoicePath, $"[yellow]Filter:[/] {filterLabel}"))

            for subdirectory in subDirectories do
                let label = getLabel subdirectory now bucket
                menu.Add(MenuChoice(subdirectory.Path, label))

            menu.Add(MenuChoice.Exit)

            let dynamicTitle =
                headerLine +
                $"[grey]Items:[/] {subDirectories.Length}/{current.Subdirectories.Count}  " +
                $"[grey]Page size:[/] {PageSize}  " +
                $"[grey]Sort:[/] {Utils.escapeMarkup(sortLabel)}  " +
                $"[grey]Filter:[/] {filterLabel}\n"

            let prompt =
                SelectionPrompt<MenuChoice>()
                    .Title(dynamicTitle)
                    .PageSize(PageSize)
                    .MoreChoicesText("[grey](Move up and down to reveal more subdirectories...)[/]")
                    .UseConverter(Func<MenuChoice, string>(fun c -> c.Label))
                    .AddChoices(menu)

            let selection = AnsiConsole.Prompt(prompt)

            if selection.Path = MenuChoice.Exit.Path then
                stack.Clear()
            elif selection.Path = MenuChoice.Up.Path then
                stack.Pop() |> ignore
            elif selection.Path = SortChoicePath then
                sortMode <- nextSortMode sortMode
            elif selection.Path = FilterChoicePath then
                filterText <- promptForFilter filterText
            else
                let selectedDirectory =
                    subDirectories
                    |> List.tryFind (fun s -> String.Equals(s.Path, selection.Path, StringComparison.OrdinalIgnoreCase))

                match selectedDirectory with
                | Some dir -> stack.Push(dir)
                | None -> ()

            lastPath <- Some current.Path
