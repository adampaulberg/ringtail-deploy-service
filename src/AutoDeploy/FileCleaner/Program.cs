using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;


namespace FileCleaner
{
    class Program
    {
        static int Main(string[] args)
        {               
            var options = new Options();
            if(!CommandLine.Parser.Default.ParseArguments(args, options))
            {                
                return 1;
            }

            var cleaner = new CleanerHelper(options);            
            var exitCode = cleaner.Process();
            Console.WriteLine(cleaner.WriteLog());
            cleaner.WriteOutput();

            Console.WriteLine("Exiting with code " + exitCode);
            return exitCode;
        }
    }    
}
