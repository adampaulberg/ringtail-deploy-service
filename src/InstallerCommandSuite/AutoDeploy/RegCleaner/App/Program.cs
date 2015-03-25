using Microsoft.Win32;
using RegistryReader.App;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegistryReader
{
    public class Program
    {
        static int Main(string[] args)
        {
            int exit = 0;
            try
            {
                var regKeyToAppDictionary = RegistryReaderUtilities.BuildRegistryToApplicationMap(SimpleFileReader.Read("registry.config"));

                Dictionary<string, string> outFileNonDupe;
                Dictionary<string, string> altConfig;
                RegistryReaderUtilities.GenerateFiles(regKeyToAppDictionary, out outFileNonDupe, out altConfig);


                SimpleFileWriter.Write("regKeys.config", outFileNonDupe.Keys);
                SimpleFileWriter.Write("currentMachine.config", altConfig.Keys.ToList().OrderBy(x => x));


                if (!new FileInfo("regKeys.config").Exists)
                {
                    exit = 1;
                }
                if (!new FileInfo("currentMachine.config").Exists)
                {
                    exit = 1;
                }

                if (exit == 0)
                {
                    Console.WriteLine("RegKeys...........");
                    SimpleFileReader.Read("regKeys.config").ForEach(x => Console.WriteLine(x));
                    Console.WriteLine(string.Empty);
                    Console.WriteLine("CurrentConfig.............");
                    SimpleFileReader.Read("currentMachine.config").ForEach(x => Console.WriteLine(x));

                    Console.WriteLine("RegistryReader succeeded");
                }

            }
            catch (Exception ex)
            {
                var msg = new List<string>();
                msg.Add("Error writing....");
                msg.Add(ex.Message);
                msg.Add(ex.StackTrace);
                SimpleFileWriter.Write("RegistryReader.Log", msg);
                msg.ForEach(x => Console.WriteLine(x));
                exit = 1;
            }

            return exit;
        }

        
    }

    public class RegistryReaderUtilities
    {
        public static Dictionary<string, string> BuildRegistryToApplicationMap(List<string> mapFile)
        {
            var ret = new Dictionary<string, string>();

            foreach (var x in mapFile)
            {
                var split = x.Split('|');
                ret.Add(split[0], split[1]);
            }

            return ret;
        }

        public static void GenerateFiles(Dictionary<string, string> registryToApplicationMap, out Dictionary<string, string> outFileNonDupe, out Dictionary<string, string> altConfig)
        {
            var regTree = new RegistryReaderHelper().GetAllKeysInBaseKey();
            outFileNonDupe = new Dictionary<string, string>();
            altConfig = new Dictionary<string, string>();


            foreach (var x in regTree)
            {
                foreach (var y in x.KeyValues.Keys)
                {
                    string s = x.RegPath + "|\"" + y + "\"|\"" + x.KeyValues[y] + "\"";
                    if (!outFileNonDupe.ContainsKey(s))
                    {
                        outFileNonDupe.Add(s, string.Empty);
                    }
                    string app = "Common";

                    if (registryToApplicationMap.ContainsKey(x.RegPath))
                    {
                        app = registryToApplicationMap[x.RegPath];
                    }

                    string alt = app + "|" + y.ToUpper() + "=\"" + x.KeyValues[y] + "\"";

                    if (!altConfig.ContainsKey(alt))
                    {
                        altConfig.Add(alt, string.Empty);
                    }
                }
            }
        }
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
        public static void Write(string fileName, IEnumerable<string> s)
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
