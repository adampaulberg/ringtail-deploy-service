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
            //Console.WriteLine(" InstallNameTruncator...");
            Logger logger = new Logger();
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

                        logger.AddToLog("   truncating: " + x + " to: " + newName);
                        s.Add("rename \"" + x.Name + "\" \"" + newName + "\"");
                    }
                }

                if (s.Count == 0)
                {
                    s.Add("NO_OP");
                }

                SimpleFileWriter.Write("scrubNames.bat", s);

                logger.AddToLog("   Wrote out scrubNames.bat");

                if (args.Length > 0 && args[0] == "/r")
                {
                    int exitCode = ExecuteCommand("scrubNames.bat", logger);
                    
                    if(exitCode == 0)
                    {
                        Console.WriteLine(" Ok");
                    }
                    else
                    {
                        Console.WriteLine(" There was an error.  See the installNameTruncator.log for more information.");
                    }
                }

                

                logger.Write("installNameTruncator.log");
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

        private static int ExecuteCommand(string command, Logger logger)
        {
            int ExitCode = 0;
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
            process.Close();

            logger.AddToLog("Output: " + output);
            logger.AddToLog("Errors: " + error);
            logger.AddToLog("ExitCode: " + ExitCode);

            return ExitCode;
        }
    }


    public class Logger
    {
        List<string> log = new List<string>();

        public void AddToLog(string s)
        {
            log.Add(s);
        }
        public void AddToLog(List<string> s)
        {
            log.AddRange(s);
        }

        public List<string> GetLog()
        {
            return log;
        }

        public void Write(string file)
        {
            SimpleFileWriter.Write(file, log);
        }

        public void AddAndWrite(string s, string file)
        {
            AddToLog(s);
            Write(file);
        }

        public void AddAndWrite(List<string> s, string file)
        {
            AddToLog(s);
            Write(file);
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
