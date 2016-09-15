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

                    var retryMax = 100;         // i know - this is a really high number.  our IIS deployments are junk, and sometimes the big ones take a bunch of times to work right.
                                                // under the hood, they're writing to applicationHost.config at the same time IIS is trying to write to the same file and it's a 100kb file.
                                                // and that causes the OS to be sad.
                    var fileName = x.Split('\\');
                    var friendlyName = fileName[fileName.Length - 1];

                    Console.WriteLine(" " + friendlyName + "   " + (i + 1) + " of " + filesToDeploy.Count);

                    int j = 1;
                    ProcessOutcome exitInfo = null;
                    for (; j <= retryMax; j++)
                    {

                        List<string> myBatch = new List<string>();
                        string batchPath = "runDeployments-" + i + ".bat";

                        log.AddAndWrite("About to deploy: " + x, logFileName);
                        var batchContents = GenerateDeploymentCommand(x, myBatch);  // writes to myBatch.   bad API here, sorry.
                        log.AddAndWrite(batchContents, logFileName);

                        SimpleFileWriter.Write(batchPath, myBatch);

                        exitInfo = RunFile(batchPath, x, log);

                        if (exitInfo.ExitCode == 0)
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

                            if (exitInfo != null)
                            {
                                Console.WriteLine(exitInfo.Error);
                                break;
                            }
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


        private static ProcessOutcome RunFile(string file, string appFullName, Logger log)
        {
            int exitCode = 0;

            bool ranOk = true;

            StringBuilder errorText = new StringBuilder();

            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();

            using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
            using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
            {
                using (var process = new System.Diagnostics.Process())
                {
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.Arguments = " /c " + file;
                    process.StartInfo.WorkingDirectory = Environment.CurrentDirectory;
                    process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.UseShellExecute = false;
                    process.Start();


                    //http://stackoverflow.com/questions/139593/processstartinfo-hanging-on-waitforexit-why
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            outputWaitHandle.Set();
                        }
                        else
                        {
                            if (e.Data.StartsWith("Total"))
                            {
                                string s = e.Data.Length > 30 ? e.Data.Substring(0, 25) + "..." : e.Data;
                                s = s.Split('(')[0];
                                output.AppendLine(" " + s);
                            } 
                        }
                    };
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            errorWaitHandle.Set();
                        }
                        else
                        {
                            if (e.Data.StartsWith("The system"))
                            {
                                exitCode = 0;
                                ranOk = true;
                            }
                            else
                            {
                                log.AddToLog(" " + e.Data);
                                

                                if(e.Data.StartsWith("Error:" ))
                                {
                                    string tmp = e.Data.Replace("Error:", string.Empty);
                                    error.AppendLine(tmp);
                                }
                                else
                                {
                                    error.AppendLine(e.Data);
                                }


                                if (e.Data.StartsWith("Error count:"))
                                {
                                    ranOk = false;
                                    exitCode = 2;
                                }
                            }
                        }
                    };

                    Stopwatch sw = new Stopwatch();
                    sw.Start();

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    int timeout = 300000;  // 5 minutes for each step out to be enough for anybody.


                    if (process.WaitForExit(timeout) &&
                        outputWaitHandle.WaitOne(timeout) &&
                        errorWaitHandle.WaitOne(timeout))
                    {
                        // Process completed.
                        if (!ranOk)
                        {
                            exitCode = 2;
                        }
                    }
                    else
                    {
                        // Timed out.
                        log.AddToLog("* timed out....");
                        log.AddToLog("*process took longer than " + timeout + "  ms");
                        return new ProcessOutcome("Timed out", string.Empty, 1001, false);
                    }


                    if (process.ExitCode == 0 && ranOk)
                    {
                        Console.WriteLine( " " + output.ToString().TrimEnd());
                    }
                    else
                    {
                        //Error: The process cannot access 'C:\\inetpub\\wwwroot\\Coordinator\\312fca79-5f6c-4bc2-820b-59466cedf3d7trace.log' because it is being used by another process.
                        var fileName = appFullName.Split('\\');
                        var str = fileName[fileName.Length - 1];
                        var data = error.ToString();

                        errorText.AppendLine("  Problem with: " + str);
                        errorText.AppendLine("  ERROR - The IIS deployment for " + str + " did not run correctly.");
                        errorText.AppendLine("  " + data);
                    }
                }
            }

            ProcessOutcome outcome = new ProcessOutcome(errorText.ToString(), String.Empty, exitCode, ranOk);


            return outcome;
        }

        public static List<string> GenerateDeploymentCommand(string file, List<string> myBatch)
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
