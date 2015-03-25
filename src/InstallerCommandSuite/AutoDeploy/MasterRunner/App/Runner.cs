using MasterRunner.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterRunner.App
{
    public interface IRunner
    {
        int RunFile();
    }

    public class RunnerFactory
    {
        public static IRunner MakeRunner(string logFile, string fileName, string workingFolder, string username, string password)
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
                return new BatchFileRunner(logger, SimpleFileReader.Read(fileName), workingFolder, username, password);
            }
            if (fileName.Contains(".exe"))
            {
                Console.WriteLine("Spawning: ExeFileRunner: " + fileName);
                return new ExeFileRunner(logger, fileName, workingFolder, username, password);
            }

            return new NoOp();
        }
    }

    public class NoOp : IRunner
    {
        public int RunFile()
        {
            return 0;
        }
    }

    public class BatchFileRunner : IRunner
    {
        private List<string> fileContents;
        private string workingFolder;
        private Logger logger;
        private string username;
        private string password;

        public BatchFileRunner(Logger logger, List<string> fileContents, string workingFolder, string username, string password)
        {
            this.logger = logger;
            this.fileContents = fileContents;
            this.workingFolder = workingFolder;
            this.username = username;
            this.password = password;
        }

        public int RunFile()
        {
            int exitCode = 0;
            Console.WriteLine("Processing via BatchFileRunner");
            if (fileContents.Count == 0)
            {
                Console.WriteLine("WARNING: tried to process a file with no contents");
            }

            var allowedExits = SimpleFileReader.Read(workingFolder + "allowedExit.config");
            ProcessExecutorHelper helper = new ProcessExecutorHelper(logger, allowedExits);


            if (allowedExits.Count == 0)
            {
                logger.AddToLog("* found no exceptions");
            }
            else
            {
                logger.AddToLog("* found the following whitelisted areas");
                allowedExits.ForEach(x => logger.AddToLog(x));
            }

            foreach (var x in fileContents)
            {
                var command = x.Trim();
                if (!string.IsNullOrWhiteSpace(command))
                {
                    if (command.StartsWith("rem") || command.StartsWith("@") || command.StartsWith("--"))
                    {
                        Console.WriteLine(command);
                        continue;
                    }
                    var result = helper.SpawnAndLog(command, workingFolder, username, password);
                    if (result != 0)
                    {
                        exitCode = result;
                        break;
                    }
                }
            }

            if (exitCode == 0)
            {
                logger.AddAndWrite("UPGRADE SUCCESSFUL");
            }
            else
            {
                logger.AddAndWrite("UPGRADE FAILED");
                logger.AddAndWrite("Exit code: " + exitCode);
            }

            return exitCode;
        }
    }

    public class ExeFileRunner : IRunner
    {
        private string filename;
        private string workingFolder;
        private Logger logger;
        private string username;
        private string password;

        public ExeFileRunner(Logger logger, string filename, string workingFolder, string username, string password)
        {
            this.logger = logger;
            this.filename = filename;
            this.workingFolder = workingFolder;
            this.username = username;
            this.password = password;
        }

        public int RunFile()
        {
            Console.WriteLine("Processing via ExeFileRunner");

            var allowedExits = SimpleFileReader.Read(workingFolder + "allowedExit.config");
            ProcessExecutorHelper helper = new ProcessExecutorHelper(logger, allowedExits);

            int exitCode = helper.SpawnAndLog(filename, workingFolder, username, password);
            if (exitCode == 0)
            {
                logger.AddAndWrite("UPGRADE SUCCESSFUL");
            }
            else
            {
                logger.AddAndWrite("UPGRADE FAILED");
                logger.AddAndWrite("Exit code: " + exitCode);
            }
            return exitCode;

        }
    }
}
