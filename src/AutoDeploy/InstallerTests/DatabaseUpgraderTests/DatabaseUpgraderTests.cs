using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using System.Linq;
using DatabaseUpgrader.App;

namespace InstallerTests
{
    [TestClass]
    public class DatabaseUpgraderTests
    {
        [TestMethod]
        public void DoIt()
        {
            var argumentsAsList = new List<string>();
            argumentsAsList.Add("upgradePortal");
            argumentsAsList.Add("-u");
            argumentsAsList.Add("\"sa\"");
            argumentsAsList.Add("-p");
            argumentsAsList.Add("\"pwd\"");
            argumentsAsList.Add("-d");
            argumentsAsList.Add("\"portal\"");
            Options opts = new Options();
            if (CommandLine.Parser.Default.ParseArguments(argumentsAsList.ToArray(), opts))
            {

                Assert.AreEqual(1, opts.Actions.Count);
            }
            else
            {
                Assert.Fail();
            }

            if (!opts.ValidateActions())
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void WriteLaunchKeyJson()
        {
            DataCamel.Helpers.ConfigHelper.WriteLaunchKeysAsJson(@"D:\test\testOutcome.json");
        }

        [TestMethod]
        public void ConvertLaunchKeyConfigToLaunchKeyJson()
        {
            List<string> configs = new List<string>();
            configs.Add("LAUNCHKEY|MyKey=\"nokey|someFeature\"");
            configs.Add("LAUNCHKEY|MyKey2=\"nokey2|someFeature2\"");
            var x = DataCamel.Helpers.ConfigHelper.ConvertToKeysfileJson(configs);
            Console.WriteLine(x);

            Assert.AreEqual(x, "[{\"Description\":\"someFeature\", \"FeatureKey\":\"MyKey\", \"MinorKey\":\"nokey\"},{\"Description\":\"someFeature2\", \"FeatureKey\":\"MyKey2\", \"MinorKey\":\"nokey2\"}]");
        }
    }
}