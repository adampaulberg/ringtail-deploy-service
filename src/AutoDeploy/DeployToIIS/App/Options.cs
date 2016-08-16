
using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace TestIISDeploy
{
    public class Options
    {
        [Option('u', "username", Required = true, HelpText = "username")]
        public string Username { get; set; }

        [Option('p', "password", Required = true, HelpText = "password")]
        public string Password { get; set; }

        [Option('a', "appname", Required = true , HelpText = "The name of the application")]
        public string AppName { get; set; }

        [Option('i', "install-path", Required = true, HelpText = "Path of the service")]
        public string InstallPath { get; set; }


        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
