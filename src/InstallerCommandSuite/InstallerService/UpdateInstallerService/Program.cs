using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace UpdateInstallerService
{
    class Program
    {
        static void Main(string[] args)
        {
            DoIt();

        }

        private static void DoIt()
        {
            List<string> log = new List<string>();

            // Give callers time to return a result before shutting down the web service.
            System.Threading.Thread.Sleep(500);

            ServiceController sc = null;
            try
            {
                var services = ServiceController.GetServices();

                foreach (var service in services)
                {
                    if (service.DisplayName.Contains("RingtailDeployService"))
                    {
                        sc = service;
                    }
                }
                if (sc != null)
                {
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped);
                    log.Add("RingtailDeployService stopped....");
                }
            }
            catch (Exception ex)
            {
                log.Add("RingtailDeployService COULD NOT BE STOPPED: " + ex.Message);
                sc = null;
            }

            try
            {

                string dropFolder = EnvironmentInfo.GetUpgradeDropFolderConfig();
                string command = @"xcopy " + dropFolder + " " + EnvironmentInfo.UPGRADE_ROOT + " /y /s /c /h";

                log.Add("Drop Folder: " + dropFolder);
                log.Add("Command: " + command);

                Console.WriteLine(command);
                int exitCode;
                ProcessStartInfo processInfo;
                Process process;

                processInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
                processInfo.CreateNoWindow = true;
                processInfo.UseShellExecute = false;
                // *** Redirect the output ***
                processInfo.RedirectStandardError = true;
                processInfo.RedirectStandardOutput = true;

                process = Process.Start(processInfo);

                // *** Read the streams ***
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                exitCode = process.ExitCode;


                Console.WriteLine("O: " + output);
                Console.WriteLine("E: " + error);

                log.Add("Output....................");
                log.Add(output);

                log.Add("Errors....................");
                log.Add(error);

                if (sc != null)
                {
                    try
                    {
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running);
                        log.Add("RingtailDeployService started....");
                    }
                    catch (Exception x)
                    {
                        log.Add("RingtailDeployService COULD NOT BE RESTARTED: " + x.Message);
                    }
                }
            }
            catch (Exception x)
            {
                log.Add(x.Message);
                log.Add(x.StackTrace);
            }
            SimpleFileWriter.Write("InstallerServiceUpgradeLog.txt", log);
        }

        public class EnvironmentInfo
        {
            public static string UPGRADE_ROOT = @"C:\Upgrade\";
            public static string CONFIG_LOCATION = @"C:\Upgrade\InstallerService\upgrade.config";

            public static string GetUpgradeDropFolderConfig()
            {
                var dropFolder = string.Empty;
                var fileName = string.Empty;
                FileInfo fi = new FileInfo(EnvironmentInfo.CONFIG_LOCATION);
                if (fi.Exists)
                {
                    var x = SimpleFileReader.Read(EnvironmentInfo.CONFIG_LOCATION);
                    dropFolder = x[0].Split('|')[1];
                }

                return dropFolder;
            }
        }

        public class SimpleFileWriter
        {
            public static void Write(string fileName, List<string> s)
            {
                using (StreamWriter wr = new StreamWriter(fileName))
                {
                    foreach (string str in s)
                    {
                        wr.WriteLine(str);
                    }
                }
            }
        }

        public class SimpleFileReader
        {
            public static List<string> Read(string fileName)
            {
                List<string> s = new List<string>();
                using (StreamReader stream = new StreamReader(fileName))
                {
                    string input = null;
                    while ((input = stream.ReadLine()) != null)
                    {
                        s.Add(input);
                    }
                }

                return s;
            }
        }
    }
}