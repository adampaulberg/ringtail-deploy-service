using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileCleaner
{
    public class Options
    {
        [Option('p', "path", Required = false, DefaultValue = @"C:\Upgrade\AutoDeploy", HelpText = "The path to look for files to clean up.")]
        public string Path { get; set; }

        [Option('s', "startswith", Required = true, HelpText = "The filter for file name to start with.")]
        public string Startswith { get; set; }

        [Option('e', "ext", Required = false, DefaultValue = "exe", HelpText = "The filter for file names extension.")]
        public string Extension { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }

}
