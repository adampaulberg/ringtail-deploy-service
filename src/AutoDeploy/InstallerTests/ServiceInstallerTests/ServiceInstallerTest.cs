﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceInstaller.App;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstallerTests.ServiceInstallerTests
{
    [TestClass]
    public class ServiceInstallerTest
    {

        [TestMethod]
        public void Test()
        {
            var testFile = new List<string>();

            testFile.Add("<add key=\"RpfDBUser\" value=\"webuser\" />");

            var vData = new List<string>();
            vData.Add("Common|RpfDBUser=\"localPortal\"");
            vData.Add("SomeOtherApp|RpfDBUser=\"Junk\"");


            var newConfig = ConfigHelper.ApplyVolitleDataToConfig("blah", testFile, vData);
            Assert.IsTrue(newConfig[0].Contains("localPortal"), "Should be localPortal");


            vData.Add("TESTAPP|RpfDBUser=\"alternateKey\"");
            newConfig = ConfigHelper.ApplyVolitleDataToConfig("TESTAPP", testFile, vData);
            Assert.IsTrue(newConfig[0].Contains("alternateKey"), "Should be alternateKey");

        }

        [TestMethod]
        public void TestGenerateIISInstallCommand_DoesNotPassAuthenticationCreds()
        {
            var vData = new List<string>();

            var x = ServiceInstallerHelper.GenerateIISInstallCommand(vData, "MyApp");
            Console.WriteLine(x);

            Assert.AreEqual(x, "DeployToIIS.exe -a MyApp -i \"C:\\Program Files\\FTI Technology\\MyApp\"", "no matching creds");

            vData = new List<string>();
            vData.Add("MyApp|SERVICEUSERNAME=\"user\"");
            vData.Add("MyApp|SERVICEPASSWORD=\"password\"");

            x = ServiceInstallerHelper.GenerateIISInstallCommand(vData, "MyApp");
            Assert.AreEqual(x, "DeployToIIS.exe -a MyApp -i \"C:\\Program Files\\FTI Technology\\MyApp\"", "creds present");
        }
    }
}

