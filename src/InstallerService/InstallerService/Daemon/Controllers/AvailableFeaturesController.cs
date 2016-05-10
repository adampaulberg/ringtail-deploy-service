using InstallerService.Helpers;
using System;
using System.Diagnostics;
using System.Linq;
using System.Web.Http;
using System.IO;
using System.Collections.Generic;

namespace InstallerService.Daemon.Controllers
{
    public class AvailableFeaturesController : BaseController
    {
        [HttpGet]
        public string GetKeys(string dropLocation)
        {
            // returns a list of available keys given the drop location.
            string keys = "";

            var log = new List<string>();

            var installerServiceFolder = EnvironmentInfo.INSTALLER_SERVICE_WORKING_FOLDER;
            var config = EnvironmentInfo.InstallerServiceConfig();
            var fileName = string.Empty;
            string username = null, password = null;


            // needs to call the exe at the drop location and get the possible keys.
            try
            {
                if (config.ContainsKey(EnvironmentInfo.KeyMasterRunnerUser) && config.ContainsKey(EnvironmentInfo.KeyMasterRunnerPass))
                {
                    username = config[EnvironmentInfo.KeyMasterRunnerUser];
                    password = config[EnvironmentInfo.KeyMasterRunnerPass];
                }

                log.Add("Starting.... " + DateTime.Now.ToLongDateString());

                CopyFilesLocally(dropLocation);

                fileName = @"C:\Upgrade\InstallerService\Test\" + "RingtailFeatureUtility.exe";
                string cmd = "/c " + fileName;
                var processInfo = new ProcessStartInfo("cmd.exe", cmd);


                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = fileName;
                process.StartInfo.WorkingDirectory = dropLocation;
                process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.Arguments = "-g";

                log.Add(" start info...");
                log.Add(" WorkingDir: " + dropLocation);
                log.Add(" FileName: " + fileName);
                log.Add(" Cmd: " + cmd);


                log.Add("Cmd target: " + process.StartInfo.WorkingDirectory);
                log.Add("Cmd file: " + process.StartInfo.FileName);

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                int exitCode = process.ExitCode;

                if (exitCode != 0)
                {
                    FileHelpers.SimpleFileWriter.Write(@"C:\Upgrade\InstallerService\availableFeaturesLog-Error.txt", log);
                }

                log.Add("Cmd errors: " + error);
                log.Add("Cmd output: " + output);
                log.Add("Cmd is: " + cmd);
                log.Add("Cmd exitCode: " + exitCode);

                FileHelpers.SimpleFileWriter.Write(@"C:\Upgrade\InstallerService\availableFeaturesLog.txt", log);


                if (exitCode == 0)
                {
                    keys = output;
                }

            }
            catch (Exception ex)
            {
                log.Add(ex.Message);
                log.Add(ex.StackTrace);
                FileHelpers.SimpleFileWriter.Write(@"C:\Upgrade\InstallerService\availableFeaturesLogError.txt", log);
                return "ERROR: " + ex.Message;
            }

            return keys;
        }

        private static void CopyFilesLocally(string dropLocation)
        {
            var fi = new FileInfo(dropLocation + "RingtailFeatureUtility.exe");
            fi.CopyTo(@"C:\upgrade\InstallerService\test\RingtailFeatureUtility.exe", true);

            fi = new FileInfo(dropLocation + "RingtailDarkKeys.csv");
            fi.CopyTo(@"C:\upgrade\InstallerService\test\RingtailDarkKeys.csv", true);
        }
    }
}
