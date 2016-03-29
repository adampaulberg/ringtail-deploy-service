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
        static int Main(string[] args)
        {
            int exitCode = 0;
            try
            {
                var options = new Options();
                var upgrader = new Upgrader(Console.Write);

                if (CommandLine.Parser.Default.ParseArguments(args, options))
                {
                    if(!options.ValidateActions())
                    {
                        Console.WriteLine(options.GetUsage());
                        return 1;
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
                        return 2;
                    }


                    if (string.IsNullOrEmpty(options.Version))
                    {
                        Console.WriteLine("Version could not be found. Ensure SQL Components is installed");
                        return 3;
                    }

                    // Step 2. Create the upgrade script
                    ScriptInstallHelper.InstallScripts(options, Console.Write);


                    // Step 3. Upgrade the 
                    exitCode = upgrader.UpgradeDatabases(options);

                    if (exitCode == 0)
                    {
                        Console.WriteLine("\r\nDatabase upgrade complete!");
                    }
                    else
                    {
                        Console.WriteLine("\r\nDatabase upgrade finished with errors!");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("\r\nUnhandled Exception:\r\n{0}", ex);
                exitCode = 4;
            }

            return exitCode;
        }
    }
}
