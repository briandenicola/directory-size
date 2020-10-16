using System;
using System.Linq;
using System.Collections.Generic;

namespace DirectorySize
{
    static class DirectoryOutput
    {
        const int MB = 1048576;
        const int PADDING = 60;
        const int MAXCHAR = 50;

        static private string Truncate(string value, int maxChars) => value.Length <= maxChars ? value : "..." + value.Substring((value.Length-maxChars), maxChars);

        static private void writeDisplayHeader() => Console.WriteLine("{0}{1}{2}", "Directory".PadRight(PADDING), "Number of Files".PadRight(PADDING), " Size (MB)".PadRight(PADDING));  
        static private void writeErrorHeader() => Console.WriteLine("{0}{1}", "Directory".PadRight(PADDING+13), "Error".PadRight(PADDING));  

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

        static public void DisplayResults( List<DirectoryStatistics> repo, long count, long size, long time,  bool quiet) 
        {
            Console.WriteLine(Environment.NewLine);
        
            int c = 0;
            foreach (var directory in repo.OrderByDescending(o => o.DirectorySize)) 
            {
                if( c == 0 ) 
                    writeDisplayHeader();

                Console.WriteLine(
                    "{0}{1,15:n0}{2}{3,10:##,###.##}", 
                    Truncate(directory.Path, MAXCHAR).PadRight(PADDING), 
                    directory.FileCount, 
                    "".PadRight(PADDING-15),
                    Math.Round(((double)directory.DirectorySize / MB), 2 )
                );

                c = pause(c, quiet) ? 0 : (c+1);
                
            }
            Console.WriteLine();

            Console.WriteLine(
                "{0}{1,15:n0}{2}{3,10:##,###.##} ", 
                "Totals:".PadRight(PADDING), 
                count,
                "".PadRight(PADDING-15),
                Math.Round((double) size / MB), 2 
            );
            
            Console.WriteLine(
                "{0}{1,15:n0}(ms) ",
                "Total Time Taken:".PadRight(PADDING), 
                time
            );
        }

        static public void DisplayErrors( List<DirectoryErrorInfo> errors, bool quiet) 
        {               
            Console.WriteLine();
            Console.WriteLine(
                "{0}{1,15:n0} ",
                "Total Errors:".PadRight(PADDING), 
                errors.Count()
            );

            if(errors.Count > 0)
            {
                int c = 0;
                foreach( var error in errors ) 
                {
                    if( c == 0 ) 
                        writeErrorHeader();

                    Console.WriteLine(
                        "{0}{1}", 
                        Truncate(error.Path, MAXCHAR).PadRight(PADDING+13), 
                        error.ErrorDescription
                    );
                    
                    c = pause(c, quiet) ? 0 : (c+1);
                }
                Console.WriteLine();
            }
        }

    }
}