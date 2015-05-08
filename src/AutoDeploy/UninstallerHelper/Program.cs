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
                var ringtailKeys = RegistryHelper.GetAllRingtailKeys();

                var allUninstallStrings = new List<string>();

                string matchBy = String.Empty;
                if(args.Length == 0 )
                {
                    matchBy = args[0];
                }

                string outputFile = "uninstall.bat";

                string[] exclusions = null;
                if (args.Length > 1)
                {
                    var exclusionStrings = args[1];
                    var exclusionParts = exclusionStrings.Split(',');
                    exclusions = exclusionParts
                        .Select(p => p.Trim())
                        .Where(p => !string.IsNullOrEmpty(p))
                        .ToArray();
                }

                ringtailKeys.ForEach(z => allUninstallStrings.Add(UninstallCommandGenerator.CreateUninstallString(z, matchBy, exclusions)));

                allUninstallStrings = new Prioritizer().OrderCommands(allUninstallStrings).ToList();

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
