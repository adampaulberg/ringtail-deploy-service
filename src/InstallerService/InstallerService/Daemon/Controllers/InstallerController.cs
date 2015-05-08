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
    public class InstallerController : BaseController
    {
        [HttpGet]
        public string GetRunInstall()
        {
            string results = string.Empty;
            var installerServiceFolder= EnvironmentInfo.INSTALLER_SERVICE_WORKING_FOLDER;
            var autoDeployFolder = EnvironmentInfo.GetAutoDeploySuiteFolder();
            var config = EnvironmentInfo.InstallerServiceConfig();
            var fileName = string.Empty;
            var buildLog = new List<string>();
            var LOCKFILE = installerServiceFolder + "LOCK_BUILDS.txt";
            string username = null, password = null;

            try
            {
                if (config.ContainsKey(EnvironmentInfo.KeyMasterRunnerUser) && config.ContainsKey(EnvironmentInfo.KeyMasterRunnerPass))
                {
                    username = config[EnvironmentInfo.KeyMasterRunnerUser];
                    password = config[EnvironmentInfo.KeyMasterRunnerPass];
                }

                FileInfo lockFile = new FileInfo(LOCKFILE);
                if (lockFile.Exists)
                {
                    var x = SimpleFileReader.Read(LOCKFILE);
                    results = "There is a build currently running.....";

                    foreach (var str in x)
                    {
                        results += "<p>" + str + "</p>";
                    }
                    return results;
                }

                
                buildLog.Add("Build started: " + DateTime.Now);

                //if (Request != null)
                //{
                //    if (Request.Properties != null)
                //    {
                //        foreach (var x in Request.Properties.Keys)
                //        {
                //            buildLog.Add("K: " + x + " v: " + Request.Properties[x].ToString());
                //        }
                //    }
                //}

                buildLog.Add("Invoking Master.exe at: " + autoDeployFolder + "Master.exe" + DateTime.Now);
                FileHelpers.SimpleFileWriter.Write(LOCKFILE, buildLog);

                FileInfo fi = new FileInfo(EnvironmentInfo.CONFIG_LOCATION);
                if (fi.Exists)
                {
                    try
                    {
                        fileName = autoDeployFolder + "Master.exe";
                        string cmd = "/c " + fileName;
                        var processInfo = new ProcessStartInfo("cmd.exe", cmd);

                        var process = new System.Diagnostics.Process();
                        process.StartInfo.FileName = fileName;
                        process.StartInfo.WorkingDirectory = autoDeployFolder;
                        process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.RedirectStandardError = true;
                        process.StartInfo.UseShellExecute = false;
                        process.Start();
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();

                        int exitCode = process.ExitCode;

                        if (exitCode != 0)
                        {
                            results += ReadMasterLog();
                            output.Split('\n').ToList().ForEach(x => results += "<p>" + x + "</p>");
                            results += "<p>UPGRADE ABORTED</p>";                                                      
                            WriteBuildOutput(results);
                            DeleteFileLock();  
                            return results;
                        }

                        buildLog.Add("Cmd target: " + process.StartInfo.WorkingDirectory);
                        buildLog.Add("Cmd file: " + process.StartInfo.FileName);
                        buildLog.Add("Cmd errors: " + error);
                        buildLog.Add("Cmd output: " + output);
                        buildLog.Add("Cmd is: " + cmd);
                        buildLog.Add("Invoking master.bat at: " + autoDeployFolder + "master.bat at " + DateTime.Now);

                        System.Threading.Thread.Sleep(3000);

                        DateTime dt = DateTime.Now;
                        bool batchFileExists = new FileInfo(autoDeployFolder + "master.bat").Exists;

                        while (!batchFileExists)
                        {
                            buildLog.Add("....master.bat file does not exist...." + DateTime.Now);
                            batchFileExists = new FileInfo(autoDeployFolder + "master.bat").Exists;
                            TimeSpan ts = new TimeSpan(DateTime.Now.Ticks - dt.Ticks);

                            if (ts.TotalSeconds > 8)
                            {
                                DeleteFileLock();
                                results += ReadMasterLog();
                                WriteBuildOutput(results);

                                results += "<p>Master.bat write timeout.</p><p>UPGRADE ABORTED</p>";
                                return results;
                            }
                        }

                        buildLog.Add("...................master.bat contents...............");
                        var masterBatContents = SimpleFileReader.Read(EnvironmentInfo.GetAutoDeploySuiteFolder() + "master.bat");
                        buildLog.AddRange(masterBatContents);
                        buildLog.Add("...................<end> master.bat contents...............");

                        FileHelpers.SimpleFileWriter.Write(LOCKFILE, buildLog);

                        fileName = autoDeployFolder + "masterRunner.exe";
                        process = new System.Diagnostics.Process();
                        process.StartInfo.FileName = fileName;

                        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                        {
                            buildLog.Add(string.Format("Excuting masterRunner.exe -f master.bat -u \"{0}\" -p \"{1}\"", username, "password"));
                            process.StartInfo.Arguments = string.Format("-f master.bat -u \"{0}\" -p \"{1}\"", username, password);
                        }
                        else
                        {
                            buildLog.Add("Excuting masterRunner.exe -f master.bat");
                            process.StartInfo.Arguments = "-f master.bat";
                        }
                        process.StartInfo.WorkingDirectory = autoDeployFolder;
                        process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
                        process.Start();

                        results += AppendFileToResults(autoDeployFolder + "buildOutput.txt");
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
                FileHelpers.SimpleFileWriter.Write(LOCKFILE, buildLog);
                DeleteFileLock();
            }
            catch (Exception ex)
            {
                results = ex.Message + " " + fileName;
                buildLog.Add("Build failed: " + DateTime.Now);
                FileInfo fi = new FileInfo(LOCKFILE);
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
            return dt.Month + "." + dt.Day + "."  + dt.Hour + dt.Minute + "." + dt.Second;
        }

        private static string ReadMasterLog()
        {            
            var autoDeployFolder = EnvironmentInfo.GetAutoDeploySuiteFolder();            
            var masterLogPath = autoDeployFolder + "MasterLog.txt";

            string result = string.Empty;
            try
            {
                using (StreamReader sr = new StreamReader(masterLogPath))
                {
                    result = sr.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                result = string.Format("Failed to read {0} with execption: {1}", masterLogPath, ex.ToString());
            }
            return result;
        }

        private static void WriteBuildOutput(string text)
        {
            var autoDeployFolder = EnvironmentInfo.GetAutoDeploySuiteFolder();            
            var buildOutputPath = autoDeployFolder + "buildOutput.txt";            
            try
            {
                using(StreamWriter sw = new StreamWriter(buildOutputPath))
                {
                    sw.Write(text);
                }
            }
            catch(Exception)
            {
                // meh... we're screwed
            }
        }
    }
}
