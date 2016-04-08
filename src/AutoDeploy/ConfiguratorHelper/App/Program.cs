using ConfiguratorHelper.App;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfiguratorHelper
{
    public class Program
    {
        static int Main(string[] args)
        {
            int exit = 0;
            var options = new Options();

            Console.WriteLine("ConfiguratorHelper....");

            try
            {
                if (CommandLine.Parser.Default.ParseArguments(args, options))
                {
                    var exclusions = DynamicExclusionDetector.DetectExclusions();

                    options.dbVersion = RegistryHelper.GetDBVersion();
                    options.cbVersion = RegistryHelper.GetCBVersion();
                    var configuratorFile = ConfiguratorFileBuilder.CreateConfiguratorFile_Agent(options);

                    if (exclusions.Contains("RingtailLegalAgentServer"))
                    {
                        configuratorFile = new List<string>();
                        configuratorFile.Add("@echo SKIPPING");
                    }

                    SimpleFileWriter.Write("runConfigurator-Agent.bat", configuratorFile);

                    if (exclusions.Contains("RingtailLegalApplicationServer"))
                    {
                        configuratorFile = new List<string>();
                        configuratorFile.Add("@echo SKIPPING");
                    }
                    else
                    {
                        configuratorFile = ConfiguratorFileBuilder.CreateConfiguratorFile_Classic(options);
                    }

                    SimpleFileWriter.Write("runConfigurator-Classic.bat", configuratorFile);

                    if (!new FileInfo("runConfigurator-Agent.bat").Exists)
                    {
                        Console.WriteLine("Filed to write runConfigurator-Agent.bat");
                        exit = 1;
                    }
                    else if (!new FileInfo("runConfigurator-Classic.bat").Exists)
                    {
                        Console.WriteLine("Filed to write runConfigurator-Classic.bat");
                        exit = 1;
                    }
                    else
                    {
                        Console.WriteLine("ConfiguratorHelper succeeded");
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                exit = 1;
            }

            return exit;
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
                        if (fileName.StartsWith("omit-"))
                        {
                            var omission = fileName.Split('-')[1];
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

    }
}
