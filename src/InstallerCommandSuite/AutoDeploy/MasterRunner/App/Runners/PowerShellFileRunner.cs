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
            int errorCode = 0;
            Console.WriteLine("Processing via PowerShellRunner");

            Runspace rs = System.Management.Automation.Runspaces.Runspace.DefaultRunspace;
            PowerShell ps = PowerShell.Create();
            
            if(contents != null)
            {
                Runspace runSpace = RunspaceFactory.CreateRunspace();
                runSpace.Open();
                Pipeline pipeline = runSpace.CreatePipeline();

                foreach (var x in contents)
                {
                    pipeline.Commands.Add(new Command(x));
                }

                this.Output = pipeline.Invoke();

                errorCode = pipeline.HadErrors ? 1 : 0;

            }

            return errorCode;
        }
    }
}
