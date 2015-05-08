using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace InstallerService.Daemon.Controllers
{
	public class InstallCertificateController:BaseController
	{
		[HttpGet]
        public string GetRunInstall(string CertificatePath, string Password)
        {
			string results = string.Empty;
			var autoDeployFolder = EnvironmentInfo.GetAutoDeploySuiteFolder();
			string fileName = autoDeployFolder + "RingtailCertificate.exe";
			string cmd = "/c " + fileName;
			var processInfo = new ProcessStartInfo("cmd.exe", cmd);

			var process = new System.Diagnostics.Process();
			process.StartInfo.FileName = fileName;
			process.StartInfo.Arguments = CertificatePath + " " + Password;
			process.StartInfo.WorkingDirectory = autoDeployFolder;
			process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.UseShellExecute = false;
			process.Start();
			
			string output = process.StandardOutput.ReadToEnd();
			string error = process.StandardError.ReadToEnd();
			return output;
		}
	}
}
