namespace DirectorySize;

using DirectorySize.Common;
using DirectorySize.Models;

public static class DirectoryOutput
{
    private const string DirectoryTitle = "Directory";
    private const string CountTitle = "Count";
    private const string SizeTitle = "Size";

    public static void DisplayTable(DirectoryStatistics stats)
    {
        var stack = new Stack<DirectoryStatistics>();
        stack.Push(stats);

        while (stack.Count > 0)
        {
            var current = stack.Peek();
            var menu = new List<MenuChoice>();

            AnsiConsole.Clear();
            AnsiConsole.MarkupLine($"[bold yellow]Directory:[/] [blue]{Utils.EscapeMarkup(current.Path)}[/]\n");

            if (stack.Count > 1)
                menu.Add(MenuChoice.Up);

            var subDirectories = current.Subdirectories
                .OrderByDescending(d => d.DirectorySize)
                .ToList();

            foreach (var subdirectory in subDirectories)
            {
                var label = FormatDirectoryLabel(subdirectory);
                menu.Add(new MenuChoice(subdirectory.Path, label));
            }
            
            menu.Add(MenuChoice.Exit);

            var prompt = new SelectionPrompt<MenuChoice>()
                .Title($"[bold]{DirectoryTitle,-52}[/] | [bold]{CountTitle,6}[/] | [bold]{SizeTitle,6}[/]\n")
                .PageSize(20)
                .MoreChoicesText("[grey](Move up and down to reveal more subdirectories...)[/]")
                .UseConverter(c => c.Label)
                .AddChoices(menu);

            var selection = AnsiConsole.Prompt(prompt);

            if (selection == MenuChoice.Exit)
                break;
            else if (selection == MenuChoice.Up)
                stack.Pop();
            else
            {
                var selectedDirectory = subDirectories
                    .FirstOrDefault(s => string.Equals(s.Path, selection.Path, StringComparison.OrdinalIgnoreCase));
                
                if (selectedDirectory is not null)
                    stack.Push(selectedDirectory);
            }
        }
    }

    private static string FormatDirectoryLabel(DirectoryStatistics stats) =>
        $"{Utils.EscapeMarkup(Utils.TrimPath(stats.Path)),-50} | {Utils.ToNumberFormat(stats.FileCount),6} |  {Utils.ToMB(stats.DirectorySize),6}";
}