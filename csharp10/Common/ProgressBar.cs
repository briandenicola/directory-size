namespace DirectorySize;
static class ProgressBar
{
    public static void ClearCurrent()
    {
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write("\r" + new string(' ', Console.WindowWidth) + "\r");
        Console.SetCursorPosition(0, Console.CursorTop - 1);
    }
    
    public static void Report( int total, double percent )
    {
        Console.WriteLine("Folders to Process: {0}. Completed: {1}%", total, Math.Round(percent,0));
        ClearCurrent();
    }
}