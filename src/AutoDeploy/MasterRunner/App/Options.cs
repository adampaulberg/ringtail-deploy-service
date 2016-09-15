using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterRunner.App
{
    public class Options
    {
        [Option('f', "file", Required = true, HelpText = "File to execute")]
        public string FileName { get; set; }

        [Option('d', "workingFolder", DefaultValue = "", HelpText = "location of the batch file")]
        public string WorkingFolder { get; set; }

        [Option('o', "output", DefaultValue = "buildOutput.txt", HelpText = "log to file")]
        public string OutputFile { get; set; }

        [Option('u', "user", HelpText = "domain and user to execute runas command")]
        public string User { get; set; }

        [Option('p', "password", HelpText = "password to execute runas command")]
        public string Password { get; set; }


        [Option('l', "logMode", DefaultValue = "", HelpText = "if set to 'append' this run will append to the logs rather than restarting.")]
        public string LogMode { get; set; }



        [Option('t', "defaultTaskTimeout", DefaultValue = 600000, Required = false, HelpText = "default task timeout length")]
        public int Timeout { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
