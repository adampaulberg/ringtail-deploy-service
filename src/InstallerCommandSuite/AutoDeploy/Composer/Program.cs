using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Composer
{
    class Program
    {
        static int Main(string[] args)
        {
            int exit = 0;
            try
            {
                List<string> filesToCompose = new List<string>();
                if (args.Length == 1)
                {
                    string configFile = args[0];
                    filesToCompose = SimpleFileReader.Read(configFile);
                }

                List<string> s = new List<string>();
                foreach (var x in filesToCompose)
                {
                    if (x.StartsWith("--"))
                    {
                        s.Add("rem " + x);
                        continue;
                    }
                    if (x.Contains("exe") || x.StartsWith("@"))
                    {
                        s.Add(x);
                    }
                    else
                    {
                        if (x.Contains("bat"))
                        {
                            FileInfo fi = new FileInfo(x);
                            if (fi.Exists)
                            {
                                s.Add(x);
                               // s.AddRange(SimpleFileReader.Read(x));
                            }
                            else
                            {
                                Console.WriteLine("rem warninig: could not find: " + fi.FullName);
                            }
                        }

                    }
                }

                SimpleFileWriter.Write("master.bat", s);

                if (!new FileInfo("master.bat").Exists)
                {
                    exit = 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to write master.bat");
                List<string> s = new List<string>();
                s.Add("Composer Error");
                s.Add(ex.Message);
                s.Add(ex.StackTrace);
                SimpleFileWriter.Write("ComposerLog.txt", s);
                exit = 1;
            }
            return exit;
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
