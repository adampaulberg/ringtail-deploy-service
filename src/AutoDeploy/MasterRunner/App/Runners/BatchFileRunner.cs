using MasterRunner.Util;
using System;
using System.Collections.Generic;
using System.IO;
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
            logger.AddAndWrite("* Starting: " + DateTime.Now);
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

            bool retry = false;

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

                    Random rnd = new Random();
                    int rand = rnd.Next(1, 100);
                    if(rand <= 20)
                    {
                        result = rand;
                    }

                    if (result != 0)
                    {
                        exitCode = result;


                        var remainingSteps = new List<string>();
                        for(int j = i; j < fileContents.Count; j++)
                        {
                            remainingSteps.Add(fileContents[j]);
                        }
                        SimpleFileWriter.Write("retry.bat", remainingSteps);
                        logger.AddToLog("RETRY");
                        logger.AddToLog("************************************");
                        logger.AddToLog("   To resume from the failure point....");
                        logger.AddToLog("   Reboot this machine.");
                        logger.AddToLog("   Run: ");
                        logger.AddToLog(@"   http://IP:8080/api/retry");
                        logger.AddToLog("************************************");
                        retry = true;
                        break;
                    }
                }
            }

            if(!retry)
            {
                // remove out the retry.bat file if we either failed or succeeded so that no further retries will happen.
                FileInfo fi = new FileInfo(@"C:\upgrade\autodeploy\retry.bat");
                if(fi.Exists)
                {
                    fi.Delete();
                }
            }

            if (retry)
            {
                var log = logger.GetLog();

                var newLog = new List<string>();
                foreach (var x in log)
                {
                    if (x.StartsWith("UPGRADE SUCCESSFUL") || x.StartsWith("UPGRADE FAILED") || x.StartsWith("UPGRADE RETRY"))
                    {
                        continue;
                    }
                    newLog.Add(x);
                }

                logger.ClearLog();
                logger.AddToLog(newLog);

                logger.AddAndWrite("UPGRADE RETRY");
                return exitCode;
            }
            if (exitCode == 0)
            {
                logger.AddAndWrite("* Ending: " + DateTime.Now);
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
