using InstallerService.Helpers;
using System;
using System.Diagnostics;
using System.Web.Http;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Linq;

namespace InstallerService.Daemon.Controllers
{
    public class AvailableFeaturesController : BaseController
    {
        [HttpGet]
        public HttpResponseMessage GetInstalledKeys(string connectionString, string dropLocation)
        {
            string keys = "";
            var log = new List<string>();

            var installerServiceFolder = EnvironmentInfo.INSTALLER_SERVICE_WORKING_FOLDER;
            var config = EnvironmentInfo.InstallerServiceConfig();
            var fileName = string.Empty;

            log.Add("Getting Lit Keys");
            try
            {
                if (!dropLocation.StartsWith("\\"))
                {
                    // open volitle data.
                    var volitleData = FileHelpers.ReadConfigAsData("volitleData.config");
                    var folderRoot = volitleData.Find(x => x.Contains("BUILD_FOLDER_ROOT"));
                    folderRoot = folderRoot.Substring(folderRoot.IndexOf(@"\"));
                    folderRoot = folderRoot.Replace("\"", "");
                    log.Add("....checking volitleData for build folder root ");
                    dropLocation = folderRoot + @"\" + dropLocation;
                    if (!dropLocation.EndsWith(@"\"))
                    {
                        dropLocation = dropLocation + @"\";
                    }
                }


                log.Add("Starting.... " + DateTime.Now.ToLongDateString());

                CopyFilesLocally(dropLocation);

                fileName = LOCAL_PATH + PARSER_FILE;
                string cmd = "/c " + fileName;
                var processInfo = new ProcessStartInfo("cmd.exe", cmd);


                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = fileName;
                process.StartInfo.WorkingDirectory = dropLocation;
                process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.Arguments = "-gf -portalconnection=\"" + connectionString + "\"";

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

                var errorResponse = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
                errorResponse.Content = new StringContent("Error - see logs on server", System.Text.Encoding.Default, "application/text");
                return errorResponse;
            }

            HttpResponseMessage hr = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            try
            {
                hr.Content = new StringContent(keys, System.Text.Encoding.Default, "application/json");
            }
            catch (Exception ex)
            {
                log.Add(ex.Message);
                log.Add(ex.StackTrace);
                FileHelpers.SimpleFileWriter.Write(@"C:\Upgrade\InstallerService\availableFeaturesLogError.txt", log);

                var errorResponse = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
                errorResponse.Content = new StringContent("Error - see logs on server", System.Text.Encoding.Default, "application/text");
                return errorResponse;
            }

            FileHelpers.SimpleFileWriter.Write(@"C:\Upgrade\InstallerService\availableFeatures-Diagnostic.txt", log);
            return hr;

            throw new NotImplementedException();
        }


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


            if (!dropLocation.StartsWith("\\"))
            {
                // open volitle data.
                var volitleData = FileHelpers.ReadConfigAsData("volitleData.config");
                var folderRoot = volitleData.Find(x => x.Contains("BUILD_FOLDER_ROOT"));
                folderRoot = folderRoot.Substring(folderRoot.IndexOf(@"\"));
                folderRoot = folderRoot.Replace("\"", "");
                log.Add("....checking volitleData for build folder root ");
                dropLocation = folderRoot + @"\" + dropLocation;
                if (!dropLocation.EndsWith(@"\"))
                {
                    dropLocation = dropLocation + @"\";
                }
            }

            log.Add(" reading from: " + dropLocation);

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

                fileName = LOCAL_PATH + PARSER_FILE;
                string cmd = "/c " + fileName;
                var processInfo = new ProcessStartInfo("cmd.exe", cmd);


                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = fileName;
                process.StartInfo.WorkingDirectory = dropLocation;
                process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.Arguments = "-gk";

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

                var errorResponse = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
                errorResponse.Content = new StringContent("Error - see logs on server", System.Text.Encoding.Default, "application/text");
                return errorResponse;
            }

            HttpResponseMessage hr = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            try
            {
                hr.Content = new StringContent(keys, System.Text.Encoding.Default, "application/json");
            }
            catch (Exception ex)
            {
                log.Add(ex.Message);
                log.Add(ex.StackTrace);
                FileHelpers.SimpleFileWriter.Write(@"C:\Upgrade\InstallerService\availableFeaturesLogError.txt", log);

                var errorResponse = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
                errorResponse.Content = new StringContent("Error - see logs on server", System.Text.Encoding.Default, "application/text");
                return errorResponse;
            }

            FileHelpers.SimpleFileWriter.Write(@"C:\Upgrade\InstallerService\availableFeatures-Diagnostic.txt", log);
            return hr;
        }

        private static string PARSER_FILE = "ringtail-deploy-feature-utility.exe";
        private static string DATA_FILE = "ringtail-static-feature-data.csv";
        private static string LOCAL_PATH = @"C:\upgrade\autodeploy\";

        private static void CopyFilesLocally(string dropLocation)
        {
            var fi = new FileInfo(dropLocation + PARSER_FILE);
            fi.CopyTo(LOCAL_PATH + PARSER_FILE, true);

            fi = new FileInfo(dropLocation + DATA_FILE);
            fi.CopyTo(LOCAL_PATH + DATA_FILE, true);
        }
    }
}
