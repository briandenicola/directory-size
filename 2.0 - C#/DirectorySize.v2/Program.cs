using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DirectorySize.v2
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1) {
                Console.WriteLine("You must provide a directory argument at the command line.");
                return;
            }

            try { 
                DirectoryRepository repo = new DirectoryRepository(args[0].ToString());
                repo.Traverse();
                repo.Print();
            }
            catch( System.IO.DirectoryNotFoundException ex ) {
                System.Console.WriteLine("Could not find Directory - {0}", ex.Message.ToString());
            }
            catch (System.Exception ex) {
                System.Console.WriteLine("General Application Error - {0}.", ex.Message.ToString());
            }

            Console.Read();  
        }
    }
}
