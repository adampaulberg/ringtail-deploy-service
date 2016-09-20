using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenericInstaller
{
    class Program
    {
        static int Main(string[] args)
        {
            var exitCode = 0;
            Console.WriteLine("GenericInstaller starting on " + args[0]);

            if(args.Length == 0)
            {
                GetUsage();
                return -1;
            }

            try
            {

                GenericInstallerHelper.RunIt(args[0]);
                List<string> s = new List<string>();
                s.Add(args[0]);
                s.Add("Ok");
                SimpleFileWriter.Write("GenericLog.txt", s);
            }
            catch (Exception ex)
            {
                List<string> s = new List<string>();
                s.Add(ex.Message);
                s.Add(ex.StackTrace);
                SimpleFileWriter.Write("GenericLog.txt", s);

                Console.WriteLine("GenericInstaller error");
                s.ForEach(x => Console.WriteLine(s));

                exitCode = 1;
            }

            return exitCode;
        }


        public static void GetUsage()
        {
            Console.WriteLine("GenericInstaller - ");
            Console.WriteLine("  Usage:    GenericInstaller.exe [appName]");
            Console.WriteLine("  This will create a batch file of the form install-[APPNAME].bat");
            Console.WriteLine("  The batch file it creates will be able to run an InstallShield installer.");
            Console.WriteLine("     It looks for an app with the same name in InstallerTemplate.config.");
            Console.WriteLine("     It replaces all the params in InstallerTemplate.config with configs from volitleData.config");
        }

    }


    internal class DynamicExclusionDetector
    {
        public static List<string> DetectExclusions()
        {
            var list = new List<string>();

            var di = new DirectoryInfo(Environment.CurrentDirectory);

            var files = di.GetFiles();

            foreach (var f in files.ToList())
            {
                try
                {
                    var fileName = f.Name;
                    var STR_OMIT = "omit-";
                    if (fileName.StartsWith(STR_OMIT))
                    {
                        var omission = fileName.Substring(STR_OMIT.Length, fileName.Length - STR_OMIT.Length);
                        omission = omission.Split('.')[0];

                        list.Add(omission);
                        Console.WriteLine("found omission: " + omission);
                    }
                }
                catch
                {
                    Console.WriteLine("Minor problem reading omission files.  Continuing.");
                }
            }

            return list;
        }
    }

    public class GenericInstallerHelper
    {
        public static void RunIt(string appName)
        {
            List<string> exclusions = DynamicExclusionDetector.DetectExclusions();

            if (exclusions.Contains(appName))
            {
                Console.WriteLine("GenericInstaller found exclusion for: " + appName);
                var noOpFile = new List<string>();
                noOpFile.Add("@echo SKIPPING");
                SimpleFileWriter.Write("install-" + appName + ".bat", noOpFile);
                return;
            }

            string configFile = "installerTemplate.config";
            string volitileData = "volitleData.config";

            string applicationName = appName;

            Console.WriteLine("GenericInstaller Reading.... " + applicationName);

            List<string> installerTemplate = new List<string>();
            installerTemplate = SimpleFileReader.Read(configFile);

            List<string> volitileDataList = new List<string>();
            volitileDataList = SimpleFileReader.Read(volitileData);
            
            
            var filledInParameters = DoIt(installerTemplate, volitileDataList, applicationName);

            Console.WriteLine("GenericInstaller Writing to file.... " + "install-" + applicationName + ".bat");

            SimpleFileWriter.Write("install-" + applicationName + ".bat", filledInParameters);
        }

        public static List<string> DoIt(List<string> installerTemplate, List<string> volitileData, string applicationName)
        {
            List<string> filledInParameters = new List<string>();

            var lookupKeys = BuildConfigDictionary(volitileData);
            var commonKeys = lookupKeys["COMMON"];
            var applicationKeys = lookupKeys.ContainsKey(applicationName.ToUpper()) ? lookupKeys[applicationName.ToUpper()] : new Dictionary<string, string>();

            var commands = new List<string>();

            foreach (var x in installerTemplate)
            {
                var templateAppName = x.Split('|')[0];
                if (templateAppName == applicationName)
                {
                    commands.Add(x);
                }
            }

            foreach (var command in commands)
            {
                Console.WriteLine("                 Reading.... " + command);
                if (command.Length == 0 || command.StartsWith("--"))
                {
                    continue;
                }
                if (command.Contains("wmic"))
                {
                    filledInParameters.Add(command.Split('|')[2].TrimStart());
                    continue;
                }
                if (command.Contains(".exe"))
                {
                    var realCommand = command.Split('|')[2].TrimStart();

                    string workingcommand = realCommand;
                    var keyValueDelimiter = "=";

                    foreach (var x in commonKeys.Keys)
                    {
                        workingcommand = ReplaceParameter(workingcommand, x, commonKeys[x], keyValueDelimiter);
                    }

                    foreach (var x in applicationKeys.Keys)
                    {
                        workingcommand = ReplaceParameter(workingcommand, x, applicationKeys[x], keyValueDelimiter);
                    }

                    realCommand = workingcommand;
                    filledInParameters.Add(realCommand);
                }
                else
                {
                    filledInParameters.Add(command.Split('|')[2].TrimStart());
                }
            }

            return filledInParameters;
        }

        //private static List<string> GeneratePowershellRunnerScript(string command, Dictionary<string, string> commonKeys, Dictionary<string, string> applicationKeys, string applicationName)
        //{
        //    var realCommand = command.Split('|')[2].TrimStart();

        //    string workingcommand = realCommand;

        //    var keyValueDelimiter = " ";


        //    foreach (var x in commonKeys.Keys)
        //    {
        //        workingcommand = ReplaceParameter(workingcommand, x, commonKeys[x], keyValueDelimiter);
        //    }

        //    foreach (var x in applicationKeys.Keys)
        //    {
        //        workingcommand = ReplaceParameter(workingcommand, x, applicationKeys[x], keyValueDelimiter);
        //    }

        //    var realCommands = new List<string>();


        //    realCommands.Add("copy InstallNameTruncator.exe " + @".\" + applicationName + " /Y");
        //    realCommands.Add("cd " + applicationName);
        //    realCommands.Add("InstallNameTruncator.exe");
        //    realCommands.Add("scrubNames.bat");
        //    realCommands.Add(workingcommand);
        //    realCommands.Add("cd ..");

        //    return realCommands;

        //}

        private static string ReplaceParameter(string workingcommand, string replacementKey, string replacementValue, string keyValueDelimiter)
        {
            if (workingcommand.Contains(replacementKey))
            {
                int indexOfThisCommand = workingcommand.IndexOf(replacementKey);
                int indexOfNextCommand = indexOfThisCommand + replacementKey.Length;
                bool isAutoWrap = false;

                string right = string.Empty;

                if (workingcommand.Contains("/v"))
                {
                    isAutoWrap = true;
                }

                if (indexOfNextCommand < workingcommand.Length)
                {
                    string rightSide = workingcommand.Substring(indexOfNextCommand, workingcommand.Length - indexOfNextCommand);

                    string buffer = string.Empty;
                    for (int i = 1; i < rightSide.Length; i++)
                    {
                        buffer = rightSide.Substring(i - 1, 2);
                        if (buffer == "/v" || buffer == "/S" || buffer == "--")
                        {
                            right = rightSide.Substring(i - 1, rightSide.Length - i + 1);
                            break;
                        }
                    }
                }

                if (isAutoWrap)
                {
                    replacementValue = SmartWrapParameterInQuotes(replacementValue);
                }

                string left = workingcommand.Substring(0, indexOfThisCommand);

                string finalizer = "\" ";
                if (left.EndsWith("--"))
                {
                    finalizer = " ";
                    left = left.Substring(0, left.Length - 1);
                }

                string newString = left + replacementKey + keyValueDelimiter + replacementValue + finalizer + right;

                //Console.WriteLine("     replacing: " + replacementKey + " with " + replacementValue);
                //Console.WriteLine("     new is: " + newString);

                workingcommand = newString;
            }
            return workingcommand;
        }

        private static string SmartWrapParameterInQuotes(string replacementValue)
        {
            if (replacementValue.Contains(" "))
            {
                int quoteCount = 0;
                for (int i = 0; i < replacementValue.Length; i++)
                {
                    if (replacementValue[i] == '"')
                    {
                        quoteCount++;
                    }
                }
                if (quoteCount <= 2)
                {
                    replacementValue = "\"\"\"" + replacementValue + "\"\"\"";
                }
            }

            return replacementValue;
        }

        private static Dictionary<string, Dictionary<string, string>> BuildConfigDictionary(List<string> config)
        {
            var lookupKeys = new Dictionary<string, Dictionary<string, string>>();

            foreach (var x in config)
            {
                if (x.Length > 0)
                {
                    if (!x.StartsWith("--"))
                    {
                        string[] split = x.Split('|');
                        string applicationKey = split[0].ToUpper();
                        string[] variableKeyValue = split[1].Split('=');

                        if (!lookupKeys.ContainsKey(applicationKey))
                        {
                            lookupKeys.Add(applicationKey, new Dictionary<string, string>());
                        }

                        var varValue = variableKeyValue[1];
                        varValue = varValue.Substring(1, varValue.Length - 2);      // strip wrapping quotes.

                        lookupKeys[applicationKey].Add(variableKeyValue[0], varValue);
                    }
                }
            }
            return lookupKeys;

        }
    }

    public class SimpleFileReader
    {
        public static List<string> Read(string fileName)
        {
            List<string> s = new List<string>();
            using (StreamReader stream = new StreamReader(fileName))
            {
                string input = null;
                while ((input = stream.ReadLine()) != null)
                {
                    s.Add(input);
                }
            }

            return s;
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
