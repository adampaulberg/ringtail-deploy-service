using MasterRunner.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MasterRunner.App
{
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

    public class ProcessExecutorHelper
    {
        Logger logger;
        List<string> allowedExceptions = new List<string>();
        List<string> timeoutList = new List<string>();
        int defaultTimeout = 0;

        public ProcessExecutorHelper(Logger logger, List<string> allowedExceptions, List<string> timeoutList, int defaultTimeout)
        {
            this.logger = logger;
            this.allowedExceptions = allowedExceptions;
            this.timeoutList = timeoutList;
            this.defaultTimeout = defaultTimeout;
        }

        public int SpawnAndLog(string command, string workingFolder, string username, string password, string headerInfo)
        {
            int exitCode = 0;
            try
            {
                logger.AddAndWrite("-----------");
                logger.AddAndWrite(headerInfo);
                logger.AddAndWrite("* starting: " + workingFolder + command);
                //logger.AddAndWrite("* time: " + DateTime.Now);
                var result = SpawnProcess(command, workingFolder, username, password);

                if (result.ExitCode != 0 && !result.ExitOk)
                {
                    logger.AddAndWrite("* time: " + DateTime.Now);
                    logger.AddAndWrite("* Exited with code " + result.ExitCode);
                    exitCode = result.ExitCode;
                }
                else if (result.ExitCode !=0 && result.ExitOk)
                {
                    logger.AddAndWrite("* time: " + DateTime.Now);
                    logger.AddAndWrite("* Exited with code " + result.ExitCode);
                    logger.AddAndWrite("* ...but this is a whitelisted exit code for this command");
                    exitCode = 0;
                }
            }
            catch (Exception ex)
            {
                logger.AddAndWrite("* RunFile error - trying to run the process threw an exception.");
                logger.AddAndWrite(ex.Message);
                logger.AddAndWrite(ex.StackTrace);
                logger.AddAndWrite("* time: " + DateTime.Now);
                logger.AddAndWrite("* Exited with code 2");
                exitCode = 2;
            }

            if (exitCode != 0)
            {
                logger.AddAndWrite("* finished: " + workingFolder + command);
                logger.AddAndWrite("* time: " + DateTime.Now);
            }

            return exitCode;
        }

        private ProcessOutcome SpawnProcess(string commandName, string workingDirectory, string username, string password)
        {
            var index = commandName.IndexOf(' ');

            string file = commandName;
            string args = string.Empty;
            if (index != -1)
            {
                file = commandName.Substring(0, index);
                args = commandName.Substring(index + 1, commandName.Length - index - 1);
            }

            //logger.AddToLog("*cmd: " + file);
            //logger.AddToLog("*args: " + args);


            //logger.AddAndWrite("* ...now running... ");


            string cmd = "/c " + workingDirectory + commandName;
            var processInfo = new ProcessStartInfo("cmd.exe", cmd);
            var exitCode = 0;
            bool exitOk = false;
            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();


            bool suppressNoise = commandName.Contains("fetch-");

            using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
            using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
            {
                using (Process process = new Process())
                {

                    process.StartInfo.FileName = file;
                    process.StartInfo.WorkingDirectory = workingDirectory;
                    process.StartInfo.Arguments = args;
                    process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.UseShellExecute = false;

                    if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                    {
                        var nameParts = username.Split('\\');
                        string domain = null;
                        string user = null;
                        if (nameParts.Length == 2)
                        {
                            domain = nameParts[0];
                            user = nameParts[1];
                        }
                        else
                        {
                            user = username;
                        }

                        process.StartInfo.Domain = domain;
                        process.StartInfo.UserName = user;
                        SecureString securePassword = new SecureString();
                        foreach (char c in password.ToCharArray()) securePassword.AppendChar(c);
                        process.StartInfo.Password = securePassword;
                    }

                    //logger.AddAndWrite("* started");


                    var timeout = ProcessExecutorHelper.GetTimeoutLength(commandName, timeoutList, logger, defaultTimeout);

                    logger.AddAndWrite("* Output Text: ");

                    //http://stackoverflow.com/questions/139593/processstartinfo-hanging-on-waitforexit-why
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            outputWaitHandle.Set();
                        }
                        else
                        {
                            if (!suppressNoise)
                            {
                                logger.AddAndWrite("  " + e.Data);
                                output.AppendLine("  " + e.Data);
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
                            logger.AddAndWrite("* Error Text: ");
                            logger.AddAndWrite("  " + e.Data);
                            error.AppendLine("  " + e.Data);
                        }
                    };

                    Stopwatch sw = new Stopwatch();
                    sw.Start();

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    if (timeout != 0)
                    {
                        if (process.WaitForExit(timeout) &&
                            outputWaitHandle.WaitOne(timeout) &&
                            errorWaitHandle.WaitOne(timeout))
                        {
                            // Process completed.

                            if (suppressNoise)
                            {
                                logger.AddAndWrite("  Finished - OK");
                            }


                            sw.Stop();
                            if (sw.ElapsedMilliseconds > 10000)
                            {
                                // show .00 place if it's less than 10 seconds, otherwise just show integer seconds.
                                var elapsed = sw.ElapsedMilliseconds > 10000 ? Math.Floor(sw.ElapsedMilliseconds / 1000m) : Math.Round(sw.ElapsedMilliseconds / 1000m, 2);
                                logger.AddAndWrite("* finished in " + elapsed + " s");
                            }
                            exitCode = process.ExitCode;
                        }
                        else
                        {
                            // Timed out.
                            logger.AddAndWrite("* timed out....");
                            logger.AddAndWrite("* process took longer than " + timeout + "  ms");
                            return new ProcessOutcome("", "", 1001, false);
                        }
                    }
                    else
                    {
                        // Process completed.
                        process.WaitForExit();

                        sw.Stop();
                        if (sw.ElapsedMilliseconds > 10000)
                        {
                            // show .00 place if it's less than 10 seconds, otherwise just show integer seconds.
                            var elapsed = sw.ElapsedMilliseconds > 10000 ? Math.Floor(sw.ElapsedMilliseconds / 1000m) : Math.Round(sw.ElapsedMilliseconds / 1000m, 2);
                            logger.AddAndWrite("* finished in " + elapsed + " s");
                        }
                        exitCode = process.ExitCode;

                    }

                    if (process.ExitCode != 0)
                    {
                        logger.AddAndWrite("* ran with exit code: " + process.ExitCode);
                    }



                    if (process.ExitCode > 0)
                    {
                        var allowedException = this.allowedExceptions.Find(a => a.Contains(commandName));
                        if (!String.IsNullOrEmpty(allowedException))
                        {
                            exitOk = true;
                        }

                    }
                    exitCode = process.ExitCode;
                }
            }

            return new ProcessOutcome(error.ToString(), output.ToString(), exitCode, exitOk);
        }


        public static int GetTimeoutLength(string commandName, List<string> timeoutList, Logger logger, int defaultTimeout)
        {
            try
            {
                if (timeoutList != null && timeoutList.Count > 0)
                {
                    var timeout = timeoutList.Find(x => x.Split('|')[0] == commandName);

                    if (timeout != null && timeout.Length > 0)
                    {
                        logger.AddToLog(" found special timeout: " + timeout);
                        var customTimeout = Convert.ToInt32(timeout.Split('|')[1]);
                        return customTimeout;
                    }
                }
            }
            catch(Exception ex)
            {
                logger.AddToLog("...exception reading timeouts... defaulting to " + defaultTimeout + " ms");
                logger.AddToLog("       " + ex.Message);
            }

            return defaultTimeout;
        }
    }
}
