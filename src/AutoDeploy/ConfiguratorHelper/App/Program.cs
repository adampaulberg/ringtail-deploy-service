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
                    options.dbVersion = RegistryHelper.GetDBVersion();
                    options.cbVersion = RegistryHelper.GetCBVersion();
                    var configuratorFile = ConfiguratorFileBuilder.CreateConfiguratorFile_Agent(options);

                    SimpleFileWriter.Write("runConfigurator-Agent.bat", configuratorFile);

                    configuratorFile = ConfiguratorFileBuilder.CreateConfiguratorFile_Classic(options);
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

    }
}
