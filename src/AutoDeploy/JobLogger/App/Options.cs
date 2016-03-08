using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobLogger.App
{
    public class Options
    {
        public string Address { get; set; }

        [Option('d', "dropFolder", Required = false)]
        public string FolderRoot { get; set; }

        [Option('h', "header", Required = false)]
        public string Header { get; set; }

        [Option('m', "message", Required=true)]
        public string Message { get; set; }

        [Option('c', "copyLogs", Required = false)]
        public string CopyLogs { get; set; }

        [Option('t', "tag", Required = false)]
        public string Tag { get; set; }

        [Option('v', "version", Required = false, DefaultValue="2.6.0")]
        public string Version { get; set; }


        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}