namespace DirectorySize;
public class DirectoryOutput
{
    const double MB = 1048576.0;
    Table output = new Table();
    TableColumn pathColumn= new TableColumn("Path");
    TableColumn countColumn = new TableColumn("Files");
    TableColumn sizeColumn = new TableColumn("Size (MB)");
    public DirectoryOutput() 
    {
        output.Border(TableBorder.DoubleEdge);
        //output.Border(TableBorder.Rounded);
        output.Centered();
        output.Width(System.Console.WindowWidth);

        output.AddColumn(pathColumn);
        output.AddColumn(countColumn);
        output.AddColumn(sizeColumn);
    }
    
    private static string ToNumberFormat(long val) => string.Format("{0:#,0}", val);
    private static string ToMB(long val) => string.Format("{0:#,0.00}", (double) Math.Round((double) val / MB, 2));
    
    public void DisplayResults( ConcurrentDictionary<string,DirectoryStatistics> repo, long count, long size, long time, int errors) 
    {   
        System.Console.WriteLine();

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
    }
}