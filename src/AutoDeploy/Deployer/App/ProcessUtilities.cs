using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deployer.App
{
    public class ProcessUtilities
    {
        public class ProcessOutcome
        {
            public int ExitCode { get; private set; }
            public string Output { get; private set; }
            public string Error { get; private set; }

            public ProcessOutcome(string error, string output, int exitCode)
            {
                this.ExitCode = exitCode;
                this.Error = error;
                this.Output = output;
            }
        }

        internal static ProcessOutcome SpawnProcess(string commandName, string workingDirectory)
        {
            var index = commandName.IndexOf(' ');

            string file = commandName;
            string args = string.Empty;
            if (index != -1)
            {
                file = commandName.Substring(0, index);
                args = commandName.Substring(index + 1, commandName.Length - index - 1);
            }

            Console.WriteLine("*cmd: " + file);
            Console.WriteLine("*args: " + args);



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
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            Console.WriteLine("*ran with exit code: " + process.ExitCode);

            return new ProcessOutcome(error, output, process.ExitCode);
        }

    }
}
