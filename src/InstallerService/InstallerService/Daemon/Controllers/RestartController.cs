using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace InstallerService.Daemon.Controllers
{
    public class RestartController : BaseController
    {

        [HttpGet]
        public bool GetValidate()
        {
            StartShutDown("-f -r");
            return true;
        }

        public void StartShutDown(string param)
        {
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.FileName = "cmd";
            proc.WindowStyle = ProcessWindowStyle.Hidden;
            proc.Arguments = "/C shutdown " + param;
            Process.Start(proc);
        }

    }
}
