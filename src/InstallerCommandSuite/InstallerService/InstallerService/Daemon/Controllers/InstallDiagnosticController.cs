using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace InstallerService.Daemon.Controllers
{
    public class InstallDiagnosticController : BaseController
    {
        [HttpGet]
        public string RunDiagnosticPreInstall()
        {
            // this just runs master.exe so you can remotely query and inspect the resulting master.bat
            string results = string.Empty;
            var installerServiceFolder = EnvironmentInfo.INSTALLER_SERVICE_WORKING_FOLDER;
            var autoDeployFolder = EnvironmentInfo.GetAutoDeploySuiteFolder();
            var fileName = string.Empty;
            var buildLog = new List<string>();

            try
            {
                FileInfo fi = new FileInfo(EnvironmentInfo.CONFIG_LOCATION);
                if (fi.Exists)
                {
                    try
                    {
                        fileName = autoDeployFolder + "Master.exe";
                        string cmd = "/c " + fileName;
                        var processInfo = new ProcessStartInfo("cmd.exe", cmd);

                        var process = new System.Diagnostics.Process();
                        process.StartInfo.FileName = fileName;
                        process.StartInfo.WorkingDirectory = autoDeployFolder;
                        process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.RedirectStandardError = true;
                        process.StartInfo.UseShellExecute = false;
                        process.Start();
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();
                    }
                    catch (Exception ex)
                    {
                        buildLog.Add("FAILED: ...............");
                        buildLog.Add(ex.Message);
                        buildLog.Add(ex.StackTrace);
                    }
                }
                else
                {
                    results = "Cannot find config";
                }
            }
            catch (Exception ex)
            {
                results = ex.Message;
            }

            return results;
        }
    }
}
