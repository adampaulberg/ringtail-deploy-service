using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DataCamel.App
{
    public class Options
    {
        /// <summary>
        /// Valid arguments are upgrade and portal-upgrade [portal]
        /// </summary>
        [ValueList(typeof(List<string>), MaximumElements = 2)]
        public IList<string> Actions { get; set; }

        [Option('u', "username", Required=true, HelpText="SA equivalent username needed to execute upgrade")]
        public string Username { get; set; }

        [Option('p', "password", Required=true, HelpText="SA equivalent password needed to execute upgrade")]
        public string Password { get; set; }

        [Option('s', "server", DefaultValue = @"localhost", HelpText="The server where the databases live")]
        public string Server { get; set; }

        [OptionList('d', "databases", Separator = ',', HelpText = "The comma separated list of databases to upgrade as used by the upgrade action")]
        public IList<string> Databases { get; set; }

        [Option('c', "component", HelpText="The SQL Component version to use, defaults to newest in install path")]
        public string Version { get; set; }

        [Option('m', "max", DefaultValue="3", HelpText = "The max number of databases to upgrade concurrently.")]
        public string Max { get; set; }

        [Option('i', "install-path", DefaultValue=@"C:\Program Files\Ringtail", HelpText="SQL Components install path")]
        public string InstallPath { get; set; }

        public string GetHeading()
        {
            var heading = new StringBuilder();
            heading.Append(@"
                ,,__
      ..  ..   / o._)
     /--'/--\  \-'||
    /        \_/ / |
  .'\  \__\  __.'.'
    )\ |  )\ |
   // \\ // \\
  ||_  \\|_  \\_
  '--' '--'' '--'

");
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            heading.AppendLine(string.Format("DataCamel {0}.{1}.{2}", version.Major, version.Minor, version.Build));
            heading.AppendLine("Copyright c 2015 FTI Consulting");

            return heading.ToString();
        }


        [HelpOption]
        public string GetUsage()
        {
            var heading = GetHeading();
            var usage = @"

DataCamel is a utility for upgrading Ringtail databases. It can be used
to upgrade a list of databases or an entire portal. It will take a 
specific version of the Ringtail SQL Comoponents as an option or it can
use the latest version of SQL Components installed.

Usage: 

    DataCamel upgrade -u [user] -p [pass] -d [databases]
    DataCamel upgrade -u username -p passwd -d portal,case1,case2,rpf

    Action: 'upgrade' performs an upgrade of the spefified databases 
    supplied with the --databases option

    
    DataCamel upgradeportal [portaldb] -u [user] -p [pass]
    DataCamel upgradeportal portaldb -u username -p passwd

    Action: 'upgradeportal <portal db name>' will attempt to find all cases
    and the rpf database for the specified portal. It will perform upgrades
    of the portal db, case dbs, rpf db, and rstempdb. This option ignores 
    databases supplied with the --database option.


";
            

            var autogen = HelpText.AutoBuild(this, (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
            autogen.Copyright = " ";
            autogen.Heading = "Options:";
            return heading.ToString() + usage + autogen;
        }

        public bool ValidateActions()
        {
            if (this.Actions.Count == 0)
                return false;

            var action = this.Actions[0];

            if (action != "upgrade" && action != "upgradeportal")
                return false;

            if(action == "upgrade")
            {
                if(this.Databases == null || this.Databases.Count == 0)
                    return false;
            }

            if (action == "upgradeportal")
            {
                if (this.Actions.Count < 2)
                    return false;
            }

            return true;
        }
    }
}
