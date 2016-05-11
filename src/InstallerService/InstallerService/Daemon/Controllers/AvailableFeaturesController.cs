using InstallerService.Helpers;
using System;
using System.Diagnostics;
using System.Linq;
using System.Web.Http;
using System.IO;
using System.Collections.Generic;
using System.Web;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Net.Http.Headers;

namespace InstallerService.Daemon.Controllers
{
    public class AvailableFeaturesController : BaseController
    {
        [HttpGet]
        public HttpResponseMessage GetKeys(string dropLocation)
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

                //return keys;
            }

            HttpResponseMessage hr = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            try
            {
                hr.Content = new StringContent(keys, System.Text.Encoding.Default, "application/json");

                //hr.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                //hr.ContentEncoding = System.Text.Encoding.UTF8;
                //hr.ContentType = "application/json";
                //hr.StatusCode = 200;
                //hr.Write(keys);
            }
            catch (Exception ex)
            {
                log.Add(ex.Message);
                log.Add(ex.StackTrace);
                FileHelpers.SimpleFileWriter.Write(@"C:\Upgrade\InstallerService\availableFeaturesLogError.txt", log);
            }
            log.Add("Made it");
            FileHelpers.SimpleFileWriter.Write(@"C:\Upgrade\InstallerService\availableFeatures-Diagnostic.txt", log);
            return hr;
        }

        private class JunkContent : HttpContent
        {
            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                throw new NotImplementedException();
            }

            protected override bool TryComputeLength(out long length)
            {
                throw new NotImplementedException();
            }
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
