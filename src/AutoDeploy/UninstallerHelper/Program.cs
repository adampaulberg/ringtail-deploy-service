using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UninstallerHelper.App;
using UninstallerHelper.Util;

namespace UninstallerHelper
{
    class Program
    {
        static int Main(string[] args)
        {
            int exitCode = 0;
            Logger l = new Logger();
            l.fileName = "UninstallerHelper.log";
            try
            {
                l.AddAndWrite("-- UninstallerHelper --");
                var ringtailKeys = RegistryHelper.GetAllRingtailKeys(l);

                var allUninstallStrings = new List<string>();

                string matchBy = String.Empty;
                if(args.Length == 0 )
                {
                    matchBy = args[0];
                }

                string outputFile = "uninstall.bat";


                var exclusions = new List<string>();
                exclusions.Add("native");
                if (args.Length > 1)
                {
                    var exclusionStrings = args[1];
                    var exclusionParts = exclusionStrings.Split(',');
                    exclusions = exclusionParts
                        .Select(p => p.Trim())
                        .Where(p => !string.IsNullOrEmpty(p)).ToList();
                }
                exclusions.AddRange(DynamicExclusionDetector.DetectExclusions());

                l.AddAndWrite(" Found the following exclusions: ");
                foreach (var x in exclusions)
                {
                    l.AddAndWrite("     " + x);
                }


                List<RegistryFacade> rfList = new List<RegistryFacade>();
                var exclusionsAsArray = exclusions.ToArray();
                foreach (var x in ringtailKeys)
                {
                    l.AddAndWrite("Reading reg key: " + x.Name);
                    var rfItem = new RegistryFacade(x, l);
                    var uninstallString = UninstallCommandGenerator.CreateUninstallString(l, rfItem, matchBy, exclusionsAsArray);

                    if (!String.IsNullOrEmpty(uninstallString))
                    {
                        allUninstallStrings.Add(uninstallString);
                    }
                }

                l.AddAndWrite("Read all keys - generated " + allUninstallStrings.Count + " uninstall commands.");

                allUninstallStrings = new Prioritizer().OrderCommands(allUninstallStrings).ToList();

                l.AddAndWrite("Prioritizer finished.");

                if (allUninstallStrings.Count == 0)
                {
                    allUninstallStrings.Add("@echo Nothing to uninstall");
                    l.AddAndWrite("WARNING: Found nothing to uninstall.");
                    exitCode = 0;
                }  

                allUninstallStrings.ForEach(x => Console.WriteLine(x));                

                l.AddAndWrite("Writing " + outputFile);

                SimpleFileWriter.Write(outputFile, allUninstallStrings);

                if (!new FileInfo(outputFile).Exists)
                {
                    l.AddAndWrite("Failed to write " + outputFile);
                    exitCode = 1;
                }
              
            }
            catch (Exception ex)
            {
                try
                {
                    l.AddAndWrite(ex.Message);
                    l.AddAndWrite(ex.StackTrace);
                    exitCode = 1;
                }
                catch(Exception x)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine("--");
                    Console.WriteLine(x.Message);
                    Console.WriteLine(x.StackTrace);
                    exitCode = 5;
                }
            }

            return exitCode;
        }
    }

}
