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
    public class StopController : BaseController
    {
        [HttpGet]
        public HttpResponseMessage GetInstalledKeys()
        {
            HttpResponseMessage hr = new HttpResponseMessage(System.Net.HttpStatusCode.OK);

            var log = new List<string>();

            try
            {
                string message = "Its not running.";
                bool msiExecRunning = false;

                foreach (var process in Process.GetProcessesByName("msiexec"))
                {
                    message = "Stopped";
                    msiExecRunning = true;
                    process.Kill();
                }

                bool isDataCamelRunning = false;

                foreach (var process in Process.GetProcessesByName("DataCamel"))
                {
                    message = "Cannot stop while DataCamel is running - we don't want to risk corrupting your DB mid-upgrade.";
                    isDataCamelRunning = true;
                }


                if (msiExecRunning == false && !isDataCamelRunning)
                {
                    bool masterRunnerHalted = false;

                    // killing msiExec will cause MasterRunner to bail anyway, so might as well just let it end normally, so we get the logging.
                    foreach (var process in Process.GetProcessesByName("MasterRunner"))
                    {
                        message = "Stopped";
                        masterRunnerHalted = true;
                        process.Kill();
                    }

                    if (masterRunnerHalted)  // write out to the buildOutput file if we manually halt MasterRunner so downstream processes know it's finished (and failed).
                    {

                        string filePath = @"C:\Upgrade\AutoDeploy\buildOutput.txt";

                        var file = SimpleFileReader.Read(filePath);
                        file.Add("-----------");
                        file.Add("*time: " + DateTime.Now);
                        file.Add("*STOPPED by the http api.");
                        file.Add("UPGRADE FAILED");
                        FileHelpers.SimpleFileWriter.Write(filePath, file);
                        string alt = @"C:\Upgrade\AutoDeploy\buildOutputCopy.txt";
                        FileHelpers.SimpleFileWriter.Write(alt, file);
                    }
                }


                hr.Content = new StringContent(message, System.Text.Encoding.Default, "application/json");
            }
            catch (Exception ex)
            {
                log.Add(ex.Message);
                log.Add(ex.StackTrace);
                FileHelpers.SimpleFileWriter.Write(@"C:\Upgrade\InstallerService\StopControllerError.txt", log);

                var errorResponse = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
                errorResponse.Content = new StringContent("Error - see logs on server", System.Text.Encoding.Default, "application/text");
                return errorResponse;
            }

            return hr;
        }
    
    }
}
