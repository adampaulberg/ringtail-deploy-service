using MasterRunner.App;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterRunner
{
    class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("_MasterRunner_");
            int exitCode = 0;
            var options = new Options();

            try
            {
                if (CommandLine.Parser.Default.ParseArguments(args, options))
                {
                    options.WorkingFolder = Environment.CurrentDirectory + @"\";
                    Console.WriteLine("running: " + options.WorkingFolder + options.FileName);
                    exitCode = RunnerFactory.MakeRunner(options.OutputFile, options.FileName, options.WorkingFolder, options.User, options.Password, options.Timeout).RunFile();
                }
            }
            catch
            {
                exitCode = 1;
                Console.WriteLine("Exiting with code " + exitCode);
            }

            return exitCode;
        }

    }
}
