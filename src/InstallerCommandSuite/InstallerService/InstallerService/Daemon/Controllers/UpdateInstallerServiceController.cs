using InstallerService.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace InstallerService.Daemon.Controllers
{
    public class UpdateInstallerServiceController : BaseController
    {

        [HttpGet]
        public string UpdateInstallerService()
        {
            string results = string.Empty;
            List<string> log = new List<string>();
            var workingDirectory = EnvironmentInfo.INSTALLER_SERVICE_WORKING_FOLDER;
            var fileName = workingDirectory + "UpdateInstallerService.exe";
            var pullLocation = FileHelpers.ReadConfig("upgrade.config", EnvironmentInfo.INSTALLER_SERVICE_WORKING_FOLDER);

            log.Add("UpdateInstallerService started: " + DateTime.Now);
            
            log.Add("Pulling from: " + pullLocation);
            log.Add("Running: " + fileName);
            
            try
            {

                log.Add("Deleting file locks: " + EnvironmentInfo.INSTALLER_SERVICE_WORKING_FOLDER);
                DirectoryInfo di = new DirectoryInfo(EnvironmentInfo.INSTALLER_SERVICE_WORKING_FOLDER);
                var files = di.GetFiles("LOCK_BUILDS*.*");

                for(int i = 0; i < files.Length; i++)
                {
                    files[i].Delete();
                }
                

                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = fileName;
                process.StartInfo.Arguments = "";
                process.StartInfo.WorkingDirectory = workingDirectory;
                process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
                process.Start();

                results = FileHelpers.ConvertToString(log);
            }
            catch (Exception ex)
            {
                results = "<p>Could not locate Updater at:  " + fileName + "</p>";
                results += "<p>" + ex.Message + "</p>";
                results += "<p>" + ex.StackTrace + "</p>";
            }
            return results;
        }


        [HttpGet]
        public string ChangeUpdateServiceSourceFolder(string value)
        {
            string results = string.Empty;
            string file = EnvironmentInfo.INSTALLER_SERVICE_WORKING_FOLDER + @"upgrade.config";
            string key = "DROP_FOLDER";

            try
            {
                DirectoryInfo di = new DirectoryInfo(value);

                if (di.Exists)
                {
                    string message = FileHelpers.RAW_UpdateFileAtPath(file, key, value);
                    results += "<p>........Action..........." + message + "</p>";
                }
                else
                {
                    results += "<p>" + value + " is not a valid drop folder location because the folder does not exist.</p>";
                }

                results += FileHelpers.ReadConfig("upgrade.config", EnvironmentInfo.INSTALLER_SERVICE_WORKING_FOLDER);
            }
            catch (Exception ex)
            {
                results += ex.Message;
            }

            return results;
        }



    }

}
