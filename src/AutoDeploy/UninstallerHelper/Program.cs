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
            Logger logger = new Logger();
            logger.fileName = "UninstallerHelper.log";
            try
            {
                logger.AddAndWrite("-- UninstallerHelper --");
                var ringtailKeys = RegistryHelper.GetAllRingtailKeys(logger);

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

                logger.AddAndWrite(" Found the following exclusions: ");
                foreach (var x in exclusions)
                {
                    logger.AddAndWrite("     " + x);
                }


                List<RegistryFacade> rfList = new List<RegistryFacade>();
                var exclusionsAsArray = exclusions.ToArray();
                foreach (var x in ringtailKeys)
                {
                    logger.AddAndWrite("Reading reg key: " + x.Name);
                    var rfItem = new RegistryFacade(x, logger);
                    var uninstallString = UninstallCommandGenerator.CreateUninstallString(logger, rfItem, matchBy, exclusionsAsArray);

                    if (!String.IsNullOrEmpty(uninstallString))
                    {
                        allUninstallStrings.Add(uninstallString);
                    }
                }

                logger.AddAndWrite("Read all keys - generated " + allUninstallStrings.Count + " uninstall commands.");

                allUninstallStrings = new Prioritizer().OrderCommands(allUninstallStrings).ToList();

                logger.AddAndWrite("Prioritizer finished.");

                if (allUninstallStrings.Count == 0)
                {
                    allUninstallStrings.Add("@echo Nothing to uninstall");
                    logger.AddAndWrite("WARNING: Found nothing to uninstall.");
                    exitCode = 0;
                }  

                allUninstallStrings.ForEach(x => Console.WriteLine(x));                

                logger.AddAndWrite("Writing " + outputFile);

                SimpleFileWriter.Write(outputFile, allUninstallStrings);

                if (!new FileInfo(outputFile).Exists)
                {
                    logger.AddAndWrite("Failed to write " + outputFile);
                    exitCode = 1;
                }
            }
            catch (Exception ex)
            {
                try
                {
                    logger.AddAndWrite(ex.Message);
                    logger.AddAndWrite(ex.StackTrace);
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
