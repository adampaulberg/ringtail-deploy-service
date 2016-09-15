using InstallerService.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace InstallerService.Daemon.Controllers
{
    public class RetryController : BaseController
    {
        [HttpGet]
        public string GetRunRetry()
        {
            string results = string.Empty;
            var installerServiceFolder = EnvironmentInfo.INSTALLER_SERVICE_WORKING_FOLDER;
            var autoDeployFolder = EnvironmentInfo.GetAutoDeploySuiteFolder();
            var config = EnvironmentInfo.InstallerServiceConfig();
            var fileName = string.Empty;
            var buildLog = new List<string>();
            string username = null, password = null;

            List<string> friendlyLog = new List<string>();

            try
            {
                if (config.ContainsKey(EnvironmentInfo.KeyMasterRunnerUser) && config.ContainsKey(EnvironmentInfo.KeyMasterRunnerPass))
                {
                    username = config[EnvironmentInfo.KeyMasterRunnerUser];
                    password = config[EnvironmentInfo.KeyMasterRunnerPass];
                }

                if (ProcessHelpers.IsMasterRunnerAlreadyRunning())
                {
                    results = "There is already a deployment in progress.....";
                    results = "Use api/stop if you want to stop that before running a retry.";
                    return results;
                }
                else
                {
                    friendlyLog.Add("Retrying from the last failure point.");
                    AppendToBuildOutput(friendlyLog);
                    friendlyLog.Clear();
                }


                FileInfo fi = new FileInfo(EnvironmentInfo.CONFIG_LOCATION);
                if (fi.Exists)
                {
                    try
                    {
                        DateTime dt = DateTime.Now;
                        bool batchFileExists = new FileInfo(autoDeployFolder + "retry.bat").Exists;

                        if(!batchFileExists)
                        {
                            friendlyLog.Add("retry.bat did not exist.");
                            friendlyLog.Add("UPGRADE FAILED");
                            AppendToBuildOutput(friendlyLog);
                            friendlyLog.Clear();
                            results += "<p>retry.bat did not exist.</p><p>UPGRADE ABORTED</p>";
                        }
                                

                        var masterBatContents = SimpleFileReader.Read(EnvironmentInfo.GetAutoDeploySuiteFolder() + "retry.bat");
                        
                        fileName = autoDeployFolder + "masterRunner.exe";
                        using (Process process = new System.Diagnostics.Process())
                        {
                            process.StartInfo.FileName = fileName;

                            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                            {
                                buildLog.Add(string.Format("Excuting masterRunner.exe -f retry.bat -l append -u \"{0}\" -p \"{1}\"", username, "password"));
                                process.StartInfo.Arguments = string.Format("-f retry.bat -l append -u \"{0}\" -p \"{1}\"", username, password);
                            }
                            else
                            {
                                buildLog.Add("Excuting masterRunner.exe -f retry.bat -l append");
                                process.StartInfo.Arguments = "-f retry.bat -l append";
                            }
                            process.StartInfo.WorkingDirectory = autoDeployFolder;
                            process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
                            process.Start();

                            results += "<p>OK</p>";
                        }
                    }
                    catch (Exception ex)
                    {
                        buildLog.Add("FAILED: ...............");
                        buildLog.Add(ex.Message);
                        buildLog.Add(ex.StackTrace);
                    }
                }
                else
                {
                    results = "<p>Installer Service not setup properly.  Couldn't find: " + EnvironmentInfo.CONFIG_LOCATION + "</p><p>UPGRADE ABORTED</p>";
                }

                buildLog.Add(results);
                buildLog.Add("Build completed: " + DateTime.Now);
            }
            catch (Exception ex)
            {
                results = ex.Message + " " + fileName;
                buildLog.Add("Build failed: " + DateTime.Now);
            }

            return results;
        }

        private void DeleteFileLock()
        {
            var installerServiceFolder = EnvironmentInfo.INSTALLER_SERVICE_WORKING_FOLDER;
            var LOCKFILE = installerServiceFolder + "LOCK_BUILDS.txt";
            var fi = new FileInfo(LOCKFILE);

            if (fi.Exists)
            {
                try
                {
                    string newFile = "__buildArchive." + ConvertDateToFileString(DateTime.Now) + "." + Guid.NewGuid().ToString().Substring(0, 10) + ".txt";
                    fi.CopyTo(installerServiceFolder + newFile);
                    fi.Delete();
                }
                catch
                {
                    fi.Delete();
                }
            }
        }

        private static string AppendFileToResults(string filePath)
        {
            FileInfo fi = new FileInfo(filePath);

            string results = string.Empty;
            if (fi.Exists)
            {
                var s = SimpleFileReader.Read(filePath);

                foreach (var str in s)
                {
                    results += "<p>" + str + "</p>";
                }
            }
            else
            {
                results = "Install started... check api/status to find out how it's going.";
            }

            return results;
        }

        private static string ConvertDateToFileString(DateTime dt)
        {
            return dt.Month + "." + dt.Day + "." + dt.Hour + dt.Minute + "." + dt.Second;
        }


        private static List<string> ReadBuildOutput()
        {
            var autoDeployFolder = EnvironmentInfo.GetAutoDeploySuiteFolder();
            var masterLogPath = autoDeployFolder + "buildOutput.txt";
            return SimpleFileReader.Read(masterLogPath);
        }

        private static void AppendToBuildOutput(List<string> s)
        {
            var autoDeployFolder = EnvironmentInfo.GetAutoDeploySuiteFolder();
            var masterLogPath = autoDeployFolder + "buildOutput.txt";
            var x = ReadBuildOutput();

            if(x == null)
            {
                x = new List<string>();
            }

            x.AddRange(s);

            FileHelpers.SimpleFileWriter.Write(masterLogPath, x);
        }

        public class SimpleFileReader
        {
            public static List<string> Read(string fileName)
            {
                List<string> s = new List<string>();

                FileInfo fi = new FileInfo(fileName);
                if (fi.Exists)
                {

                    using (StreamReader stream = new StreamReader(fileName))
                    {
                        string input = null;
                        while ((input = stream.ReadLine()) != null)
                        {
                            s.Add(input);
                        }
                    }
                }

                return s;
            }
        }
    }
}
