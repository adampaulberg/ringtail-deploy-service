using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoleResolverUtility.Util
{
    public class Logger
    {
        List<string> log = new List<string>();
        IOutputEngine output;
        public Logger()
        {

        }
        public Logger(IOutputEngine ioe)
        {
            output = ioe;
        }
        public void AddToLog(string s)
        {
            log.Add(s);
        }
        public void AddToLog(List<string> s)
        {
            log.AddRange(s);
        }
        public void Write(string file)
        {
            foreach (var x in log)
            {
                Console.WriteLine("\t" + x);
            }

            if (output == null)
            {
                SimpleFileWriter.Write(file, log);
            }
            else
            {
                output.Output(log);
            }
        }
    }

    public interface IOutputEngine
    {
        void Output(List<string> s);
    }

    public class SimpleFileReader
    {
        public static List<string> Read(string fileName)
        {
            List<string> s = new List<string>();

            FileInfo fi = new FileInfo(fileName);
            if (fi.Exists)
            {

                using (StreamReader stream = new StreamReader(fileName))
                {
                    string input = null;
                    while ((input = stream.ReadLine()) != null)
                    {
                        s.Add(input);
                    }
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
