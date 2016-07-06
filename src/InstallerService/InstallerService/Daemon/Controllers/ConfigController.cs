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
    public class ConfigController : BaseController
    {
        [HttpGet]
        public string GetConfig()
        {
            return FileHelpers.ReadConfig("volitleData.config");
        }

        [HttpGet]
        public string GetConfig(string fileName)
        {
            if (fileName == "*")
            {
                return FileHelpers.ReadFolder();
            }

            return FileHelpers.ReadConfig(fileName);
        }

        [HttpGet]
        public string ChangeConfig(string key, string value)
        {
            string results = string.Empty;

            try
            {
                if (ProcessHelpers.IsMasterRunnerAlreadyRunning())
                {
                    results = @"<p>Deployment in progress, cannot change config at this time.</p>";
                }
                else
                {
                    string message = FileHelpers.ChangeConfigItemWithWildcards("volitleData.config", key, value);
                    results += "<p>............Action.........." + message + "</p>";
                    results += FileHelpers.ReadConfig("volitleData.config");
                }
            }
            catch (Exception ex)
            {
                results = ex.Message;
            }

            return results;
        }

        [HttpGet]
        public string ChangeConfig(string fileName, string key, string value)
        {
            string results = string.Empty;

            try
            {
                if (ProcessHelpers.IsMasterRunnerAlreadyRunning())
                {
                    results = @"<p>Deployment in progress, cannot change config at this time.</p>";
                }
                else
                {
                    string message = FileHelpers.ChangeConfigItemWithWildcards(fileName, key, value);
                    results += "<p>............Action.........." + message + "</p>";

                    if (!fileName.StartsWith("*"))
                    {
                        results += FileHelpers.ReadConfig(fileName);
                    }
                    else
                    {
                        results += "Multiple files potentially changed.";
                    }
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
