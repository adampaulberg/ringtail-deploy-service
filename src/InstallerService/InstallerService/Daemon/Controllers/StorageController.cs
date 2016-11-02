using System.IO;
using System.Collections.Generic;
using System.Web.Http;
using System.Dynamic;

namespace InstallerService.Daemon.Controllers
{
    public class StorageController : BaseController
    {
        [HttpGet]
        public List<ExpandoObject> GetIsStorageSpaceAvailable()
        {
            return GetAvailableStorage();
        }

        [HttpGet]
        public List<ExpandoObject> GetIsStorageSpaceAvailable(string minGB)
        {
            int tempMinGB = int.Parse(minGB);
            return GetAvailableStorage(tempMinGB);
        }

        private static List<ExpandoObject> GetAvailableStorage(int minGB = 10)
        {
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            List<ExpandoObject> driveData = new List<ExpandoObject>();

            foreach (DriveInfo d in allDrives)
            {
                if(d.IsReady)
                {
                    long freeGB = d.AvailableFreeSpace / (1024 * 1024 * 1024);

                    dynamic obj = new ExpandoObject();
                    obj.Name = d.Name;
                    obj.freeGB = freeGB;

                    if(freeGB >= minGB)
                    {
                        obj.success = true;
                    } else
                    {
                        obj.success = false;
                    }

                    driveData.Add(obj);
                }
            }

            return driveData;
        }
    }
}
