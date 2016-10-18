using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace DeployToIIS
{
    public class Options
    {
        [Option('u', "username", Required = false, HelpText = "username")]
        public string Username { get; set; }

        [Option('p', "password", Required = false, HelpText = "password")]
        public string Password { get; set; }

        [Option('a', "appname", Required = true , HelpText = "The name of the application")]
        public string AppName { get; set; }

        [Option('i', "install-path", Required = true, HelpText = "Path of the service")]
        public string InstallPath { get; set; }


        [Option('v', "runtimeVersion", Required = false, DefaultValue ="v4.0", HelpText = "CLR Versions")]
        public string ManagedRuntimeVersion { get; set; }



        [HelpOption]
        public string GetUsage()
        {
            Console.WriteLine("  This tool takes an installed path where the file is already unzipped, and adds an app pool and IIS website for that folder.");

            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
