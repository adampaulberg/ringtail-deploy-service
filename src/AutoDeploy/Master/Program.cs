using Master.App;
using Master.Model;
using Master.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Master
{
    class Program
    {
        static int Main(string[] args)
        {
            int exitCode = 0;
            Logger logger = new Logger();
            logger.fileName = "MasterLog.txt";
            try
            {
                logger.AddToLog("...Master.exe starting...");
                try
                {
                    Utilities.Cleanup(logger);
                    exitCode = Utilities.DoIt(logger);
                }
                catch (Exception ex)
                {
                    logger.AddAndWrite("Error creating master.bat file: " + ex.StackTrace);
                    exitCode = 1;
                }
            }
            catch (Exception ex)
            {
                var s = new List<string>();
                logger.AddToLog("Error in Master.exe");
                logger.AddToLog(ex.Message);
                logger.AddAndWrite(ex.StackTrace);
                exitCode = 1;
            }

            try
            {
                if (exitCode != 0)
                {
                    FileInfo fi = new FileInfo("parameterizeCommands.bat");
                    if (fi.Exists)
                    {
                        fi.Delete();
                    }
                    fi = new FileInfo("master.bat");
                    if (fi.Exists)
                    {
                        fi.Delete();
                    }
                    logger.AddAndWrite("...master.exe FAILED");
                }
                else
                {
                    var s = new List<string>();
                    s.Add("Master Log...........");
                    s.Add(".......... parameterizedCommands.bat ...........");
                    s.AddRange(SimpleFileReader.Read("parameterizedCommands.bat"));
                    s.Add(String.Empty);
                    s.Add(".......... master.bat ...........");
                    s.AddRange(SimpleFileReader.Read("master.bat"));
                    s.Add("..........................................");
                    s.ForEach(x => logger.AddToLog((x)));
                    logger.AddAndWrite("...master.exe completed successfully");
                }
            }
            catch (Exception ex)
            {
                logger.AddToLog("Error writing out logs: ");
                logger.AddAndWrite(ex.Message);
                exitCode = 1;
            }

            return exitCode;
        }

        public static void Help()
        {
            Console.WriteLine(" *********************************************************");
            Console.WriteLine(" This tool will build a batch file to run your commands.");
            Console.WriteLine(" Supply a commands config file (a list of exe's to execute with some params).");
            Console.WriteLine(" Supply a user config file where it does parameter fill-in onto a commands file.");
            Console.WriteLine(" It will output the commands with the parameters replaced as a batch file.");
            Console.WriteLine("   this is called   parameterizedCommands.bat");
            Console.WriteLine("");
        }
    }


    public class Utilities
    {
        static string userData = "volitleData.config";
        static string machineData = "currentMachine.config";
        static string commandData = "commands.config";

        public static void WriteMasterConfig()
        {
            System.Diagnostics.Process.Start("RoleResolver.exe");
        }

        public static void CompareConfigurations(List<string> a, List<string> b)
        {
            var x = KeyValueConfigDictionary.CompareConfigurations(a, b);
            SimpleFileWriter.Write("configDifferences.config", x);
        }

        public static int Cleanup(Logger logger)
        {
            logger.AddToLog("Attempting to cleanup files from a prior run.");
            try
            {
                var sub = "bak_" + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + "_" + DateTime.Now.Hour + DateTime.Now.Minute + "__" + Guid.NewGuid();
                var main = new DirectoryInfo(Environment.CurrentDirectory);
                var backup = main.CreateSubdirectory(sub);

                var files = new List<FileInfo>();

                files.AddRange(main.GetFiles("fetch-*.bat").ToList());
                files.AddRange(main.GetFiles("install-*.bat").ToList());
                files.AddRange(main.GetFiles("parameterizedCommands.bat").ToList());
                files.AddRange(main.GetFiles("master.bat").ToList());

                files.AddRange(main.GetFiles("i-*.log").ToList());
                files.AddRange(main.GetFiles("u-*.log").ToList());

                foreach (var x in files)
                {
                    File.Move(x.FullName, backup.FullName + @"\" + x.Name);
                }
            }
            catch (Exception ex)
            {
                logger.AddToLog("WARNING: exception during cleanup, continuing anyway.");
                logger.AddToLog(ex.Message);
            }

            return 0;
        }

        public static int DoIt(Logger logger)
        {
            int exitCode = 0;
            try
            {
                List<string> filledInParameters = new List<string>();
                var userDataFile = SimpleFileReader.Read(userData);
                var machineDataFile = SimpleFileReader.Read(machineData);
                var masterConfig = KeyValueConfigDictionary.CombineConfigurations(userDataFile, machineDataFile);

                List<string> configFileIssues = new List<string>();
                logger.AddAndWrite("Validating configuration....");
                var isValid = ConfigurationValidator.ValidateConfiguration(masterConfig, 1, out configFileIssues);
                configFileIssues.ForEach(x => logger.AddToLog(x));

                if (isValid)
                {
                    CompareConfigurations(userDataFile, machineDataFile);
                    SimpleFileWriter.Write("runtime.config", masterConfig);

                    var lookupKeys = new KeyValueConfigDictionary();
                    lookupKeys.Read(masterConfig);
                    var commands = SimpleFileReader.Read(commandData);                

                    logger.AddAndWrite("Validation ok....");
                    foreach (var command in commands)
                    {
                        if (command.Length > 0 && !command.StartsWith("--"))
                        {
                            var realCommand = ConfigurationHelper.FillInParametersForCommand(command, lookupKeys);
                            filledInParameters.Add(realCommand);
                        }
                    }

                    SimpleFileWriter.Write("parameterizedCommands.bat", filledInParameters);

                    var x = ProcessUtilities.SpawnProcess("masterRunner.exe -f parameterizedCommands.bat -o Runner.txt", Environment.CurrentDirectory);
                    
                    logger.AddAndWrite(x.Output);
                    exitCode = x.ExitCode;
                }
                else
                {
                    logger.AddAndWrite("Validation FAILED....");
                    exitCode = 1;
                }
            }
            catch (Exception ex)
            {
                logger.AddToLog(ex.Message);
                logger.AddAndWrite(ex.StackTrace);
                exitCode = 2;
            }

            return exitCode;
        }

    }
}
