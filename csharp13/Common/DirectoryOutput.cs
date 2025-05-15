namespace DirectorySize;

public class DirectoryOutput
{
    const double MB = 1048576.0;
    // readonly Table       table         = new();
    // readonly TableColumn pathColumn     = new("Path");
    // readonly TableColumn countColumn    = new("Files");
    // readonly TableColumn sizeColumn     = new("Size (MB)");

    public DirectoryOutput() 
    {
        // table.Border(TableBorder.DoubleEdge);
        // table.Centered();
        // table.Width(Console.WindowWidth);

        // table.AddColumn(pathColumn);
        // table.AddColumn(countColumn);
        // table.AddColumn(sizeColumn);
    }
    
    private static string ToNumberFormat(long val) => string.Format("{0:#,0}", val);
    private static string ToMB(long val) => string.Format("{0:#,0.00}", (double) Math.Round((double) val / MB, 2));
    
    public Table BuildTable( DirectoryStatistics stats )//, long time) 
    {   
        Table       table       = new();
        TableColumn pathColumn  = new("Path");
        TableColumn countColumn = new("Files");
        TableColumn sizeColumn  = new("Size (MB)");

        table.Border(TableBorder.DoubleEdge);
        table.Centered();
        table.Width(Console.WindowWidth);

        table.AddColumn(pathColumn);
        table.AddColumn(countColumn);
        table.AddColumn(sizeColumn);

        foreach (var directory in stats.Subdirectories.OrderByDescending( o => o.DirectorySize))
        {
            table.AddRow(
                new Text(directory.Path),
                new Text(ToNumberFormat(directory.FileCount)),
                new Text(ToMB(directory.DirectorySize))
            );
        }

        pathColumn.Footer(new Text("Totals:"));
        countColumn.Footer(new Text($"{ToNumberFormat(stats.Subdirectories.Sum(s => s.FileCount))}"));
        sizeColumn.Footer(new Text($"{ToMB(stats.Subdirectories.Sum(s => s.DirectorySize))}"));
        

        return table;        
    }

    static string EscapeMarkup(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return text.Replace("[", "[[").Replace("]", "]]");
    }

    public void NavigateInteractive(DirectoryStatistics rootStats)
    {
        var navigationStack = new Stack<DirectoryStatistics>();
        navigationStack.Push(rootStats);

        while (navigationStack.Count > 0)
        {
            var current = navigationStack.Peek();

            AnsiConsole.Clear();
            AnsiConsole.MarkupLine($"[bold yellow]Directory:[/] [blue]{EscapeMarkup(current.Path)}[/]\n");

            var table = BuildTable(current);
            AnsiConsole.Write(table);

            var choices = new List<MenuChoice>();

            if (navigationStack.Count > 1)
                choices.Add(new MenuChoice("up", EscapeMarkup("[[..] Up]")));

            choices.Add(new MenuChoice("exit", EscapeMarkup("[Exit]")));

            var subs = current.Subdirectories.OrderByDescending(d => d.DirectorySize).ToList();
            foreach (var s in subs)
            {
                var label = $"{EscapeMarkup(Path.GetFileName(s.Path)),50} | {s.FileCount,6} files |";
                choices.Add(new MenuChoice(s.Path, label));
            }

            var prompt = new SelectionPrompt<MenuChoice>()
                .Title("Select subdirectory to drill down, Up to go back, Exit to quit:")
                .PageSize(20)
                .UseConverter(c => c.Label)
                .AddChoices(choices);

            var choice = AnsiConsole.Prompt(prompt);

            if (choice.Path == "exit")
                break;

            if (choice.Path == "up")
            {
                navigationStack.Pop();
            }
            else
            {
                // Find matching subdirectory (should always find one)
                var nextDir = subs.FirstOrDefault(s => string.Equals(s.Path, choice.Path, StringComparison.OrdinalIgnoreCase));
                if (nextDir != null)
                {
                    navigationStack.Push(nextDir);
                }
            }
        }
    }
}
 


