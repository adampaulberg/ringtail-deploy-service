using System.IO;
using System.Collections.Generic;
using System.Web.Http;
using System.Dynamic;

namespace InstallerService.Daemon.Controllers
{
    public class PrerequisiteController : BaseController
    {
        [HttpGet]
        public ExpandoObject GetValidate()
        {
            return TestRules();
        }

        [HttpGet]
        public ExpandoObject GetValidate(string minGB)
        {
            int tempMinGB = int.Parse(minGB);
            return TestRules(tempMinGB);
        }

        private static ExpandoObject TestRules(int minGB = 3)
        {
            dynamic output = new ExpandoObject();
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            List<ExpandoObject> driveData = new List<ExpandoObject>();
            List<ExpandoObject> errorData = new List<ExpandoObject>();

            output.success = true;

            foreach (DriveInfo d in allDrives)
            {
                if(d.IsReady)
                {
                    long freeGB = d.AvailableFreeSpace / (1024 * 1024 * 1024);

                    dynamic drive = new ExpandoObject();
                    drive.Name = d.Name;
                    drive.freeGB = freeGB;

                    if(freeGB >= minGB)
                    {
                        drive.success = true;
                    } else
                    {
                        output.success = false;
                        drive.success = false;

                        dynamic error = new ExpandoObject();
                        error.description = drive.Name + " has less than the required " + minGB.ToString() + "GB required";
                        error.type = "DRIVESTORAGELOW";
                        errorData.Add(error);
                    }

                    driveData.Add(drive);
                }
            }

            output.drives = driveData;
            output.errors = errorData;

            return output;
        }
    }
}
