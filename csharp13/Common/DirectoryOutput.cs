namespace DirectorySize;

public class DirectoryOutput
{
    const double MB = 1048576.0;
    readonly Table       output         = new();
    readonly TableColumn pathColumn     = new("Path");
    readonly TableColumn countColumn    = new("Files");
    readonly TableColumn sizeColumn     = new("Size (MB)");

    public DirectoryOutput() 
    {
        output.Border(TableBorder.DoubleEdge);
        output.Centered();
        output.Width(Console.WindowWidth);

        output.AddColumn(pathColumn);
        output.AddColumn(countColumn);
        output.AddColumn(sizeColumn);
    }
    
    private static string ToNumberFormat(long val) => string.Format("{0:#,0}", val);
    private static string ToMB(long val) => string.Format("{0:#,0.00}", (double) Math.Round((double) val / MB, 2));
    
    public void DisplayResults( ConcurrentDictionary<string,DirectoryStatistics> repo, long size, long count, long time) 
    {   
        Console.WriteLine();

        foreach (var directory in repo.OrderByDescending( o => o.Value.DirectorySize))
        {
            output.AddRow(
                new Text(directory.Value.Path),
                new Text(ToNumberFormat(directory.Value.FileCount)),
                new Text(ToMB(directory.Value.DirectorySize))
            );
        }

        pathColumn.Footer(new Text("Totals:"));
        countColumn.Footer(new Text(ToNumberFormat(count)));
        sizeColumn.Footer(new Text(ToMB(size)));

        AnsiConsole.Write(output);
        Console.WriteLine("Time Taken: {0}ms", time);
    }
}