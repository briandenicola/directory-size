namespace DirectorySize;

public class DirectoryOutput
{
    const double MB = 1048576.0;
    readonly Table output = new()
    {
        Border = TableBorder.DoubleEdge,
        Alignment = Justify.Right,
        Width = Console.WindowWidth
    };
    readonly TableColumn pathColumn = new("Path");
    readonly TableColumn countColumn = new("Files");
    readonly TableColumn sizeColumn = new("Size (MB)");

    public DirectoryOutput()
    {
        output.AddColumn(pathColumn);
        output.AddColumn(countColumn);
        output.AddColumn(sizeColumn);
    }

    private static string ToNumberFormat(long val) => $"{val:#,0}";
    private static string ToMB(long val) => $"{Math.Round(val / MB, 2):#,0.00}";

    public void DisplayResults(ConcurrentDictionary<string, DirectoryStatistics> repo, long size, long count, long time)
    {
        Console.WriteLine();

        foreach (var directory in repo.OrderByDescending(o => o.Value.DirectorySize))
        {
            output.AddRow(
                new Text(directory.Value.Path),
                new Text(ToNumberFormat(directory.Value.FileCount)),
                new Text(ToMB(directory.Value.DirectorySize))
            );
        }

        pathColumn.Footer = new Text("Totals:");
        countColumn.Footer = new Text(ToNumberFormat(count));
        sizeColumn.Footer = new Text(ToMB(size));

        AnsiConsole.Write(output);
        Console.WriteLine($"Time Taken: {time}ms");
    }
}