using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstallFetcher.App
{
    public class Options
    {
        public string Address { get; set; }

        [Option('f', "root", Required = true, HelpText = "Folder Root")]
        public string FolderRoot { get; set; }

        [Option('b', "branch", HelpText = "Branch")]
        public string BranchName { get; set; }

        [Option('s', "suffix", HelpText = "Folder Suffix")]
        public string FolderSuffix { get; set; }

        [Option('a', "appName", HelpText = "Application Name")]
        public string ApplicationName { get; set; }

        [Option('v', "version", Required = false, DefaultValue="1", HelpText = "Application Name")]
        public string Version { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output file suffix")]
        public string Output { get; set; }

        [Option('e', "errorLevel", Required = false, DefaultValue="1", HelpText = "Error level.")]
        public string ErrorLevel { get; set; }

        internal int GetErrorLevel()
        {
            int val = 0;
            Int32.TryParse(ErrorLevel, out val);
            return val;
        }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}