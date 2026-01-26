namespace DirectorySize;

using DirectorySize.Common;
using DirectorySize.Models;

public static class DirectoryOutput
{
    private const string DirectoryTitle = "Directory";
    private const string CountTitle = "Count";
    private const string SizeTitle = "Size";
    private const string ModifiedTitle = "Modified";
    private const int DirectoryColumnWidth = 56;
    private const int CountColumnWidth = 12;
    private const int SizeColumnWidth = 10;
    private const int ModifiedColumnWidth = 9;
    private const int PageSize = 20;
    private const string FilterChoicePath = "__filter__";
    private const string SortChoicePath = "__sort__";

    public static void DisplayTable(DirectoryStatistics stats)
    {
        var stack = new Stack<DirectoryStatistics>();
        stack.Push(stats);
        var labelCache = new Dictionary<string, (string Label, long Bucket)>(StringComparer.OrdinalIgnoreCase);
        var topFilesCache = new Dictionary<string, List<(string Name, long Size)>>(StringComparer.OrdinalIgnoreCase);
        string? lastPath = null;
        var sortMode = SortMode.SizeDesc;
        var filterText = string.Empty;

        var headerLine =
            $"[bold]{DirectoryTitle,-DirectoryColumnWidth}[/] | [bold]{CountTitle,CountColumnWidth}[/] | [bold]{SizeTitle,SizeColumnWidth}[/] | [bold]{ModifiedTitle,ModifiedColumnWidth}[/]\n" +
            $"[grey]{new string('─', DirectoryColumnWidth)}[/] | [grey]{new string('─', CountColumnWidth)}[/] | [grey]{new string('─', SizeColumnWidth)}[/] | [grey]{new string('─', ModifiedColumnWidth)}[/]\n";

        while (stack.Count > 0)
        {
            var current = stack.Peek();
            var menu = new List<MenuChoice>();
            var now = DateTime.Now;
            var bucket = now.Ticks / TimeSpan.FromMinutes(1).Ticks;

            if (!string.Equals(lastPath, current.Path, StringComparison.Ordinal))
                AnsiConsole.Clear();

            AnsiConsole.MarkupLine($"[bold yellow]Directory:[/] [blue]{Utils.EscapeMarkup(current.Path)}[/]");
            AnsiConsole.MarkupLine(
                $"[grey]Total:[/] [green]{Utils.ToNumberFormat(current.FileCount)} files[/]  " +
                $"[grey]Size:[/] [green]{Utils.ToMB(current.DirectorySize)}[/]\n");
            AnsiConsole.MarkupLine($"[grey]Path:[/] {GetBreadcrumbs(stack)}\n");
            RenderTopFilesIfLeaf(current);

            if (stack.Count > 1)
                menu.Add(MenuChoice.Up);

            var subDirectories = ApplySortAndFilter(current.Subdirectories, sortMode, filterText);

            var sortLabel = GetSortLabel(sortMode);
            var filterLabel = string.IsNullOrWhiteSpace(filterText) ? "Off" : Utils.EscapeMarkup(filterText);

            menu.Add(new MenuChoice(SortChoicePath, $"[yellow]Sort:[/] {Utils.EscapeMarkup(sortLabel)}"));
            menu.Add(new MenuChoice(FilterChoicePath, $"[yellow]Filter:[/] {filterLabel}"));

            foreach (var subdirectory in subDirectories)
            {
                var label = GetLabel(subdirectory, now, bucket);
                menu.Add(new MenuChoice(subdirectory.Path, label));
            }
            
            menu.Add(MenuChoice.Exit);

            var dynamicTitle =
                headerLine +
                $"[grey]Items:[/] {subDirectories.Count}/{current.Subdirectories.Count}  " +
                $"[grey]Page size:[/] {PageSize}  " +
                $"[grey]Sort:[/] {Utils.EscapeMarkup(sortLabel)}  " +
                $"[grey]Filter:[/] {filterLabel}\n";

            var prompt = new SelectionPrompt<MenuChoice>()
                .Title(dynamicTitle)
                .PageSize(PageSize)
                .MoreChoicesText("[grey](Move up and down to reveal more subdirectories...)[/]")
                .UseConverter(c => c.Label)
                .AddChoices(menu);

            var selection = AnsiConsole.Prompt(prompt);

            if (selection == MenuChoice.Exit)
                break;
            else if (selection == MenuChoice.Up)
                stack.Pop();
            else if (selection.Path == SortChoicePath)
                sortMode = NextSortMode(sortMode);
            else if (selection.Path == FilterChoicePath)
                filterText = PromptForFilter(filterText);
            else
            {
                var selectedDirectory = subDirectories
                    .FirstOrDefault(s => string.Equals(s.Path, selection.Path, StringComparison.OrdinalIgnoreCase));
                
                if (selectedDirectory is not null)
                    stack.Push(selectedDirectory);
            }

            lastPath = current.Path;
        }

        string GetLabel(DirectoryStatistics directory, DateTime now, long bucket)
        {
            if (labelCache.TryGetValue(directory.Path, out var cached) && cached.Bucket == bucket)
                return cached.Label;

            var label = FormatDirectoryLabel(directory, now);
            labelCache[directory.Path] = (label, bucket);
            return label;
        }

        static string GetBreadcrumbs(Stack<DirectoryStatistics> pathStack)
        {
            var items = pathStack.ToArray();
            Array.Reverse(items);
            var segments = items.Select(s => Utils.EscapeMarkup(Utils.TrimPath(s.Path)));
            return string.Join(" [grey]>[/] ", segments);
        }

        static string PromptForFilter(string currentFilter)
        {
            var prompt = new TextPrompt<string>("[yellow]Filter (substring, empty to clear):[/]")
                .AllowEmpty()
                .DefaultValue(currentFilter);

            return AnsiConsole.Prompt(prompt).Trim();
        }

        void RenderTopFilesIfLeaf(DirectoryStatistics current)
        {
            if (current.Subdirectories.Count != 0)
                return;

            var topFiles = GetTopFiles(current.Path);
            if (topFiles.Count == 0)
                return;

            AnsiConsole.MarkupLine("[grey]Top files:[/]");
            foreach (var (name, size) in topFiles)
            {
                var label = $"{Utils.EscapeMarkup(Utils.TrimPath(name)),-DirectoryColumnWidth} | " +
                            $"[green]{Utils.ToMB(size),SizeColumnWidth}[/]";
                AnsiConsole.MarkupLine(label);
            }

            AnsiConsole.WriteLine();
        }

        List<(string Name, long Size)> GetTopFiles(string path)
        {
            if (topFilesCache.TryGetValue(path, out var cached))
                return cached;

            var results = new List<(string Name, long Size)>(5);

            try
            {
                foreach (var file in Directory.EnumerateFiles(path))
                {
                    try
                    {
                        var info = new FileInfo(file);
                        results.Add((info.Name, info.Length));
                    }
                    catch { }
                }
            }
            catch { }

            results = results
                .OrderByDescending(f => f.Size)
                .Take(5)
                .ToList();

            topFilesCache[path] = results;
            return results;
        }
    }

