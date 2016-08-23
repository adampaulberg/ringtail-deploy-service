using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoleResolverUtility
{
    public class Options
    {
        [Option('r', "role", Required=true)]
        public string Role { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            Console.WriteLine("Role Resolver - ");
            Console.WriteLine("  Reads in the masterCommands.config file and filters it by the roles.config file.");
            Console.WriteLine("  Writes out master.config");
            Console.WriteLine("  master.config is then used late by the Composer to write out master.bat");

            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
