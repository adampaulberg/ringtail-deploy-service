﻿using MasterRunner.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Text;
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

        public int SpawnAndLog(string command, string workingFolder, string username, string password)
        {
            int exitCode = 0;
            try
            {
                logger.AddAndWrite("-----------");
                logger.AddAndWrite("*starting: " + workingFolder + command);
                logger.AddAndWrite(" *time: " + DateTime.Now);
                var result = SpawnProcess(command, workingFolder, username, password);

                logger.AddAndWrite("*Output text: ");
                logger.AddAndWrite(result.Output);

                if (result.ExitCode != 0 && !result.ExitOk)
                {
                    logger.AddAndWrite("*Error text: ");
                    logger.AddAndWrite(result.Error);
                    logger.AddAndWrite("* time: " + DateTime.Now);
                    logger.AddAndWrite("*Exited with code " + result.ExitCode);
                    exitCode = result.ExitCode;
                }
                else if (result.ExitCode !=0 && result.ExitOk)
                {
                    logger.AddAndWrite("*Error text: ");
                    logger.AddAndWrite(result.Error);
                    logger.AddAndWrite("* time: " + DateTime.Now);
                    logger.AddAndWrite("*Exited with code " + result.ExitCode);
                    logger.AddAndWrite("*...but this is a whitelisted exit code for this command");
                    exitCode = 0;
                }
            }
            catch (Exception ex)
            {
                logger.AddAndWrite("*RunFile error - trying to run the process threw an exception.");
                logger.AddAndWrite(ex.Message);
                logger.AddAndWrite(ex.StackTrace);
                logger.AddAndWrite("* time: " + DateTime.Now);
                logger.AddAndWrite("*Exited with code 2");
                exitCode = 2;
            }

            logger.AddAndWrite("*finished: " + workingFolder + command);
            logger.AddAndWrite(" *time: " + DateTime.Now);

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

            logger.AddToLog("*cmd: " + file);
            logger.AddToLog("*args: " + args);


            logger.AddAndWrite("* ...now running... ");


            string cmd = "/c " + workingDirectory + commandName;
            var processInfo = new ProcessStartInfo("cmd.exe", cmd);

            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = file;
            process.StartInfo.WorkingDirectory = workingDirectory;
            process.StartInfo.Arguments = args;
            process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;

            if(!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password)) 
            {
                var nameParts = username.Split('\\');
                string domain = null;
                string user = null;
                if(nameParts.Length == 2) 
                {
                    domain = nameParts[0];
                    user = nameParts[1];
                } else 
                {
                    user = username;
                }

                process.StartInfo.Domain = domain;
                process.StartInfo.UserName = user;
                SecureString securePassword = new SecureString();
                foreach (char c in password.ToCharArray()) securePassword.AppendChar(c);
                process.StartInfo.Password = securePassword;
            }
            Stopwatch sw = new Stopwatch();
            sw.Start();
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            logger.AddAndWrite("* started");

            var timeout = ProcessExecutorHelper.GetTimeoutLength(commandName, timeoutList, logger, defaultTimeout);

            if (timeout != 0)
            {
                logger.AddAndWrite("* waitingForExit");

                if (process.WaitForExit(timeout))
                {
                    sw.Stop();
                    logger.AddAndWrite("*finished in " + Math.Floor(sw.ElapsedMilliseconds / 1000m) + " s");
                }
                else
                {
                    logger.AddAndWrite("*process took longer than " + timeout + "  ms");
                    return new ProcessOutcome("", "", 1, false);
                }
            }



            if (timeout == 0)
            {
                sw.Stop();
                logger.AddAndWrite("*finished in " + Math.Floor(sw.ElapsedMilliseconds / 1000m) + " s");
            }

            logger.AddAndWrite("*ran with exit code: " + process.ExitCode);

            bool exitOk = false;

            if (process.ExitCode > 0)
            {
                //logger.AddToLog("*Looking for allowed exceptions for: " + commandName);
                //allowedExceptions.ForEach(z => logger.AddToLog("*: " + z));
                var allowedException = this.allowedExceptions.Find(a => a.Contains(commandName));
                if (!String.IsNullOrEmpty(allowedException))
                {
                    exitOk = true;
                }

            }

            return new ProcessOutcome(error, output, process.ExitCode, exitOk);
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
