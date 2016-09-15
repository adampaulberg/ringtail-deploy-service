using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using System.Linq;
using DatabaseUpgrader.App;
using System.Web.Script.Serialization;
using DataCamel.Helpers;

namespace InstallerTests
{
    [TestClass]
    public class DatabaseUpgraderTests
    {
       // const string configFile = @"DatabaseUpgraderTests\testData.config";

        /// <summary>
        /// Dynamically generate a test config
        /// </summary>
        /// <returns></returns>
        public string generateConfig()
        {
            string tempFile = Path.GetTempFileName();

            using (StreamWriter writetext = new StreamWriter(tempFile))
            {                
                writetext.WriteLine("LAUNCHKEY|MyKey=\"someFeature with \"some\"\"");
                writetext.WriteLine("LAUNCHKEY|MyKey2=\"someFeature2\"");
            }
            return tempFile;
        }


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
            // Write keys to temp file
            string tempFile = Path.GetTempFileName();

            DataCamel.Helpers.ConfigHelper.WriteLaunchKeysAsJson(new ConfigHelper.ConfigOptions() { VolitleDataFile = tempFile, WriteLocation = Path.Combine(Directory.GetCurrentDirectory(), generateConfig()) });

            Assert.IsTrue(File.Exists(tempFile), "Keyfile was not created");
            string keyFileContents = File.ReadAllText(tempFile);
            Assert.IsNotNull(keyFileContents, "Keyfile was created, but is empty.  Expected file to contain keys entries.");
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            try
            {
                // Test for valid JSon
                var obj = serializer.Deserialize<object>(keyFileContents);
            }
            catch (Exception ex)
            {
                Assert.Fail("JSon data could not be deserialized indicating bad data: " + ex.Message);
            }
        }

        [TestMethod]
        public void ConvertLaunchKeyConfigToLaunchKeyJson()
        {
            List<string> configs = new List<string>();
            configs.Add("LAUNCHKEY|MyKey=\"someFeature\"");
            configs.Add("LAUNCHKEY|MyKey2=\"someFeature2\"");
            var x = DataCamel.Helpers.ConfigHelper.ConvertToKeysfileJson(configs);
            Console.WriteLine(x);

            var targetMatch = "[{\"Description\":\"someFeature\",\"FeatureKey\":\"MyKey\"},{\"Description\":\"someFeature2\",\"FeatureKey\":\"MyKey2\"}]";
            Assert.AreEqual(x, targetMatch);
        }


        [TestMethod]
        public void ConvertLaunchKeyConfigToLaunchKeySpecialCharsJson()
        {
            List<string> configs = new List<string>();
            configs.Add("LAUNCHKEY|MyKey=\"someFeature\" with -- some \"special\" chars");
            configs.Add("LAUNCHKEY|MyKey2=\"someFeature2\"");
            var x = DataCamel.Helpers.ConfigHelper.ConvertToKeysfileJson(configs);
            Console.WriteLine(x);

            var targetMatch = "[{\"Description\":\"someFeature\\\" with -- some \\\"special\\\" char\",\"FeatureKey\":\"MyKey\"},{\"Description\":\"someFeature2\",\"FeatureKey\":\"MyKey2\"}]";
            Assert.AreEqual(x, targetMatch);
        }

    }
}