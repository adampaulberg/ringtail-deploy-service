using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using System.Linq;
using MasterRunner.App.Runners;
using MasterRunner.Util;
using System.Diagnostics;
using System.Management.Automation;

namespace InstallerTests.MasterRunner
{
    [TestClass]
    public class PowerShellFileTest
    {
        [TestMethod]
        public void PowerShellBaseTest()
        {
            Logger logger = new Logger();

            List<string> fileContents = new List<string>();
            fileContents.Add("Get-Process");
            fileContents.Add("Sort-Object");

            PowerShellFileRunner psF = new PowerShellFileRunner(logger, fileContents);
            psF.RunFile();
            var output = psF.Output;
            foreach (PSObject psObject in output)
            {
                var process = (Process) psObject.BaseObject;
                Console.WriteLine("Process name: " + process.ProcessName);
            }
        }
    }

    public class TestOutputEngine : IOutputEngine
    {
        public List<string> Log { get; set; }
        public void Output(List<string> s)
        {
            if (Log == null)
            {
                Log = new List<string>();
            }
            Log.AddRange(s);
        }

    }
}
