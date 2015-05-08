using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deployer.Util
{
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

        public void Write(string file)
        {
            SimpleFileWriter.Write(file, log);
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
}
