using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using System.Linq;
using RegistryReader;
using RegistryReader.App;

namespace InstallerTests
{
    [TestClass]
    public class RegistryReaderScrubberTests
    {
        [TestMethod]
        public void Test_GetRingtailKeys()
        {
            var regTree = new RegistryReaderHelper().GetAllKeysInBaseKey();


            foreach (var x in regTree)
            {
                //Console.WriteLine(x.RegPath);
                foreach (var y in x.KeyValues.Keys)
                {
                    Console.WriteLine(x.RegPath + "|\""  + y  + "\"|\"" + x.KeyValues[y] + "\"");
                }
            }

            Assert.IsNotNull(regTree);
            Assert.IsTrue(regTree.Count > 0);
        }

        [TestMethod]
        public void RegistryKEyReaderTestSample()
        {
            var regKeyToAppDictionary = RegistryReaderUtilities.BuildRegistryToApplicationMap(SimpleFileReader.Read(@"D:\registry.config"));



            Dictionary<string, string> outFileNonDupe;
            Dictionary<string, string> altConfig;
            RegistryReaderUtilities.GenerateFiles(regKeyToAppDictionary, out outFileNonDupe, out altConfig);


            Console.WriteLine("Registry:");
            foreach (var x in outFileNonDupe)
            {
                Console.WriteLine("\t" + x);
            }


            Console.WriteLine("Config:");
            foreach (var x in altConfig)
            {
                Console.WriteLine("\t" + x);
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
}
