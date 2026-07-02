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

        while (stack.Count > 0)
        {
            var current = stack.Peek();
            var subdirectories = current.Subdirectories
                .OrderByDescending(d => d.DirectorySize)
                .ThenBy(d => d.Path, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var topFiles = GetTopFiles(current.Path);
            var navigationEntries = BuildNavigationEntries(stack, subdirectories);
            var selectedIndex = 0;
            var action = NavigationAction.None;

            while (action == NavigationAction.None)
            {
                AnsiConsole.Clear();
                RenderBrowser(stack, current, navigationEntries, selectedIndex, topFiles);

                var key = Console.ReadKey(true).Key;
                switch (key)
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
                }
            }

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
    }

    private static void RenderBrowser(
        Stack<DirectoryStatistics> stack,
        DirectoryStatistics current,
        IReadOnlyList<NavigationEntry> navigationEntries,
        int selectedIndex,
        IReadOnlyList<(string Name, long Size)> topFiles)
    {
        var consoleWidth = GetConsoleWidth();
        var headerPanel = BuildHeaderPanel(stack, current, navigationEntries.Count);
        var directoryTable = BuildDirectoryTable(navigationEntries, selectedIndex, consoleWidth);
        var filesPanel = BuildTopFilesPanel(topFiles, consoleWidth);

        AnsiConsole.Write(headerPanel);
        AnsiConsole.WriteLine();
        AnsiConsole.Write(directoryTable);
        AnsiConsole.WriteLine();
        AnsiConsole.Write(filesPanel);
    }

    private static Panel BuildHeaderPanel(Stack<DirectoryStatistics> pathStack, DirectoryStatistics current, int navigationCount)
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
            $"[grey]Items:[/] {navigationCount}   [grey]↑/↓ move • Enter open • Esc exit[/]"
        });

        return new Panel(new Markup(summary))
            .Header("[bold]Directory Browser[/]")
            .Border(BoxBorder.Rounded)
            .Expand();
    }

    private static Table BuildDirectoryTable(IReadOnlyList<NavigationEntry> entries, int selectedIndex, int consoleWidth)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .Expand();

        table.AddColumn("[bold][/]");
        table.AddColumn("[bold]Name[/]");
        if (consoleWidth >= 90)
            table.AddColumn("[bold]Files[/]");
        table.AddColumn("[bold]Size[/]");
        if (consoleWidth >= 120)
            table.AddColumn("[bold]Modified[/]");

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

        for (var index = 0; index < entries.Count; index++)
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
        }
        else
        {
            foreach (var (name, size) in topFiles)
            {
                var displayName = Truncate(Path.GetFileName(name), Math.Clamp(consoleWidth / 3, 16, 40));
                table.AddRow(
                    $"[white]{Utils.EscapeMarkup(displayName)}[/]",
                    $"[green]{Utils.ToMB(size)}[/]");
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
        return entry.Kind switch
        {
            NavigationKind.Parent => Truncate("⬆ Parent", Math.Clamp(consoleWidth / 4, 20, 36)),
            NavigationKind.Exit => Truncate("✕ Exit", Math.Clamp(consoleWidth / 4, 20, 36)),
            _ => Truncate(GetDisplayName(entry.Path), Math.Clamp(consoleWidth / 4, 20, 36))
        };
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
        var height = Math.Max(5, Console.WindowHeight > 0 ? Console.WindowHeight : 25);
        return Math.Clamp(height / 2, 5, 12);
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
        Exit
    }
}
