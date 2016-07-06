using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstallerService.Helpers
{
    internal class ProcessHelpers
    {

        /// <summary>
        /// Constructs an unstarted process that is ready to go.  By default it will redicect output and errors.
        /// </summary>
        /// <param name="autoDeployFolder"></param>
        /// <param name="exeName"></param>
        /// <returns></returns>
        public static Process BuildProcessStartInfo(string workingFolder, string exeName)
        {
            string fileName = workingFolder + exeName;
            string cmd = "/c " + fileName;
            var processInfo = new ProcessStartInfo("cmd.exe", cmd);

            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = fileName;
            process.StartInfo.WorkingDirectory = workingFolder;
            process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;

            return process;
        }


        public static bool IsMasterRunnerAlreadyRunning()
        {
            var filtered = Process.GetProcesses().ToList().Where(x => x.ProcessName.ToLower().StartsWith("masterrunner"));
            return filtered.ToList().Count > 0;
        }
    }
}
