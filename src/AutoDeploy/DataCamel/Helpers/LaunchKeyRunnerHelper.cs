using DataCamel.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading.Tasks;


namespace DataCamel.Helpers
{
    public class LaunchKeyRunnerHelper
    {
        internal string keysFile = @"C:\Upgrade\AutoDeploy\launchKeys.json";
        internal string workingFolder = @"C:\upgrade\autodeploy\";

        public int RunFile(Action<string> Logger, string targetFilePath)
        {
            string filename = "ringtail-deploy-feature-utility.exe --bulkdatapath=\"" + targetFilePath + "\" --keysfile=\"" + keysFile + "\"";

            FileInfo fi = new FileInfo(workingFolder + "ringtail-deploy-feature-utility.exe");
            if (!fi.Exists)
            {
                return 0;
            }

            Helpers.ConfigHelper.WriteLaunchKeysAsJson(keysFile);

            var keyFileContents = ExpectedKeys();
            if (keyFileContents.Count == 0 || keyFileContents.Count == 1 && keyFileContents[0].StartsWith("{}"))
            {
                Logger("* Feature Keys: No keys needed to be added.\r\n");
                return 0;
            }

            int exitCode = SpawnAndLog(Logger, filename, workingFolder, null, null);
            if (exitCode == 0)
            {
                Logger("* Feature Keys: SUCCESS\r\n");
            }
            else
            {
                Logger("* Feature Keys: FAILED\r\n");
            }
            return exitCode;

        }

        public int SpawnAndLog(Action<string> Logger, string command, string workingFolder, string username, string password)
        {
            int exitCode = 0;
            try
            {
                var startingString = "*starting: " + workingFolder + command + "\r\n";
                var result = SpawnProcess(command, workingFolder, username, password);

                if (result.ExitCode != 0 && !result.ExitOk)
                {
                    Logger(startingString);
                    if (result.Output.Length > 0)
                    {
                        Logger("*Output text: ");
                        Logger(result.Output + "\r\n");
                    }
                    if (result.Error.Length > 0)
                    {
                        Logger("*Error text: ");
                        Logger(result.Error + "\r\n");
                    }
                    Logger("*Exit code: " + result.ExitCode + "\r\n");
                    exitCode = result.ExitCode;
                }

            }
            catch (Exception ex)
            {
                Logger("*RunFile error - trying to run the process threw an exception." + "\r\n");
                Logger(ex.Message);
                Logger(ex.StackTrace);
                exitCode = 2;
            }

            if (exitCode != 0)
            {
                Logger("*finished time: " + DateTime.Now + "\r\n");
            }

            return exitCode;
        }


        public List<string> ExpectedKeys()
        {
            return DataCamel.Helpers.SimpleFileReader.Read(keysFile);
        }

        /// <summary>
        /// Returns a list of any items from launch Keys that are NOT in featuresetListContents.
        /// </summary>
        /// <param name="launchKeys"></param>
        /// <param name="featuresetListContents"></param>
        /// <returns></returns>
        public static List<string> ReconcileExpectedKeysWithPostUpgradeKeys(IList<string> launchKeys, List<string> featuresetListContents)
        {
            var diff = new List<string>();
            foreach (var launchKey in launchKeys)
            {
                var key = launchKey.Split('|')[1].Split('=')[0];

                bool found = featuresetListContents.Exists(x => x == key);

                if (!found)
                {
                    diff.Add(key);
                }
            }

            return diff;
            
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

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            return new ProcessOutcome(error, output, process.ExitCode, false);
        }
    }

}
