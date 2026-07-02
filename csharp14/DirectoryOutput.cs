namespace DirectorySize;

using DirectorySize.Common;
using DirectorySize.Models;
using Spectre.Console;

public static class DirectoryOutput
{
    public static void DisplayTable(DirectoryStatistics stats)
    {
        var stack = new Stack<DirectoryStatistics>();
        stack.Push(stats);
        var sortMode = SortMode.SizeDesc;

        AnsiConsole.Cursor.Hide();
        AnsiConsole.Clear();

        try
        {
            AnsiConsole.Live(new Text(""))
                .Overflow(VerticalOverflow.Crop)
                .Start(ctx =>
                {
                    while (stack.Count > 0)
                    {
                        var current = stack.Peek();
                        var subdirectories = ApplySort(current.Subdirectories, sortMode);
                        var topFiles = GetTopFiles(current.Path);
                        var navigationEntries = BuildNavigationEntries(stack, subdirectories);
                        var selectedIndex = 0;
                        var action = NavigationAction.None;

                        while (action == NavigationAction.None)
                        {
                            var layout = RenderBrowser(stack, current, navigationEntries, selectedIndex, topFiles, sortMode);
                            ctx.UpdateTarget(layout);

                            var key = Console.ReadKey(true);
                            switch (key.Key)
                            {
                                case ConsoleKey.UpArrow:
                                    selectedIndex = Math.Max(0, selectedIndex - 1);
                                    break;
                                case ConsoleKey.DownArrow:
                                    selectedIndex = Math.Min(navigationEntries.Count - 1, selectedIndex + 1);
                                    break;
                                case ConsoleKey.Enter:
                                    action = GetAction(navigationEntries[selectedIndex]);
                                    break;
                                case ConsoleKey.Escape:
                                    action = NavigationAction.Exit;
                                    break;
                                case ConsoleKey.S:
                                    sortMode = NextSortMode(sortMode);
                                    action = NavigationAction.SortChanged;
                                    break;
                            }
                        }

                        if (action == NavigationAction.SortChanged)
                            continue;

                        if (action == NavigationAction.Exit)
                            return;

                        if (action == NavigationAction.Parent)
                        {
                            stack.Pop();
                            continue;
                        }

                        if (action == NavigationAction.Open)
                        {
                            var selectedDirectory = subdirectories.FirstOrDefault(d =>
                                string.Equals(d.Path, navigationEntries[selectedIndex].Path, StringComparison.OrdinalIgnoreCase));

                            if (selectedDirectory is not null)
                                stack.Push(selectedDirectory);
                        }
                    }
                });
        }
        finally
        {
            AnsiConsole.Cursor.Show();
            AnsiConsole.Clear();
        }
    }

    private static Rows RenderBrowser(
        Stack<DirectoryStatistics> stack,
        DirectoryStatistics current,
        IReadOnlyList<NavigationEntry> navigationEntries,
        int selectedIndex,
        IReadOnlyList<(string Name, long Size)> topFiles,
        SortMode sortMode)
    {
        var consoleWidth = GetConsoleWidth();
        var pageSize = GetPageSize();
        var headerPanel = BuildHeaderPanel(stack, current, navigationEntries.Count, sortMode);
        var directoryTable = BuildDirectoryTable(navigationEntries, selectedIndex, consoleWidth, pageSize);
        var filesPanel = BuildTopFilesPanel(topFiles, consoleWidth);

        return new Rows(
            headerPanel,
            directoryTable,
            filesPanel
        );
    }

    private static Panel BuildHeaderPanel(Stack<DirectoryStatistics> pathStack, DirectoryStatistics current, int navigationCount, SortMode sortMode)
    {
        var pathSegments = pathStack.ToArray();
        Array.Reverse(pathSegments);
        var breadcrumbs = string.Join(" [grey]>[/] ", pathSegments.Select(s =>
            $"[blue]{Utils.EscapeMarkup(GetDisplayName(s.Path))}[/]"));

        var summary = string.Join("\n", new[]
        {
            $"[bold]{Utils.EscapeMarkup(GetDisplayName(current.Path))}[/]",
            $"[grey]Files:[/] [green]{Utils.ToNumberFormat(current.FileCount)}[/]  [grey]Size:[/] [green]{Utils.ToMB(current.DirectorySize)}[/]",
            $"[grey]Modified:[/] [magenta]{FormatTimestamp(current.LastModified)}[/]",
            $"[grey]Path:[/] {breadcrumbs}",
            $"[grey]Items:[/] {navigationCount}   [grey]↑/↓ move • Enter open • Esc exit • (S)ort: {GetSortLabel(sortMode)}[/]"
        });

        return new Panel(new Markup(summary))
            .Header("[bold]Directory Browser[/]")
            .Border(BoxBorder.Rounded)
            .Expand();
    }

