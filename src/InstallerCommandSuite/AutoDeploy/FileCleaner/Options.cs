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
        [OptionList('f', "paths", Separator= ',', HelpText = "The comma separated list of files or folders to delete")]
        public IList<string> Paths { get; set; }

        [OptionList('s', "subs", Separator = ',', HelpText = "The comma separated list of root directories where all sub-folders will be deleted")]
        public IList<string> Subs { get; set; }

        [Option('o', "out", Required = true, HelpText = "The output batch file that will be created")]
        public string OutFile { get; set; }

        [Option('l', "log", Required = true, HelpText = "The log file")]
        public string LogFile { get; set; }        

        [HelpOption]
        public string GetUsage()
        {
            var autogen = HelpText.AutoBuild(this, (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
            return "\r\nThis program creates a script file that will delete the specified files and directories.\r\n\r\n" + autogen; 
        }
    }

}
