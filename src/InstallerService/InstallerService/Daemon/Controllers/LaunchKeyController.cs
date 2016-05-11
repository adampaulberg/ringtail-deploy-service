using InstallerService.Helpers;
using System;
using System.Diagnostics;
using System.Web.Http;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;


namespace InstallerService.Daemon.Controllers
{
    public class LaunchKeyController : BaseController
    {

        [HttpGet]
        public string ClearLaunchKeys()
        {
            return ChangeConfig("&&LaunchKey", "", true);
        }

        [HttpGet]
        public string AddLaunchKey(string launchKey)
        {
            return ChangeConfig("LaunchKey|" + launchKey, launchKey);
        }

        private string ChangeConfig(string key, string value, bool deleteAllowed = false)
        {
            string results = string.Empty;

            try
            {
                if (value.Contains(" "))
                {
                    value = "\"\"\"" + value + "\"\"\"";
                }

                string message = FileHelpers.ChangeConfigItemWithWildcards("volitleData.config", key, value, deleteAllowed);
                results += "<p>............Action.........." + message + "</p>";
                results += FileHelpers.ReadConfig("volitleData.config");
            }
            catch (Exception ex)
            {
                results = ex.Message;
            }

            return results;
        }
    }
}
