using MasterRunner.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterRunner.App.Runners
{
    public class BatchFileRunner : IRunner
    {
        private List<string> fileContents;
        private string workingFolder;
        private Logger logger;
        private string username;
        private string password;
        private int defaultTimeout;

        public BatchFileRunner(Logger logger, List<string> fileContents, string workingFolder, string username, string password, int defaultTimeout)
        {
            this.logger = logger;
            this.fileContents = fileContents;
            this.workingFolder = workingFolder;
            this.username = username;
            this.password = password;
            this.defaultTimeout = defaultTimeout;
        }

        public int RunFile()
        {
            int exitCode = 0;
            logger.AddAndWrite("* Processing via BatchFileRunner");
            Console.WriteLine("Processing via BatchFileRunner");
            if (fileContents.Count == 0)
            {
                Console.WriteLine("WARNING: tried to process a file with no contents");
            }

            var allowedExits = SimpleFileReader.Read(workingFolder + "allowedExit.config");
            ProcessExecutorHelper helper = new ProcessExecutorHelper(logger, allowedExits, SimpleFileReader.Read("timeout.config"), defaultTimeout);


            if (allowedExits.Count == 0)
            {
                logger.AddToLog("* found no exceptions");
            }
            else
            {
                logger.AddToLog("* found the following whitelisted areas");
                allowedExits.ForEach(x => logger.AddToLog(x));
            }

            for(int i = 0; i < fileContents.Count; i++)
            {
                var x = fileContents[i];
                var friendlyNameStep = i + 1;
                var progress = "* " + friendlyNameStep + " of " + fileContents.Count;

                var command = x.Trim();
                if (!string.IsNullOrWhiteSpace(command))
                {
                    if (command.StartsWith("rem") || command.StartsWith("@") || command.StartsWith("--"))
                    {
                        Console.WriteLine(command);
                        continue;
                    }
                    var result = helper.SpawnAndLog(command, workingFolder, username, password, progress);
                    if (result != 0)
                    {
                        exitCode = result;


                        var remainingSteps = new List<string>();
                        for(int j = i; j < fileContents.Count; j++)
                        {
                            remainingSteps.Add(fileContents[j]);
                        }
                        SimpleFileWriter.Write("retry.bat", remainingSteps);
                        Console.WriteLine("************************************");
                        Console.WriteLine("   To resume from the failure point....");
                        Console.WriteLine("   Reboot this machine.");
                        Console.WriteLine("   Run: ");
                        Console.WriteLine(@"   C:\upgrade\autodeploy\MasterRunner.exe -f retry.bat");
                        Console.WriteLine("************************************");
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

}
