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

                ringtailKeys.ForEach(z => allUninstallStrings.Add(UninstallCommandGenerator.CreateUninstallString(z, matchBy)));
                allUninstallStrings.ForEach(x => Console.WriteLine(x));                

                l.AddAndWrite("Writing uninstall.bat");
                SimpleFileWriter.Write("uninstall.bat", allUninstallStrings);

                if (!new FileInfo("uninstall.bat").Exists)
                {
                    l.AddAndWrite("Failed to write uninstall.bat");
                    exitCode = 1;
                }
                if (allUninstallStrings.Count == 0)
                {
                    allUninstallStrings.Add("echo Nothing to uninstall");
                    SimpleFileWriter.Write("uninstall.bat", allUninstallStrings);
                    l.AddAndWrite("WARNING: Found nothing to uninstall.");                    
                    exitCode = 0;
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
