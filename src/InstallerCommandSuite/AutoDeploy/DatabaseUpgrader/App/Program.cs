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
            try
            {
                var options = new Options();

                if (CommandLine.Parser.Default.ParseArguments(args, options))
                {
                    if (!options.ValidateActions())
                    {
                        Console.WriteLine(options.GetUsage());
                        return 1;
                    }

                    // Step 0. Make the camel!
                    Console.WriteLine(options.GetHeading());
                    Console.WriteLine("Writing out data upgrade...");

                    BatchWriter.Write(options);

                    Console.WriteLine("Writing to dbUp.bat complete!");


                    if (!new FileInfo("dbUp.bat").Exists)
                    {
                        exit = 1;
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
