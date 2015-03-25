using DataCamel.App;
using DataCamel.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DataCamel
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var options = new Options();
                var upgrader = new Upgrader(Console.Write);

                if (CommandLine.Parser.Default.ParseArguments(args, options))
                {
                    if(!options.ValidateActions())
                    {
                        Console.WriteLine(options.GetUsage());
                        return;
                    }

                    // Step 0. Camels yo
                    Console.WriteLine(options.GetHeading());
                    Console.WriteLine("Starting database upgrade");

                    // Step 1. Find the version we want to upgrade to
                    if (string.IsNullOrEmpty(options.Version))
                        options.Version = upgrader.FindLatestVersion(options);

                    // Step 1.1 Verify
                    if (upgrader.ValidateSqlComponent(options))
                        Console.WriteLine(string.Format("Using Sql Components version {0}", options.Version));
                    else
                    {
                        Console.WriteLine(string.Format("Sql Components version {0} could not be found", options.Version));
                        return;
                    }


                    if (string.IsNullOrEmpty(options.Version))
                    {
                        Console.WriteLine("Version could not be found. Ensure SQL Components is installed");
                        return;
                    }

                    // Step 2. Create the upgrade script
                    ScriptInstallHelper.InstallScripts(options, Console.Write);


                    // Step 3. Upgrade the 
                    upgrader.UpgradeDatabases(options);

                    Console.WriteLine("Database upgrade complete!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("\r\nUnhandled Exception:\r\n{0}", ex);
            }
        }
    }
}