    private static string FormatDirectoryLabel(DirectoryStatistics stats, DateTime now) =>
        $"[white]{Utils.EscapeMarkup(Utils.TrimPath(stats.Path)),-DirectoryColumnWidth}[/] | " +
        $"[cyan]{Utils.ToNumberFormat(stats.FileCount),CountColumnWidth}[/] | " +
        $"[green]{Utils.ToMB(stats.DirectorySize),SizeColumnWidth}[/] | " +
        $"[magenta]{Utils.ToRelativeTime(stats.LastModified, now),ModifiedColumnWidth}[/]";

    private static List<DirectoryStatistics> ApplySortAndFilter(
        HashSet<DirectoryStatistics> directories,
        SortMode sortMode,
        string filterText)
    {
        IEnumerable<DirectoryStatistics> query = directories;

        if (!string.IsNullOrWhiteSpace(filterText))
            query = query.Where(d => d.Path.Contains(filterText, StringComparison.OrdinalIgnoreCase));

        return sortMode switch
        {
            SortMode.NameAsc => query.OrderBy(d => d.Path, StringComparer.OrdinalIgnoreCase).ToList(),
            SortMode.NameDesc => query.OrderByDescending(d => d.Path, StringComparer.OrdinalIgnoreCase).ToList(),
            SortMode.CountAsc => query.OrderBy(d => d.FileCount).ToList(),
            SortMode.CountDesc => query.OrderByDescending(d => d.FileCount).ToList(),
            SortMode.SizeAsc => query.OrderBy(d => d.DirectorySize).ToList(),
            _ => query.OrderByDescending(d => d.DirectorySize).ToList(),
        };
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

    private static string GetSortLabel(SortMode mode) => mode switch
    {
        SortMode.SizeDesc => "Size ↓",
        SortMode.SizeAsc => "Size ↑",
        SortMode.CountDesc => "Count ↓",
        SortMode.CountAsc => "Count ↑",
        SortMode.NameDesc => "Name ↓",
        _ => "Name ↑"
    };

    private static SortMode NextSortMode(SortMode mode) => mode switch
    {
        SortMode.SizeDesc => SortMode.SizeAsc,
        SortMode.SizeAsc => SortMode.CountDesc,
        SortMode.CountDesc => SortMode.CountAsc,
        SortMode.CountAsc => SortMode.NameDesc,
        SortMode.NameDesc => SortMode.NameAsc,
        _ => SortMode.SizeDesc
    };
}
