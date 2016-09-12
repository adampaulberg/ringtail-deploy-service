using MasterRunner.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterRunner.App.Runners
{

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
            logger.AddAndWrite("* Processing via ExeFileRunner");

            var allowedExits = SimpleFileReader.Read(workingFolder + "allowedExit.config");
            ProcessExecutorHelper helper = new ProcessExecutorHelper(logger, allowedExits, SimpleFileReader.Read("timeout.config"), 0);

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
