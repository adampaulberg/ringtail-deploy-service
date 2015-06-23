using DatabaseUpgrader.App;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseUpgrader
{
    class Program
    {
        static int Main(string[] args)
        {
            int exit = 0;
            Console.WriteLine("Database Upgrader starting!");

            try
            {
                var options = new Options();

                if (CommandLine.Parser.Default.ParseArguments(args, options))
                {
                    if (args.Length == 0 || options.ValidateIsDefaultAction())
                    {
                        Console.WriteLine(options.GetUsage());
                        if (!new FileInfo("dbUp.bat").Exists)
                        {
                            Console.WriteLine("DID NOT CREATE dbUp.bat!");
                        }
                        return 0;
                    }
                    if (!options.ValidateActions())
                    {
                        Console.WriteLine(options.GetUsage());
                        Console.WriteLine("...failed to write dbUp.bat!");
                        return 1;
                    }

                    // Step 0. Make the camel!
                    Console.WriteLine(options.GetHeading());
                    Console.WriteLine("Writing out data upgrade...");

                    BatchWriter.Write(options);

                    Console.WriteLine("Writing to dbUp.bat complete!");


                    if (!new FileInfo("dbUp.bat").Exists)
                    {
                        Console.WriteLine("...failed to write dbUp.bat!");
                        exit = 1;
                    }
                }
                else
                {
                    if (!new FileInfo("dbUp.bat").Exists)
                    {
                        Console.WriteLine("...failed to write dbUp.bat!");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("\r\nUnhandled Exception:\r\n{0}", ex);
                exit = 1;
            }
            return exit;
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
