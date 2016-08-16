using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using System.Linq;
using InstallFetcher;
using InstallFetcher.App;
using InstallFetcher.Util;

namespace InstallerTests
{
    [TestClass]
    public class GenericInstallerTests
    {
        List<string> installerTemplate;
        List<string> data;

        public void Init()
        {
            installerTemplate = SimpleFileReader.Read(@".\GenericInstallerTests\installerTemplate.config");
            data = SimpleFileReader.Read(@".\GenericInstallerTests\volitleData.config");
        }

        [TestMethod]
        public void ParameterReplacement_WindowsStyleProperties_Test()
        {
            Init();
            var output = GenericInstaller.GenericInstallerHelper.DoIt(installerTemplate, data, "RingtailDatabaseUtility");
            Console.WriteLine(output[0]);

            Assert.IsFalse(output[0].Contains("123.45.67.890"));
            Assert.IsTrue(output[0].Contains("local"));
        }

        [TestMethod]
        public void ParameterReplacement_LinuxStyleProperties_Test()
        {
            Init();
            var output = GenericInstaller.GenericInstallerHelper.DoIt(installerTemplate, data, "NativeFileServiceSetup");
            Console.WriteLine(output[0]);

            Assert.IsFalse(output[0].Contains("SERVICEUSERNAME"));
            Assert.IsTrue(output[0].Contains("serviceAccount"));
        }

        [TestMethod]
        public void ParameterReplacement_PowershellFillIn_Test()
        {
            Init();
            var output = GenericInstaller.GenericInstallerHelper.DoIt(installerTemplate, data, "Ringtail-Svc-ContentSearch");

            foreach (var x in output)
            {
                Console.WriteLine(x);
            }

            //Assert.IsFalse(output[0].Contains("CONTENT_SEARCH_USERNAME"));
            //Assert.IsTrue(output[0].Contains("myUser1"));
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
