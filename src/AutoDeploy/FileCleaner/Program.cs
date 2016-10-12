using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using FileCleaner.App;

namespace FileCleaner
{
    class Program
    {
        static int Main(string[] args)
        {               
            var options = new Options();
            if(!CommandLine.Parser.Default.ParseArguments(args, options))
            {                
                return 2;
            }

            int exitCode = 0;
            try
            {
                JobCleanupOptions opts = new JobCleanupOptions();
                opts.FilterStartsWithCriteria = options.Startswith;
                opts.FilterExtensionsCriteria = options.Extension;
                opts.Path = options.Path;
                exitCode = JobCleanupHelper.RunCleanup(opts);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Unknown exception during file clean.");
                Console.WriteLine("  Message: " + ex.Message);
                exitCode = 3;
            }
            return exitCode;
        }
    }    
}
