using ServiceInstaller.App;
using ServiceInstaller.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ServiceInstaller
{
    class Program
    {
        static int Main(string[] args)
        {
            var exitCode = 0;
            
            if(args.Length == 0)
            {
                GetUsage();
                return 2;
            }

            Console.WriteLine("ServiceInstaller starting for " + args[0]);

            string appName = args[0];

            try
            {
                exitCode = ServiceInstallerHelper.RunIt(appName);
                List<string> s = new List<string>();
                s.Add(args[0]);
                s.Add("Ok");
                SimpleFileWriter.Write("ServiceInstallerLog-" + appName + ".txt", s);
            }
            catch (Exception ex)
            {
                List<string> s = new List<string>();
                s.Add(ex.Message);
                s.Add(ex.StackTrace);
                SimpleFileWriter.Write("ServiceInstallerLog-" + appName + ".txt", s);

                Console.WriteLine("ServiceInstaller error");
                s.ForEach(x => Console.WriteLine(s));

                exitCode = 1;
            }

            return exitCode;
        }

        public static void GetUsage()
        {
            Console.WriteLine("ServiceInstaller - ");
            Console.WriteLine("  Usage:    ServiceInstaller.exe [appName]");
            Console.WriteLine("");
            Console.WriteLine("... this will create a batch file that calls the DeployToIIS.exe to unpack a zip file and install an IIS website.");
            Console.WriteLine("... It uses a convention based approach - only param is the service name.");
        }


    }

}
