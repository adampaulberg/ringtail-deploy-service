using Deployer.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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

            if (fi.Exists)
            {
                var potentialFilesToDeploy = SimpleFileReader.Read(options.DeployFile);
                var filesToDeploy = new List<string>();

                log.AddToLog("Read file: " + potentialFilesToDeploy.Count);

                foreach(var x in potentialFilesToDeploy)
                {
                    fi = new FileInfo(x);
                    if(fi.Exists)
                    {
                        filesToDeploy.Add(x);
                    }
                    else
                    {
                        log.AddAndWrite("* This file wasn't present for this role: " + x , logFileName);
                    }
                }


                for(int i = 0; i < filesToDeploy.Count; i++)
                {
                    exit = 0;
                    var x = filesToDeploy[i];

                    var retryMax = 3;
                    var fileName = x.Split('\\');
                    var friendlyName = fileName[fileName.Length - 1];

                    Console.WriteLine(" " + friendlyName + "   " + (i + 1) + " of " + filesToDeploy.Count);

                    int j = 1;
                    int exitInfo = 0;
                    for (; j <= retryMax; j++)
                    {

                        List<string> myBatch = new List<string>();
                        string batchPath = "runDeployments-" + i + ".bat";

                        log.AddAndWrite("About to deploy: " + x, logFileName);
                        var batchContents = RunDeployment(x, myBatch);  // writes to myBatch.   bad API here, sorry.
                        log.AddAndWrite(batchContents, logFileName);

                        SimpleFileWriter.Write(batchPath, myBatch);


                        exitInfo = ProcessRunner.RunFileMOD(batchPath, x, log).ExitCode;


                        if (exitInfo == 0)
                        {
                            break;
                        }
                        else
                        {
                            int retryWaitTime = 10 * j;     // it turns out wait time between these is irrelevent.... it just needs to try a bunch of times until it gets lucky and works.
                            Console.WriteLine("  Retrying deployment " + j + " of " + retryMax + " attempts in " + retryWaitTime / 1000 + " seconds");

                            System.Threading.Thread.Sleep(retryWaitTime);  // wait progressively longer for the gremilns to go away.
                        }

                        if (j == retryMax)
                        {
                            exit = 2;
                        }
                    }

                    if(exit != 0)
                    {
                        break;
                    }

                    log.Write(logFileName);

                    
                }
            }
            else
            {
                log.AddToLog(fi.FullName + " not found");
                exit = 1;
            }

            if(exit != 0 )
            {
                Console.WriteLine("Verbose logs can be found here: " + logFileName);

                Logger correctiveActoins = new Logger();
                correctiveActoins.AddToLog("REBOOT");
                correctiveActoins.AddToLog(@"C:\Upgrade\AutoDeploy\retry.bat");
                correctiveActoins.Write("deployer-corrective.log");

                Console.WriteLine("** CORRECTIVE ACTION **");
                Console.WriteLine("   Reboot the machine.");
                Console.WriteLine(@"   After reboot run:  C:\Upgrade\AutoDeploy\masterRunner.exe -f retry.bat");
                Console.WriteLine("**                   **");
            }

            return exit;
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


    public class ProcessRunner
    {
        public static ProcessOutcome RunFileMOD(string file, string appFullName, Logger log)
        {
            int exitCode = 0;

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


            Logger outputLog = new Logger();
            outputLog.AddToLog("Output: ");
            outputLog.AddToLog(output);

            outputLog.AddToLog("Errors: ");
            outputLog.AddToLog(error);

            outputLog.Write("log-deploy-" + file.Split('.')[0] + ".txt");

            if (process.ExitCode != 0)
            {
                Console.WriteLine("* ran with exit code: " + process.ExitCode);
            }

            var lines = output.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None).ToList();

            foreach (var x in lines)
            {
                if (x.Contains("Total changes:"))
                {
                    Console.WriteLine(" " + x);
                }
            }

            var errorLines = error.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None).ToList();

            log.AddToLog("* Output: " + output);
            log.AddToLog("* Errors: \n" + error);

            if (!String.IsNullOrEmpty(error))
            {
                exitCode = 1;
                outputLog.AddToLog("* Errors: \n" + error);
            }

            outputLog.Write("log-deploy-" + file.Split('.')[0] + ".txt");

            ProcessOutcome po = new ProcessOutcome(string.Empty, string.Empty, exitCode, exitCode == 0);
            return po;
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
