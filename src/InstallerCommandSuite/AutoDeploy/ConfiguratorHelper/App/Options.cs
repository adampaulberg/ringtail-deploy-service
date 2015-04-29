using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConfiguratorHelper.App
{
    public class Options
    {
        [Option("host", Required = true, DefaultValue = "webserver01", HelpText = "Host url")]
        public string hostURL { get; set; }

        [Option("ntDomain", Required = true, DefaultValue = "lm", HelpText = "NT domain")]
        public string domain { get; set; }

        [Option("ntUser", Required = true, HelpText = "NT user")]
        public string username { get; set; }

        [Option("ntPassword", Required = true, HelpText = "NT password")]
        public string password { get; set; }

        [Option("classic", DefaultValue = "Classic", HelpText = "Classic url excl host")]
        public string classicSiteName { get; set; }

        [Option("appPool", DefaultValue = "DefaultAppPool", HelpText = "Application Pool")]
        public string applicationPool { get; set; }

        [Option("dbserver", Required = true, HelpText = "dbserver")]
        public string dbserver { get; set; }

        [Option("dbsauser", Required = true, HelpText = "dbserver admin user")]
        public string dbsauser { get; set; }

        [Option("dbsapassword", Required = true, HelpText = "dbserver admin password")]
        public string dbsapassword { get; set; }

        [Option("dbname", Required = true, HelpText = "portal database name")]
        public string dbname { get; set; }

        [Option("dbusername", Required = true, HelpText = "portal database user")]
        public string dbusername { get; set; }

        [Option("dbuserpassword", Required = true, HelpText = "portal database password")]
        public string dbuserpassword { get; set; }

        [Option("dbPort", Required = false, DefaultValue="1433", HelpText = "database port")]
        public string dbPort { get; set; }

        [Option("agentVirtualName", Required = false, DefaultValue = "Agent", HelpText = "virtual name for the agent")]
        public string agentVirtualName { get; set; }


        [Option("newAppPoolName", Required = false, DefaultValue = "", HelpText = "new name for the application pool")]
        public string newAppPoolName { get; set; }


        [HelpOption]
        public string GetUsage()
        {
            var heading = GetHeading();
            var usage = @"This tool wraps a call to the ConfiguratorWebService in a batch file and fills in its parameters.";
            var autogen = HelpText.AutoBuild(this, (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
            autogen.Copyright = " ";
            autogen.Heading = "Options:";
            return heading + " " + usage + autogen;
        }

        private string GetHeading()
        {
            var heading = new StringBuilder();
            heading.Append(@"");
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            heading.AppendLine(string.Format("DatabaseUpgrader {0}.{1}.{2}", version.Major, version.Minor, version.Revision));
            heading.AppendLine("Copyright c 2014 FTI Consulting");

            return heading.ToString();
        }


    }
}
