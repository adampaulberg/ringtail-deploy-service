using Deployer.App;
using Deployer.Util;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deployer
{
    class Program
    {
        static int Main(string[] args)
        {
            int exit = 0;
            Logger log = new Logger();
            var options = new Options();
            log.AddToLog(" Deployer............. " + DateTime.Now);

            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                try
                {
                    exit = DeploymentRunner.ReadFromFile(log, options);
                }
                catch (Exception ex)
                {
                    log.AddToLog(ex.Message);
                    log.AddToLog(ex.StackTrace);
                    exit = 1;
                }
            }

            log.Write("DeployLog.txt");

            return exit;
        }
    }



}
