using MasterRunner.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using MasterRunner.App.Runners;
using System.Management.Automation.Runspaces;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace MasterRunner.App.Runners
{
    public class PowerShellFileRunner : IRunner
    {
        private string filename;
        private string workingFolder;
        private Logger logger;
        private string username;
        private string password;
        private List<string> contents;

        public PowerShellFileRunner(Logger logger, List<string> contents)
        {
            this.logger = logger;
            logger.fileName = "testoutput.txt";
            this.contents = contents;
        }

        public PowerShellFileRunner(Logger logger, string filename, string workingFolder, string username, string password)
        {
            this.logger = logger;
            this.filename = filename;
            this.workingFolder = workingFolder;
            this.username = username;
            this.password = password;
        }

        public Collection<PSObject> Output { get; private set; }

        public int RunFile()
        {
            Console.WriteLine("Processing via PowerShellRunner");

            var allowedExits = SimpleFileReader.Read(workingFolder + "allowedExit.config");

            ProcessExecutorHelper helper = new ProcessExecutorHelper(logger, allowedExits, SimpleFileReader.Read("timeout.config"), 10000);

            int exitCode = helper.SpawnAndLog("PowerShell.exe -ExecutionPolicy Bypass -File " + filename, workingFolder, username, password);
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
