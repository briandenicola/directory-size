using System;
using System.Linq;
using System.Collections.Generic;

namespace DirectorySize
{
    static class DirectoryOutput
    {
        const int MB = 1048576;
        const int PADDING = 50;
        const int MAXCHAR = 45;

        static private string Truncate(string value, int maxChars) => value.Length <= maxChars ? value : "..." + value.Substring((value.Length-maxChars), maxChars);
        static private void   writeDisplayHeader() => Console.WriteLine($"{"Directory:",-PADDING} {"Number of Files:",PADDING} {"Size (MB):",PADDING}");
        static private void   writeErrorHeader() => Console.WriteLine($"{"Directory:",-PADDING} {"Error:",PADDING}");

        static private bool pause(int currentLine, bool quiet)
        {
            if( !quiet && currentLine == Console.WindowHeight - (Console.WindowHeight/2) ) 
            {
                Console.WriteLine("Press Enter to Continue");
                Console.ReadKey(true);
                return true;
            }
            return false;
        }

        static public void DisplayResults( List<DirectoryStatistics> repo, long count, long size, long time, int errors, bool quiet) 
        {
            Console.WriteLine(Environment.NewLine);
        
            int c = 0;
            foreach (var directory in repo.OrderByDescending(o => o.DirectorySize)) 
            {
                if( c == 0 ) 
                    writeDisplayHeader();
                Console.WriteLine($"{(Truncate(directory.Path,MAXCHAR)), -PADDING} {directory.FileCount,PADDING:n0} {((double)directory.DirectorySize/MB),PADDING:n2}" );
                c = pause(c, quiet) ? 0 : (c+1);                
            }

            Console.WriteLine();
            Console.WriteLine($"{"Totals:", -PADDING} {count,PADDING:n0} {((double) size / MB),PADDING:n2}");
            Console.WriteLine($"{"Total Time Taken (ms):", -PADDING} {time,PADDING:n0}");
            Console.WriteLine($"{"Total Errors:", -PADDING} {errors, PADDING:n0}");
        }

        static public void DisplayErrors( List<DirectoryErrorInfo> errors, bool quiet) 
        {               
            Console.WriteLine();

            if(errors.Count > 0)
            {
                int c = 0;
                foreach( var error in errors ) 
                {
                    if( c == 0 ) 
                        writeErrorHeader();
                    Console.WriteLine($"{(Truncate(error.Path, MAXCHAR)), -(PADDING+44)} {error.ErrorDescription}");                  
                    c = pause(c, quiet) ? 0 : (c+1);
                }
                Console.WriteLine();
            }
        }

    }
}