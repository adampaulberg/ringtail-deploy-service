using MasterRunner.App.Runners;
using MasterRunner.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterRunner.App
{
    public class RunnerFactory
    {
        public static IRunner MakeRunner(string logFile, string fileName, string workingFolder, string username, string password, int defaultTimeout)
        {
            Logger logger = new Logger();
            logger.fileName = logFile;

            
            if (fileName.StartsWith("rem") || fileName.StartsWith("@") || fileName.StartsWith("--"))
            {
                Console.WriteLine("Spawning: NoOp: " + fileName);
                return new NoOp();
            }
            if (fileName.Contains(".bat"))
            {
                Console.WriteLine("Spawning: BatchFileRunner: " + fileName);
                return new BatchFileRunner(logger, SimpleFileReader.Read(fileName), workingFolder, username, password, defaultTimeout);
            }
            if (fileName.Contains(".exe"))
            {
                Console.WriteLine("Spawning: ExeFileRunner: " + fileName);
                return new ExeFileRunner(logger, fileName, workingFolder, username, password);
            }

            if(fileName.Contains(".ps1"))
            {
                Console.WriteLine("Spawning: PowerShellFileRunner: " + fileName);
                return new PowerShellFileRunner(logger, fileName, workingFolder, username, password);
            }

            return new NoOp();
        }
    }
}
