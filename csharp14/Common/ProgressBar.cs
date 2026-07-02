namespace DirectorySize.Common;

public static class ProgressBar
{
    public static void ClearCurrent()
    {
        if (Console.IsOutputRedirected || Console.IsInputRedirected)
            return;

        try
        {
            var currentTop = Console.CursorTop;
            if (currentTop < 0)
                return;

            var width = Math.Max(1, Console.WindowWidth);
            Console.SetCursorPosition(0, currentTop);
            Console.Write("\r" + new string(' ', width) + "\r");
            Console.SetCursorPosition(0, Math.Max(currentTop - 1, 0));
        }
        catch
        {
        }
    }

    public static void Report(int total, double percent)
    {
        Console.WriteLine("Folders to Process: {0}. Completed: {1}%", total, Math.Round(percent, 0));
        ClearCurrent();
    }
}