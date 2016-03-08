using JobLogger.App;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobLogger
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                var options = new Options();

                if (CommandLine.Parser.Default.ParseArguments(args, options))
                {
                    Logger.WriteLog(options);
                }
                else
                {
                    // logging off.  no worries.
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unknown error with the logger.");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            return 0;   // never surrender!
        }
    }
}
