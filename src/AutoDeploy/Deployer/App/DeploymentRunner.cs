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
        internal static int ReadFromFile(Logger log, Options options)
        {
            int exit = 0;
            FileInfo fi = new FileInfo(options.DeployFile);

            string logFileName = "log-" + options.DeployFile.Split('.')[0] + ".txt";

            Console.WriteLine("Verbose logs can be found here: " + logFileName);

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
                    log.Write(logFileName);
                    log.AddToLog(RunDeployment(x, myBatch));
                    log.Write(logFileName);


                    i++;
                }
                SimpleFileWriter.Write(batchPath, myBatch);

                exit = RunFile(batchPath, log);
                log.Write(logFileName);
            }
            else
            {
                log.AddToLog(fi.FullName + " not found");
                exit = 1;
            }

            return exit;
        }

        private static string RunFile_Legacy(string file)
        {
            Console.WriteLine(file);
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = " /c " + file;
            process.StartInfo.WorkingDirectory = Environment.CurrentDirectory;
            process.StartInfo.CreateNoWindow = false;

            process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            process.Start();
            process.WaitForExit();

            return "Ok";
        }

        private static int RunFile(string file, Logger log)
        {
            int exitCode = 0;
            Console.WriteLine(file);
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = " /c " + file;

            process.StartInfo.WorkingDirectory = Environment.CurrentDirectory;
            process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();


            Console.WriteLine("*ran with exit code: " + process.ExitCode);

            if (!String.IsNullOrEmpty(output))
            {
                var x = output.Split(new string[] { "Total changes" }, StringSplitOptions.None);
                foreach (var outputLine in x.ToList())
                {
                    int len = outputLine.Length > 100 ? 100 : outputLine.Length;
                    Console.WriteLine("Total changes" + outputLine.Substring(0, len) + " ...");
                }
            }

            Console.WriteLine(error);
            log.AddToLog("* Output: " + output);

            if (error.Length > 0)
            {
                string okError = "The system cannot find the path specified.";
                okError = okError.ToLower();
                if (error.TrimStart().TrimEnd().ToLower() == okError)
                {
                    log.AddToLog("* Warning: " + error);

                }
                else
                {
                    string helpText = "...tip: This often hapens when you try to deploy when the coordinator version does not match the database version.";
                    log.AddToLog("* Errors: " + error);
                    log.AddToLog(helpText);
                    exitCode = 1;
                }
            }
            else
            {
                exitCode = process.ExitCode;
            }

            return exitCode;
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

    public class ProcessOutcome
    {
        public int ExitCode { get; private set; }
        public string Output { get; private set; }
        public string Error { get; private set; }
        public bool ExitOk { get; private set; }

        public ProcessOutcome(string error, string output, int exitCode, bool exitOk)
        {
            this.ExitCode = exitCode;
            this.Error = error;
            this.Output = output;
            this.ExitOk = exitOk;
        }
    }
}
