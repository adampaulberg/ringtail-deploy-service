using Deployer.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deployer.App
{
    public class DeploymentRunner
    {
        internal static void ReadFromFile(Logger log, Options options)
        {
            FileInfo fi = new FileInfo(options.DeployFile);

            if (fi.Exists)
            {
                var filesToDeploy = SimpleFileReader.Read(options.DeployFile);

                log.AddToLog("Read file: " + filesToDeploy.Count);

                List<string> myBatch = new List<string>();
                string batchPath = "runDeployments.bat";
                int i = 0;
                foreach (var x in filesToDeploy)
                {
                    log.AddToLog("About to deploy: " + x);
                    log.Write("DeployLog.txt");
                    log.AddToLog(RunDeployment(x, myBatch));
                    log.Write("DeployLog.txt");


                    i++;
                }
                SimpleFileWriter.Write(batchPath, myBatch);

                RunFile(batchPath);
            }
            else
            {
                log.AddToLog(fi.FullName + " not found");
            }
        }

        private static void RunFile(string file)
        {
            Console.WriteLine(file);
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = " /c " + file;
            process.StartInfo.WorkingDirectory = @"C:\upgrade\autodeploy";
            process.StartInfo.CreateNoWindow = false;

            process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            process.Start();
            process.WaitForExit();
        }

        public static List<string> RunDeployment(string file, List<string> myBatch)
        {
            var log = new List<string>();
            log.Add(" attempting to run deployment: " + file);

            try
            {
                string deploymentCommandFile = string.Empty;

                FileInfo fi = new FileInfo(file);
                if (fi.Exists)
                {
                    var contents = SimpleFileReader.Read(file);

                    DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(file));

                    foreach (var x in di.GetFiles())
                    {
                        if (x.Name.Contains("postDeploy") && x.Name.Contains(".bak"))
                        {
                            x.Delete();
                        }
                        else if (x.Name.Contains("postDeploy"))
                        {
                            try
                            {
                                FileInfo info = new FileInfo(x.FullName + ".bak");
                                if (!info.Exists)
                                {
                                    x.CopyTo(x.FullName + ".bak", false);
                                }
                            }
                            catch
                            {
                                log.Add(" couldn't create a postDeploy backup file, but continuing anyway");
                            }
                            RegistryReaderScrubber.EndToEnd(x.FullName, x.Name);
                        }
                    }

                    List<string> noPause = new List<string>();
                    foreach (var x in contents)
                    {

                        if (!x.ToLower().Contains("pause"))
                        {
                            noPause.Add(x);
                        }
                    }
                    string fileName = file.Substring(0, file.Length - 4) + "_noPause.cmd";
                    SimpleFileWriter.Write(fileName, noPause);
                    deploymentCommandFile = fileName;
                }

                string workingPath = Path.GetDirectoryName(file);
                string workingFile = Path.GetFileNameWithoutExtension(file);
                string commandFile = Path.GetFileNameWithoutExtension(deploymentCommandFile);

                myBatch.Add("cd \"" + workingPath +"\"");
                myBatch.Add(@"%comspec% /c " + commandFile);

                log.Add("...reading from path: " + workingPath);
            }
            catch (Exception ex)
            {
                log.Add("Process could not be started...");
                log.Add(ex.Message);
                log.Add(ex.StackTrace);
            }


            return log;

        }
    }
}
