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

            try
            {
                
                RunIt(args[0]);
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

        public static void RunIt(string appName)
        {
            string configFile = "installerTemplate.config";
            string volitileData = "volitleData.config";

            string applicationName = appName;

            Console.WriteLine("GenericInstaller Reading.... " + applicationName);

            List<string> installerTemplate = new List<string>();
            installerTemplate = SimpleFileReader.Read(configFile);

            List<string> volitileDataList = new List<string>();
            volitileDataList = SimpleFileReader.Read(volitileData);

            DoIt(installerTemplate, volitileDataList, applicationName);
        }

        private static void DoIt(List<string> installerTemplate, List<string> volitileData, string applicationName)
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

                    foreach (var x in commonKeys.Keys)
                    {
                        workingcommand = ReplaceParameter(workingcommand, x, commonKeys[x]);
                    }

                    foreach (var x in applicationKeys.Keys)
                    {
                        workingcommand = ReplaceParameter(workingcommand, x, applicationKeys[x]);
                    }

                    realCommand = workingcommand;

                    //char[] whitespace = new char[] { ' ', '\t' };
                    //var parameters = new List<string>();
                    //parameters = realCommand.Split(whitespace).ToList();

                    ////realCommand = "\"" + split[1] + "\"";
                    //realCommand = FillInParameters(commonKeys, applicationKeys, realCommand, parameters);
                    filledInParameters.Add(realCommand);
                }
                else
                {
                    filledInParameters.Add(command.Split('|')[2].TrimStart());
                }
            }

            Console.WriteLine("GenericInstaller Writing to file.... " + "install-" + applicationName + ".bat");

            SimpleFileWriter.Write("install-" + applicationName + ".bat", filledInParameters);
        }

        private static string ReplaceParameter(string workingcommand, string replacementKey, string replacementValue)
        {
            if (workingcommand.Contains(replacementKey))
            {
                int indexOfThisCommand = workingcommand.IndexOf(replacementKey);
                int indexOfNextCommand = indexOfThisCommand + replacementKey.Length;

                string right = string.Empty;

                if (indexOfNextCommand < workingcommand.Length)
                {
                    string rightSide = workingcommand.Substring(indexOfNextCommand, workingcommand.Length - indexOfNextCommand);

                    string buffer = string.Empty;
                    for (int i = 1; i < rightSide.Length; i++)
                    {
                        buffer = rightSide.Substring(i - 1, 2);
                        if (buffer == "/v" || buffer == "/S")
                        {
                            right = rightSide.Substring(i - 1, rightSide.Length - i + 1);
                            break;
                        }
                    }
                }

                string left = workingcommand.Substring(0, indexOfThisCommand);

                string newString = left + replacementKey + "=" + replacementValue + "\" " + right;
                workingcommand = newString;
            }
            return workingcommand;
        }

        private static string FillInParameters(Dictionary<string, string> commonKeys, Dictionary<string, string> applicationKeys, string realCommand, List<string> parameters)
        {
            for (int i = 2; i < parameters.Count; i++)
            {
                var parameter = parameters[i];

                if (parameter.Contains("/S /v/qn"))
                {
                    realCommand += parameter;
                    continue;
                }
                if (!parameter.Contains("/v"))
                {
                    var realKey = parameter;
                    foreach (var key in commonKeys.Keys)
                    {
                        if (parameter.StartsWith(key))
                        {
                            parameter = key + "=" + commonKeys[key];
                        }
                    }
                    foreach (var key in applicationKeys.Keys)
                    {
                        if (parameter.StartsWith(key))
                        {
                            parameter = key + "=" + applicationKeys[key];
                        }
                    }


                    realCommand += " /v\"" + parameter + "\"";
                }
            }
            return realCommand;
        }


        private static string FillInParameters_BAK(Dictionary<string, string> commonKeys, Dictionary<string, string> applicationKeys, string realCommand, List<string> parameters)
        {
            for (int i = 2; i < parameters.Count; i++)
            {
                var parameter = parameters[i];

                if (parameter.Contains("/S /v/qn"))
                {
                    realCommand += parameter;
                    continue;
                }
                if (!parameter.Contains("/v"))
                {
                    var realKey = parameter;
                    foreach (var key in commonKeys.Keys)
                    {
                        if (parameter.StartsWith(key))
                        {
                            parameter = key + "=" + commonKeys[key];
                        }
                    }
                    foreach (var key in applicationKeys.Keys)
                    {
                        if (parameter.StartsWith(key))
                        {
                            parameter = key + "=" + applicationKeys[key];
                        }
                    }


                    realCommand += " /v\"" + parameter + "\"";
                }
            }
            return realCommand;
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
}
