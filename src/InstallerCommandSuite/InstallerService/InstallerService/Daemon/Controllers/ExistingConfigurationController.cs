using InstallerService.Helpers;
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
    public class ExistingConfigurationController : BaseController
    {
        [HttpGet]
        public string BuildExistingConfiguration()
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
                    string output = string.Empty;
                    string error = string.Empty;
                    try
                    {
                        var process = ProcessHelpers.BuildProcessStartInfo(autoDeployFolder, "RegistryReader.exe");
                        process.Start();
                        output = process.StandardOutput.ReadToEnd();
                        error = process.StandardError.ReadToEnd();

                        if (error == string.Empty)
                        {
                            return FileHelpers.ReadConfig("currentMachine.config");
                        }
                    }
                    catch (Exception ex)
                    {
                        buildLog.Add("FAILED: ...............");
                        buildLog.Add(ex.Message);
                        buildLog.Add(ex.StackTrace);
                        buildLog.Add(error);
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