    private static Table BuildDirectoryTable(IReadOnlyList<NavigationEntry> entries, int selectedIndex, int consoleWidth, int pageSize)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .Expand();

        table.AddColumn(new TableColumn("[bold][/]").Width(2));
        table.AddColumn(new TableColumn("[bold]Name[/]"));
        if (consoleWidth >= 90)
            table.AddColumn(new TableColumn("[bold]Files[/]").Width(12));
        table.AddColumn(new TableColumn("[bold]Size[/]").Width(12));
        if (consoleWidth >= 120)
            table.AddColumn(new TableColumn("[bold]Modified[/]").Width(16));

        if (entries.Count == 0)
        {
            var emptyRow = new List<string> { " ", "[grey](no subdirectories)[/]" };
            if (consoleWidth >= 90)
                emptyRow.Add("-");
            emptyRow.Add("-");
            if (consoleWidth >= 120)
                emptyRow.Add("-");

            table.AddRow(emptyRow.ToArray());
            return table;
        }

        int startIndex = 0;
        int endIndex = entries.Count;

        if (entries.Count > pageSize)
        {
            startIndex = Math.Max(0, selectedIndex - (pageSize / 2));
            endIndex = startIndex + pageSize;

            if (endIndex > entries.Count)
            {
                endIndex = entries.Count;
                startIndex = Math.Max(0, endIndex - pageSize);
            }
        }

        int addedRows = 0;
        for (var index = startIndex; index < endIndex; index++)
        {
            var entry = entries[index];
            var isSelected = index == selectedIndex;
            var marker = isSelected ? "[bold blue]>[/]" : " ";
            var style = isSelected ? "[bold blue]" : "[white]";
            var name = BuildEntryName(entry, consoleWidth);
            var row = new List<string>
            {
                marker,
                $"{style}{Utils.EscapeMarkup(name)}[/]"
            };

            if (consoleWidth >= 90)
            {
                var filesText = entry.Kind == NavigationKind.Directory && entry.Stats is not null
                    ? $"{style}{Utils.ToNumberFormat(entry.Stats.FileCount)}[/]"
                    : $"{style}-[/]";
                row.Add(filesText);
            }

            var sizeText = entry.Kind == NavigationKind.Directory && entry.Stats is not null
                ? $"{style}{Utils.ToMB(entry.Stats.DirectorySize)}[/]"
                : $"{style}-[/]";
            row.Add(sizeText);

            if (consoleWidth >= 120)
            {
                var modifiedText = entry.Kind == NavigationKind.Directory && entry.Stats is not null
                    ? $"{style}{FormatTimestamp(entry.Stats.LastModified)}[/]"
                    : $"{style}-[/]";
                row.Add(modifiedText);
            }

            table.AddRow(row.ToArray());
            addedRows++;
        }

        // Pad with empty rows to strictly lock the table height
        for (var i = addedRows; i < pageSize; i++)
        {
            var blankRow = new List<string> { " ", " " };
            if (consoleWidth >= 90) blankRow.Add(" ");
            blankRow.Add(" ");
            if (consoleWidth >= 120) blankRow.Add(" ");
            table.AddRow(blankRow.ToArray());
        }

