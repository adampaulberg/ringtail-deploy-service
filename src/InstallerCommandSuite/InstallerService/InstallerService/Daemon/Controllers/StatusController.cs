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
    public class StatusController : BaseController
    {

        [HttpGet]
        public string GetConfig()
        {
            return ReadConfig();
        }

        private static string ReadConfig()
        {
            string results = string.Empty;
            var workingDirectory = string.Empty;
            var fileName = string.Empty;
            try
            {
                FileInfo fi = new FileInfo(EnvironmentInfo.CONFIG_LOCATION);
                if (fi.Exists)
                {
                    var x = SimpleFileReader.Read(EnvironmentInfo.CONFIG_LOCATION);
                    workingDirectory = x[0].Split('|')[1];

                    fileName = workingDirectory + "buildOutput.txt";
                    fi = new FileInfo(fileName);

                    if (fi.Exists)
                    {
                        string copy = workingDirectory + "buildOutputCopy.txt";
                        FileInfo fi2 = new FileInfo(copy);
                        if (fi2.Exists)
                        {
                            fi2.Delete();
                        }
                        fi.CopyTo(copy);

                        var s = SimpleFileReader.Read(copy);

                        foreach (var str in s)
                        {
                            results += "<p>" + str + "</p>";
                        }
                    }
                    else
                    {
                        results = "Cannot find " +  fileName;
                    }
                }
                else
                {
                    results = "Cannot find config";
                }
            }
            catch (Exception ex)
            {
                results = ex.Message + " " + fileName;
            }

            return results;
        }

    }


}
