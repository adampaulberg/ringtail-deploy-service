using InstallerService.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace InstallerService.Daemon.Controllers
{
    public class UninstallerController : BaseController
    {
        [HttpGet]
        public string Uninstall()
        {
            return GetUninstallStatus();
        }

        private static string GetUninstallStatus()
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

                    fileName = workingDirectory + "uninstall.log";
                    fi = new FileInfo(fileName);

                    if (fi.Exists)
                    {
                        string copy = workingDirectory + "uninstallCopy.log";
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
                        results = "This api is to check on actively running uninstalls.... there doesn't appear to be an actively running uninstall: " + fileName;
                    }
                }
                else
                {
                    results = "Cannot find config - try running UpdateInstallerService";
                }
            }
            catch (Exception ex)
            {
                results = ex.Message + " " + fileName;
            }

            return results;
        }

        [HttpGet]
        public string Uninstall(string app)
        {
            return GenerateUninstallationsAndExecuteThem(app);
        }

        private static string GenerateUninstallationsAndExecuteThem(string app)
        {
            // this just runs master.exe so you can remotely query and inspect the resulting master.bat
            string results = string.Empty;
            var installerServiceFolder = EnvironmentInfo.INSTALLER_SERVICE_WORKING_FOLDER;
            var env = EnvironmentInfo.GetAutoDeploySuiteFolder();
            var fileName = string.Empty;
            var buildLog = new List<string>();

            if (app == string.Empty)
            {
                return "Please provide an application name to uninstall";
            }

            try
            {
                FileInfo fi = new FileInfo(EnvironmentInfo.CONFIG_LOCATION);
                if (fi.Exists)
                {
                    try
                    {
                        results += "<p>Starting... UninstallerHelper.exe</p>";

                        ProcessHelper helper = new ProcessHelper();
                        results += CreateUninstallBat(app, ref helper);
                        

                        System.Threading.Thread.Sleep(3000);

                        if (!new FileInfo(env + "uninstall.bat").Exists)
                        {
                            results += "Failure.... uninstall.bat does not exist";
                        }
                        else
                        {
                            results += "<p>Uninstalling the following.</p>";
                            var list = SimpleFileReader.Read(env + "uninstall.bat");
                            list.ForEach(x => results += "<p>..." + x + "</p>");

                            results += ExecuteUninstallations(ref helper);
                        }

                        results += GetUninstallStatus();
                    }
                    catch (Exception ex)
                    {
                        buildLog.Add("FAILED");
                        buildLog.Add(ex.Message);
                        buildLog.Add(ex.StackTrace);
                        results += "<p>Failed</p>";
                        results += "<p>Working folder: " + env + "</p>";
                        results += "<p>" + ex.Message + "</p>";
                        results += "<p>" + ex.StackTrace + "</p>";

                    }
                }
                else
                {
                    results = "Cannot find config - try running UpdateInstallerService";
                }
            }
            catch (Exception ex)
            {
                results = ex.Message;
            }

            return results;
        }

        private static string CreateUninstallBat(string app, ref ProcessHelper helper)
        {
            string results = string.Empty;
            var autoDeployFolder = EnvironmentInfo.GetAutoDeploySuiteFolder();
            try
            {
                string args = string.Empty;
                if (app != "*")
                {
                    args = app;
                }

                var init = autoDeployFolder + "UninstallerHelper.exe";
                Process process = new System.Diagnostics.Process();
                process.StartInfo.FileName = init;
                process.StartInfo.Arguments = args;
                process.StartInfo.WorkingDirectory = autoDeployFolder;
                process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
                process.Start();
            }
            catch (Exception ex)
            {
                results += "<p>Failed</p>";
                results += "<p>Working folder: " + autoDeployFolder + "</p>";
                results += "<p>" + ex.Message + "</p>";
                results += "<p>" + ex.StackTrace + "</p>";

                helper.logger.ForEach(x => results += "<p>..." + x + "</p>");
            }
            return results;
        }

        private static string ExecuteUninstallations(ref ProcessHelper helper)
        {
            var env = EnvironmentInfo.GetAutoDeploySuiteFolder();
            string results = "<p>Starting... MasterRunner.exe</p>";
            helper = new ProcessHelper();
            try
            {
                var init = env + "masterRunner.exe";
                Process process = new System.Diagnostics.Process();
                process.StartInfo.FileName = init;
                process.StartInfo.Arguments = "-f uninstall.bat -o uninstall.log";
                process.StartInfo.WorkingDirectory = env;
                process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
                process.Start();
            }
            catch (Exception ex)
            {
                results += "<p>Failed</p>";
                results += "<p>Working folder: " + env + "</p>";
                results += "<p>" + ex.Message + "</p>";
                results += "<p>" + ex.StackTrace + "</p>";

                helper.logger.ForEach(x => results += "<p>..." + x + "</p>");
            }

            return results;
        }

        private class ProcessHelper
        {
            public List<string> logger = new List<string>();
            public ProcessHelper()
            {

            }
            //public  ProcessOutcome SpawnProcess(string commandName, string workingDirectory)
            //{
            //    var index = commandName.IndexOf(' ');

            //    var autoDeployFolder = EnvironmentInfo.GetAutoDeploySuiteFolder();
            //    string file = commandName;
            //    string args = string.Empty;
            //    if (index != -1)
            //    {
            //        file = commandName.Substring(0, index);
            //        args = commandName.Substring(index + 1, commandName.Length - index - 1);
            //    }

            //    logger.Add("*cmd: " + file);
            //    logger.Add("*args: " + args);



            //    string cmd = "/c " + workingDirectory + commandName;
            //    logger.Add(" *trueCmd: " + cmd);
            //    var processInfo = new ProcessStartInfo("cmd.exe", cmd);

            //    var process = new System.Diagnostics.Process();
            //    process.StartInfo.FileName = file;
            //    process.StartInfo.WorkingDirectory = autoDeployFolder;
            //    process.StartInfo.Arguments = args;
            //    process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            //    process.StartInfo.RedirectStandardOutput = true;
            //    process.StartInfo.RedirectStandardError = true;
            //    process.StartInfo.UseShellExecute = false;
            //    process.Start();
            //    string output = process.StandardOutput.ReadToEnd();
            //    string error = process.StandardError.ReadToEnd();

            //    int exitCode = 1;

            //    try
            //    {
            //        exitCode = process.ExitCode;
            //    }
            //    catch(Exception ex)
            //    {
            //        logger.Add(ex.Message);
            //        logger.Add(ex.StackTrace);
            //        exitCode = 2;
            //    }

            //    logger.Add("*ran with exit code: " + exitCode);

            //    return new ProcessOutcome(error, output, exitCode);
            //}
        }

        //private class ProcessOutcome
        //{
        //    public int ExitCode { get; private set; }
        //    public string Output { get; private set; }
        //    public string Error { get; private set; }

        //    public ProcessOutcome(string error, string output, int exitCode)
        //    {
        //        this.ExitCode = exitCode;
        //        this.Error = error;
        //        this.Output = output;
        //    }
        //}

        

    }
}
