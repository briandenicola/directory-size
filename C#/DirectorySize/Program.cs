// The following example calculates the size of a directory
// and its subdirectories, if any, and displays the total size
// in bytes.

using System;
using System.IO;

public class ShowDirSize
{
    public static long DirSize(DirectoryInfo d, bool r)
    {
        long Size = 0;
        // Add file sizes.
        FileInfo[] fis = d.GetFiles();
        foreach (FileInfo fi in fis)
        {
            Size += fi.Length;
        }

        //Add subdirectory sizes.
        if (r == true)
        {
            DirectoryInfo[] dis = d.GetDirectories();
            foreach (DirectoryInfo di in dis)
            {
                Size += DirSize(di,true);
            }
        }

        return (Size);
    }
    public static void Main(string[] args)
    {     
        if (args.Length != 1)
        {
            Console.WriteLine("You must provide a directory argument at the command line.");
        }
        else
        {
            DirectoryInfo x = new DirectoryInfo(args[0]);
            Console.WriteLine("{0} => \t{1,10:0.00} MBs", x.FullName.PadRight(60), (double)DirSize(x, false) / 1048576);
            DirectoryInfo y = new DirectoryInfo(args[0]);

            DirectoryInfo[] d = y.GetDirectories();

            foreach (DirectoryInfo di in d)
            {
                try
                {
                    Console.WriteLine("{0} => \t{1,10:0.00} MBs", di.FullName.PadRight(60), (double)DirSize(di, true) / 1048576);
                }
                catch (SystemException e)
                {
                    Console.WriteLine("{0}", e.Message.ToString());
                }
            }
        }
    }
}