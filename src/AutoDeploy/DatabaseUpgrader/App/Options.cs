using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseUpgrader.App
{
    public class Options
    {
        /// <summary>
        /// Valid arguments are upgrade and upgradePortal
        /// </summary>
        [ValueList(typeof(List<string>), MaximumElements = 1)]
        public IList<string> Actions { get; set; }

        [Option('u', "username", Required = true, HelpText = "SA equivalent username needed to execute upgrade")]
        public string Username { get; set; }

        [Option('p', "password", Required = true, HelpText = "SA equivalent password needed to execute upgrade")]
        public string Password { get; set; }

        [Option('s', "server", DefaultValue = @"localhost", HelpText = "The server where the databases live")]
        public string Server { get; set; }

        [OptionList('d', "databases", Separator = ',', HelpText = "The comma separated list of databases to upgrade as used by the upgrade action")]
        public IList<string> Databases { get; set; }

        [Option('c', "component", HelpText = "The SQL Component version to use, defaults to newest in install path")]
        public string Version { get; set; }

        [Option('m', "max", DefaultValue = "3", HelpText = "The maximum number of concurrent DB upgrades to run at the same time.")]
        public string Max { get; set; }

        [Option('i', "install-path", DefaultValue = @"C:\Program Files\Ringtail", HelpText = "SQL Components install path")]
        public string InstallPath { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            var heading = GetHeading();
            var usage = @"This tool wraps a call to the DataCamel in a batch file and fills in its parameters.\n";
            var autogen = HelpText.AutoBuild(this, (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
            autogen.Copyright = " ";
            autogen.Heading = "Options:";
            return heading.ToString() + usage + autogen;
        }

        public string GetHeading()
        {
            var heading = new StringBuilder();
            heading.Append(@"");
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            heading.AppendLine(string.Format("DatabaseUpgrader {0}.{1}.{2}", version.Major, version.Minor, version.Revision));
            heading.AppendLine("Copyright c 2014 FTI Consulting");

            return heading.ToString();
        }


        internal static string ConvertOptionListToSingle(IList<string> optionList)
        {
            string singleString = string.Empty;

            if (optionList.Count > 0)
            {
                foreach (var x in optionList.ToList())
                {

                    singleString += x + ",";
                }

                Console.WriteLine(singleString);
                if (singleString.Length > 1)
                {
                    singleString = singleString.Substring(0, singleString.Length - 1);
                }
                Console.WriteLine(singleString);
            }

            return singleString;
        }

        public bool ValidateIsDefaultAction()
        {
            var action = this.Actions[0];
            if (this.Actions.Count == 0)
                return false;
            var normalized = action.ToLower();
            return normalized == "datacamel_action";
        }

        public bool ValidateActions()
        {
            if (this.Actions.Count == 0)
                return false;

            var action = this.Actions[0];

            var normalized = action.ToLower();

            if (normalized != "upgrade" && normalized != "upgradeportal")
            {
                return false;
            }

            if (normalized == "upgrade")
            {
                if (this.Databases == null || this.Databases.Count == 0)
                    return false;
            }

            if (normalized == "upgradeportal")
            {
                if (this.Actions.Count != 1)
                    return false;
            }

            return true;
        }
    }
}