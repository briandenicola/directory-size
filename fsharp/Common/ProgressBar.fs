namespace DirectorySize.Common

open System

module ProgressBar =
    let clearCurrent () =
        Console.SetCursorPosition(0, Console.CursorTop)
        Console.Write("\r" + String(' ', Console.WindowWidth) + "\r")
        Console.SetCursorPosition(0, Console.CursorTop - 1)

    let report (total: int) (percent: float) =
        Console.WriteLine("Folders to Process: {0}. Completed: {1}%", total, Math.Round(percent, 0))
        clearCurrent ()
