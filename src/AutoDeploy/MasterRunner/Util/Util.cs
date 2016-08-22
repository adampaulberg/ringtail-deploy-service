using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterRunner.Util
{

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
        public static void Write(string fileName, List<string> s, int failCount = 0)
        {
            try
            {
                using (StreamWriter wr = new StreamWriter(fileName))
                {
                    foreach (string str in s)
                    {
                        wr.WriteLine(str);
                    }
                }
            }
            catch(Exception ex)
            {
                if (failCount < 50)
                {
                    Console.WriteLine("Couldn't write " + fileName + " .... retrying.");
                    System.Threading.Thread.Sleep(1000);
                    Write(fileName, s, failCount + 1);
                }
                else
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
            }
        }
    }

    public class Logger
    {
        List<string> log = new List<string>();
        IOutputEngine output;

        internal string fileName { get; set; }

        public Logger()
        {

        }
        public Logger(IOutputEngine ioe)
        {
            output = ioe;
        }
        public void AddToLog(string s)
        {
            Console.WriteLine(s);
            log.Add(s);
        }
        public void AddToLog(List<string> s)
        {
            log.AddRange(s);
        }
        public void Write(string file)
        {
            if (output == null)
            {
                SimpleFileWriter.Write(file, log);
            }
            else
            {
                output.Output(log);
            }
        }

        public void AddAndWrite(string s)
        {
            AddToLog(s);
            Write(this.fileName);
        }
    }

    public interface IOutputEngine
    {
        void Output(List<string> s);
    }
}
