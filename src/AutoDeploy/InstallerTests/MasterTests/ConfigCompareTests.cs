using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using System.Linq;
using Master;
using Master.Util;
using Master.Model;
using Master.App;

namespace InstallerTests
{
    [TestClass]
    public class ConfigCompareTests
    {
        [TestMethod]
        public void CommandBlockReader_Test()
        {
            var existingConfigs = new List<string>();
            existingConfigs.Add("A|key_1=\"value_1\"");
            existingConfigs.Add("B|key_3=\"value_3\"");

            var newConfigs = new List<string>();
            newConfigs.Add("A|key_1=\"newValue1\"");
            newConfigs.Add("B|key_3=\"value_3\"");

            var comparisonResult = KeyValueConfigDictionary.CompareConfigurations(existingConfigs, newConfigs);

            Assert.IsTrue(comparisonResult.Count > 0);
            Assert.IsTrue(comparisonResult.Exists(x => x.Contains("newValue1")));
            Assert.IsFalse(comparisonResult.Exists(x => x.Contains("B|key_3=\"value_3\"")));
        }

        [TestMethod]
        public void KeyValueConfigDictionarySmokeTest_Test()
        {
            var existingConfigs = new List<string>();
            existingConfigs.Add("RingtailConfigurator|HOST=\"correctHost\"");
            existingConfigs.Add("Common|HOST=\"badHost\"");
            existingConfigs.Add("RingtailConfigurator|NT_DOMAIN=\"ntHost\"");
            existingConfigs.Add("RingtailConfigurator|JUNK_KEY=\"junkValue\"");


            var x = new KeyValueConfigDictionary();
            x.Read(existingConfigs);

            foreach (var z in x.GetCommonKeys())
            {
                Console.WriteLine(z);
            }

            string command = "ConfiguratorHelper.exe~RingtailConfigurator|--host HOST|--ntDomain NT_DOMAIN|--ntUser NT_USER|--ntPassword NT_PASSWORD|--dbserver IS_SQLSERVER_SERVER|--dbsauser IS_SQLSERVER_USERNAME";

            var str = ConfigurationHelper.FillInParametersForCommand(command, x);

            Assert.IsTrue(str.Contains("correctHost"));
            Assert.IsFalse(str.Contains("badHost"));
            Assert.IsTrue(str.Contains("ntHost"));
            Assert.IsFalse(str.Contains("junkValue"));
        }


        class FakeFileSystem : IFileSystem
        {

            private List<string> whitelist = new List<string>();
            public void AddToWhiteList(string file)
            {
                whitelist.Add(file);
            }
            public bool DirectoryExists(string path)
            {
                bool exists = whitelist.Any(x => x == path);

                return exists;
            }
        }


        class MockRoleProvider : IRoleProvider
        {

            public List<string> GetRoles()
            {
                var roles = new List<string>();
                roles.Add("SKYTAP-ALLINONE|InstallRPF");
                roles.Add("SKYTAP-ALLINONE|InstallRingtail8");
                return roles;
            }
        }

        [TestMethod]
        public void ConfigurationValidator_ValidateConfiguration_Role_Test()
        {
            var existingConfigs = new List<string>();
            existingConfigs.Add("RingtailConfigurator|HOST=\"correctHost\"");
            existingConfigs.Add("Common|HOST=\"badHost\"");
            existingConfigs.Add("Common|BUILD_FOLDER_ROOT=\"" + @"\\SomeServer" + "\"");
            existingConfigs.Add("Common|BRANCH_NAME=\"MAH_BRANCH\"");

            FakeFileSystem fakeFileSystem = new FakeFileSystem();
            fakeFileSystem.AddToWhiteList(@"\\SomeServer\MAH_BRANCH");

            var problems = new List<string>();
            var isValid = ConfigurationValidator.ValidateConfiguration(existingConfigs, 0, out problems, new MockRoleProvider(), fakeFileSystem);

            problems.ForEach(x => Console.Write(x));
            Assert.IsFalse(isValid);
            Assert.IsTrue(problems.Exists(x => x.Contains("RoleResolver|Role")));

            existingConfigs.Add("RoleResolver|ROLE=\"SKYTAP-ALLINONE\"");
            isValid = ConfigurationValidator.ValidateConfiguration(existingConfigs, 0, out problems, new MockRoleProvider(), fakeFileSystem);

            problems.ForEach(x => Console.Write(x));
            Assert.IsTrue(isValid);
            Assert.IsTrue(problems.Count == 0);
        }

        [TestMethod]
        public void ConfigurationValidator_ValidConfiguration__ConnectableHttp_Test()
        {
            var existingConfigs = new List<string>();
            existingConfigs.Add("RoleResolver|ROLE=\"SKYTAP-ALLINONE\"");
            existingConfigs.Add("Common|URL=\"http://localhost/Ringtail\"");
            existingConfigs.Add("Common|BUILD_FOLDER_ROOT=\"" + @"\\SomeServer" + "\"");
            existingConfigs.Add("Common|BRANCH_NAME=\"MAH_BRANCH\"");


            FakeFileSystem fakeFileSystem = new FakeFileSystem();
            fakeFileSystem.AddToWhiteList(@"\\SomeServer\MAH_BRANCH");


            var problems = new List<string>();
            var isValid = ConfigurationValidator.ValidateConfiguration(existingConfigs, 0, out problems, new MockRoleProvider(), fakeFileSystem);

            problems.ForEach(x => Console.Write(x));

            Assert.IsTrue(isValid);
            Assert.IsTrue(problems.Count == 0);

            existingConfigs.Add("Common|URL2=\"http://someUrlThatDoesNotExist_REALLY_REALLY_NOT_VALID/Ringtail\"");
            isValid = ConfigurationValidator.ValidateConfiguration(existingConfigs, 0, out problems, new MockRoleProvider(), fakeFileSystem);
            problems.ForEach(x => Console.Write(x));
            Assert.IsTrue(isValid);
            Assert.IsTrue(problems.Count == 1);
        }

        [TestMethod]
        public void ConfigurationValidator_ValidateRoleExists_Test()
        {
            var existingConfigs = new List<string>();
            existingConfigs.Add("RoleResolver|ROLE=\"SKYTAP-ALLINONE\"");

            var roles = new List<string>();
            roles.Add("SKYTAP-ALLINONE:BLAH");

            var problems = new List<string>();
            var isValid = ConfigurationValidator.ValidateRoles(existingConfigs, new TestRoleProvider(roles), problems);

            problems.ForEach(x => Console.Write(x));
            Assert.IsTrue(isValid);

            roles = new List<string>();
            roles.Add("SKYTAP_ALLINONE:BLAH");

            isValid = ConfigurationValidator.ValidateRoles(existingConfigs, new TestRoleProvider(roles), problems);
            problems.ForEach(x => Console.Write(x));
            Assert.IsFalse(isValid);
        }

        private class TestRoleProvider : IRoleProvider
        {
            private List<string> roles = new List<string>();
            public TestRoleProvider(List<string> roles)
            {
                this.roles = roles;
            }

            public List<string> GetRoles()
            {
                return this.roles;
            }
        }

        
    }
}
