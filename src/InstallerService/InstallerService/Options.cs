using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstallerService
{
    public class Options
    {
        [Option('p', "port", DefaultValue = 8080u, HelpText = "The port that the daemon should listen on")]
        public uint Port { get; set; }

        [Option('a', "host", HelpText = "Host address for the daemon to run on")]
        public string Host { get; set; }

        [Option('c', "console", HelpText = "Run in console mode")]
        public bool ConsoleMode { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}