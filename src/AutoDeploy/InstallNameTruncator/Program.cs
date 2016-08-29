using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstallNameTruncator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(" InstallNameTruncator...");
            try
            {
                if (args.Length > 0) 
                {
                    if (args[0] == "--?" || args[0] == "/?" || args[0].Contains("help"))
                    {
                        Help();
                        return;
                    }
                }

                string currentDir = Environment.CurrentDirectory;
                DirectoryInfo di = new DirectoryInfo(currentDir);

                List<string> s = new List<string>();

                foreach (var x in di.GetFiles())
                {
                    if (x.Name.StartsWith("Ringtail") || x.Name.StartsWith("NativeFileService"))
                    {
                        string newName = x.Name.Split('_')[0] + x.Extension;

                        if (!x.Name.Contains('_'))
                        {
                            newName = x.Name;
                        }

                        Console.WriteLine("   truncating: " + x + " to: " + newName);
                        s.Add("rename \"" + x.Name + "\" \"" + newName + "\"");
                    }
                }

                if (s.Count == 0)
                {
                    s.Add("NO_OP");
                }

                SimpleFileWriter.Write("scrubNames.bat", s);

                Console.WriteLine("   Wrote out scrubNames.bat");

                if (args.Length > 0 && args[0] == "/r")
                {
                    ExecuteCommand("scrubNames.bat");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("InstallNameTruncator Error: ");
                Console.Write(ex.Message);
                Console.WriteLine("");
                Console.Write(ex.StackTrace);
            }
        }

        private static void Help()
        {
            Console.WriteLine(" *********************************************************");
            Console.WriteLine(" This tool truncates versioned installer names to a uniform name.");
            Console.WriteLine(" It assumes that an installer is of the form: ");
            Console.WriteLine("   [Ringtail][AppName]_[VersionName].exe");
            Console.WriteLine("   It strips off the _[VersionName] part.");
            Console.WriteLine("   It writes the operation out to scrubNames.bat");

            Console.WriteLine(" Argument Options ---------------------------------------- ");
            Console.WriteLine("  /r              Immediately run after creating the scrubNames.bat file.");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("  /u:<FILENAME>   Specify the user data file.");
            Console.WriteLine("                  default file: volitleData.config");
            Console.WriteLine("");
            Console.WriteLine("  /c:<FILENAME>   Specify the commands file.");
            Console.WriteLine("                  default file: commands.config");
        }

        private static void ExecuteCommand(string command)
        {
            int ExitCode;
            ProcessStartInfo ProcessInfo;
            Process process;

            ProcessInfo = new ProcessStartInfo(command);
            ProcessInfo.CreateNoWindow = true;
            ProcessInfo.UseShellExecute = false;
            ProcessInfo.WorkingDirectory = Environment.CurrentDirectory;
            // *** Redirect the output ***
            ProcessInfo.RedirectStandardError = true;
            ProcessInfo.RedirectStandardOutput = true;

            process = Process.Start(ProcessInfo);
            process.WaitForExit();

            // *** Read the streams ***
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            ExitCode = process.ExitCode;

            Console.Write("output>>" + (String.IsNullOrEmpty(output) ? "(none)" : output));
            Console.Write("error>>" + (String.IsNullOrEmpty(error) ? "(none)" : error));
            Console.Write("ExitCode: " + ExitCode.ToString(), "ExecuteCommand");
            process.Close();
        }
    }


    public class SimpleFileWriter
    {
        public static void Write(string fileName, List<string> s)
        {
            using (StreamWriter wr = new StreamWriter(fileName))
            {
                foreach (string str in s)
                {
                    wr.WriteLine(str);
                }
            }
        }
    }
}