        return table;
    }

    private static Panel BuildTopFilesPanel(IReadOnlyList<(string Name, long Size)> topFiles, int consoleWidth)
    {
        var table = new Table()
            .Border(TableBorder.Simple)
            .BorderColor(Color.Grey)
            .Expand();

        table.AddColumn("[bold]File[/]");
        table.AddColumn("[bold]Size[/]");

        if (topFiles.Count == 0)
        {
            table.AddRow("[grey](no files)[/]", "-");
            for (var i = 1; i < 5; i++)
                table.AddRow(" ", " ");
        }
        else
        {
            int addedRows = 0;
            foreach (var (name, size) in topFiles)
            {
                var displayName = Truncate(Path.GetFileName(name), Math.Clamp(consoleWidth / 3, 16, 40));
                table.AddRow(
                    $"[white]{Utils.EscapeMarkup(displayName)}[/]",
                    $"[green]{Utils.ToMB(size)}[/]");
                addedRows++;
            }

            // Pad with empty rows to lock the file table height to exactly 5
            for (var i = addedRows; i < 5; i++)
            {
                table.AddRow(" ", " ");
            }
        }

        return new Panel(table)
            .Header("[bold]Largest files[/]")
            .Border(BoxBorder.Rounded)
            .Expand();
    }

    private static List<NavigationEntry> BuildNavigationEntries(
        Stack<DirectoryStatistics> pathStack,
        IReadOnlyList<DirectoryStatistics> subdirectories)
    {
        var entries = new List<NavigationEntry>();

        if (pathStack.Count > 1)
            entries.Add(new NavigationEntry("..", NavigationKind.Parent, null));

        foreach (var subdirectory in subdirectories)
            entries.Add(new NavigationEntry(subdirectory.Path, NavigationKind.Directory, subdirectory));

        entries.Add(new NavigationEntry("exit", NavigationKind.Exit, null));
        return entries;
    }

    private static NavigationAction GetAction(NavigationEntry entry) => entry.Kind switch
    {
        NavigationKind.Parent => NavigationAction.Parent,
        NavigationKind.Exit => NavigationAction.Exit,
        _ => NavigationAction.Open
    };

    private static string BuildEntryName(NavigationEntry entry, int consoleWidth)
    {
        var maxLength = Math.Clamp(consoleWidth / 4, 20, 36);
        var text = entry.Kind switch
        {
            NavigationKind.Parent => "⬆ Parent",
            NavigationKind.Exit => "✕ Exit",
            _ => GetDisplayName(entry.Path)
        };
        
        return Truncate(text, maxLength).PadRight(maxLength);
    }

    private static string FormatTimestamp(DateTime value)
    {
        if (value == DateTime.MinValue)
            return "-";

        return value.ToString("yyyy-MM-dd HH:mm");
    }

    private static int GetConsoleWidth()
    {
        try
        {
            return Console.WindowWidth > 0 ? Console.WindowWidth : 120;
        }
        catch
        {
            return 120;
        }
    }

    private static int GetPageSize()
    {
        var height = Console.WindowHeight > 0 ? Console.WindowHeight : 25;
        // Increase the margin to account for panel borders, padding, and to prevent 
        // the terminal from scrolling if we write exactly to the bottom edge.
        return Math.Max(3, height - 26);
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Length <= maxLength ? value : value[..(maxLength - 1)] + "…";
    }

    private static IReadOnlyList<(string Name, long Size)> GetTopFiles(string path)
    {
        var results = new List<(string Name, long Size)>();

        try
        {
            foreach (var file in Directory.EnumerateFiles(path))
            {
                try
                {
                    var info = new FileInfo(file);
                    results.Add((info.Name, info.Length));
                }
                catch
                {
                }
            }
        }
        catch
        {
        }

        return results
            .OrderByDescending(f => f.Size)
            .Take(5)
            .ToList();
    }

    private static IReadOnlyList<DirectoryStatistics> ApplySort(IEnumerable<DirectoryStatistics> directories, SortMode mode)
    {
        return mode switch
        {
            SortMode.SizeDesc => directories.OrderByDescending(d => d.DirectorySize).ThenBy(d => d.Path, StringComparer.OrdinalIgnoreCase).ToList(),
            SortMode.SizeAsc => directories.OrderBy(d => d.DirectorySize).ThenBy(d => d.Path, StringComparer.OrdinalIgnoreCase).ToList(),
            SortMode.CountDesc => directories.OrderByDescending(d => d.FileCount).ThenBy(d => d.Path, StringComparer.OrdinalIgnoreCase).ToList(),
            SortMode.CountAsc => directories.OrderBy(d => d.FileCount).ThenBy(d => d.Path, StringComparer.OrdinalIgnoreCase).ToList(),
            SortMode.NameDesc => directories.OrderByDescending(d => d.Path, StringComparer.OrdinalIgnoreCase).ToList(),
            SortMode.NameAsc => directories.OrderBy(d => d.Path, StringComparer.OrdinalIgnoreCase).ToList(),
            _ => directories.ToList()
        };
    }

    private static string GetSortLabel(SortMode mode) => mode switch
    {
        SortMode.SizeDesc => "Size ↓",
        SortMode.SizeAsc => "Size ↑",
        SortMode.CountDesc => "Count ↓",
        SortMode.CountAsc => "Count ↑",
        SortMode.NameDesc => "Name ↓",
        SortMode.NameAsc => "Name ↑",
        _ => ""
    };

    private static SortMode NextSortMode(SortMode mode) => mode switch
    {
        SortMode.SizeDesc => SortMode.SizeAsc,
        SortMode.SizeAsc => SortMode.CountDesc,
        SortMode.CountDesc => SortMode.CountAsc,
        SortMode.CountAsc => SortMode.NameDesc,
        SortMode.NameDesc => SortMode.NameAsc,
        SortMode.NameAsc => SortMode.SizeDesc,
        _ => SortMode.SizeDesc
    };

    private static string GetDisplayName(string path)
    {
        var trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var fileName = Path.GetFileName(trimmed);
        return string.IsNullOrWhiteSpace(fileName) ? trimmed : fileName;
    }

    private sealed record NavigationEntry(string Path, NavigationKind Kind, DirectoryStatistics? Stats);

    private enum NavigationKind
    {
        Parent,
        Directory,
        Exit
    }

    private enum NavigationAction
    {
        None,
        Parent,
        Open,
        Exit,
        SortChanged
    }

    private enum SortMode
    {
        SizeDesc,
        SizeAsc,
        CountDesc,
        CountAsc,
        NameDesc,
        NameAsc
    }
}
