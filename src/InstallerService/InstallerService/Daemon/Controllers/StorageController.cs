using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Diagnostics;

namespace InstallerService.Daemon.Controllers
{
    public class StorageController : BaseController
    {
        [HttpGet]
        public bool GetIsStorageSpaceAvailable()
        {
            return GetAvailableStorage();
        }

        [HttpGet]
        public bool GetIsStorageSpaceAvailable(string minGB)
        {
            int tempMinGB = int.Parse(minGB);
            return GetAvailableStorage(tempMinGB);
        }

        private static bool GetAvailableStorage(int minGB = 10)
        {
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            foreach (DriveInfo d in allDrives)
            {
                if (d.Name == "C:\\")
                {
                    long freeGB = d.AvailableFreeSpace / (1024 * 1024 * 1024);
                    if(freeGB >= minGB)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
