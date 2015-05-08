using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Master.Util;
using RequiredConfigurationsGenerator.App;

namespace InstallerTests
{

    // NOTE - there is no app-code for this right now.
    [TestClass]
    public class GetRequiredConfigsTests
    {
        [TestMethod]
        public void RequiredConfigurationGenerator_INTEGRATION_Test()
        {
            //var requiredConfigs = RequiredConfigurationGeneratorRunner.GenerateAllRequiredConfigurations(@"C:\Upgrade\AutoDeploy2");
        }

        [TestMethod]
        public void GetRequiredConfigsTests_Test()
        {
            var roles = new List<string>();
            roles.Add("SimpleDB|RingtailDatabaseUtility");
            roles.Add("SimpleDB|DatabaseUpgrader");

            var commands = new List<string>();
            commands.Add("DatabaseUpgrader.exe~DatabaseUpgrader|DATACAMEL_ACTION|-u IS_SQLSERVER_USERNAME|-p IS_SQLSERVER_PASSWORD|-d DATACAMEL_DATABASES");
            commands.Add("SomeAppThatDoesntExistYet.exe~RingtailDatabaseUtility|-u IS_SQLSERVER_USERNAME|-p IS_SQLSERVER_PASSWORD");

            var installerTemlpate = new List<string>();
            installerTemlpate.Add("RingtailDatabaseUtility|INSTALL| \"RingtailSQLComponent(x64).exe\" /v\"IS_SQLSERVER_SERVER=172.30.206.22\\SQL2005,1440\" /v\"IS_SQLSERVER_USERNAME=user\" /v\"WEBUSER=webuser\" /v\"IS_SQLSERVER_PASSWORD=pwd\" /S /v/qn\"");

            var expected = new List<string>();
            expected.Add("DatabaseUpgrader|DATACAMEL_ACTION");
            expected.Add("DatabaseUpgrader|IS_SQLSERVER_USERNAME");
            expected.Add("DatabaseUpgrader|IS_SQLSERVER_PASSWORD");
            expected.Add("DatabaseUpgrader|DATACAMEL_DATABASES");
            expected.Add("RingtailDatabaseUtility|IS_SQLSERVER_SERVER");
            expected.Add("RingtailDatabaseUtility|IS_SQLSERVER_USERNAME");
            expected.Add("RingtailDatabaseUtility|WEBUSER");
            expected.Add("RingtailDatabaseUtility|IS_SQLSERVER_PASSWORD");

            var requiredApps = RequiredConfigurationHelper.GetRequiredAppsByRole("SimpleDB", roles);
            requiredApps.ForEach(x => Console.WriteLine(x));
            Assert.IsTrue(requiredApps.Count > 0);
            var actualConfigs = RequiredConfigurationHelper.GetAllRequiredConfigs(requiredApps, installerTemlpate, commands);

            actualConfigs.ForEach(x => Console.WriteLine(x));

            expected.ForEach(x => Assert.IsTrue(actualConfigs.Exists(y => y == x)));

        }

        [TestMethod]
        public void GetRequiredConfigsTests_GetRequiredConfigsByAppForTemplates()
        {
            var templateFile = new List<string>();
            templateFile.Add("RingtailDatabaseUtility|INSTALL| \"RingtailSQLComponent(x64).exe\" /v\"IS_SQLSERVER_SERVER=172.30.206.22\\SQL2005,1440\" /v\"IS_SQLSERVER_USERNAME=user\" /v\"WEBUSER=webuser\" /v\"IS_SQLSERVER_PASSWORD=pwd\" /S /v/qn\"");
            templateFile.Add("RingtailLegalApplicationServer|INSTALL| \"RingtailLegalConfigurator.exe\" /v\"ADDLOCAL=All\" /v\"INSTALLDIR=\"\"\"c:\\Program Files (x86)\\Ringtail\"\"\"\" /v\"CONFIGURATORPORT=10000\" /S /v/qn\"");
            templateFile.Add("RingtailLegalApplicationServer|INSTALL| SomeOtherApp.exe /v\"CONFIGURATORPORT=10000\"");


            // Test that the RingtailDatabaseUtility also works.
            var expectedRequiredParameters_RingtailDatabaseUtility = new List<string>();
            expectedRequiredParameters_RingtailDatabaseUtility.Add("RingtailDatabaseUtility|IS_SQLSERVER_SERVER");
            expectedRequiredParameters_RingtailDatabaseUtility.Add("RingtailDatabaseUtility|IS_SQLSERVER_USERNAME");
            expectedRequiredParameters_RingtailDatabaseUtility.Add("RingtailDatabaseUtility|WEBUSER");
            expectedRequiredParameters_RingtailDatabaseUtility.Add("RingtailDatabaseUtility|IS_SQLSERVER_PASSWORD");

            var actualRequiredParameters = RequiredConfigurationHelper.GetReqruiedConfigsByAppForTemplates("RingtailDatabaseUtility", templateFile);
            actualRequiredParameters.ForEach(x => Console.WriteLine(x));
            Assert.AreEqual(expectedRequiredParameters_RingtailDatabaseUtility.Count, actualRequiredParameters.Count);
            expectedRequiredParameters_RingtailDatabaseUtility.ForEach(x => Assert.IsTrue(actualRequiredParameters.Exists(y => y == x)));

            // Now test that the RingtalLegalApplicationServer also works.
            var expectedRequiredParameters_RingtailLegalApplicationServer = new List<string>();
            //expectedRequiredParameters_RingtailLegalApplicationServer.Add("RingtailLegalApplicationServer|ADDLOCAL");    // ADDLOCAL is explicitly blacklisted at this time because users don't configure it.
            expectedRequiredParameters_RingtailLegalApplicationServer.Add("RingtailLegalApplicationServer|INSTALLDIR");
            expectedRequiredParameters_RingtailLegalApplicationServer.Add("RingtailLegalApplicationServer|CONFIGURATORPORT");

            actualRequiredParameters = RequiredConfigurationHelper.GetReqruiedConfigsByAppForTemplates("RingtailLegalApplicationServer", templateFile);
            actualRequiredParameters.ForEach(x => Console.WriteLine(x));
            Assert.AreEqual(expectedRequiredParameters_RingtailLegalApplicationServer.Count, actualRequiredParameters.Count);
            expectedRequiredParameters_RingtailLegalApplicationServer.ForEach(x => Assert.IsTrue(actualRequiredParameters.Exists(y => y == x)));
        }

        [TestMethod]
        public void GetRequiredConfigsTests_GetRequiredConfigsByAppForCommands()
        {
            var commands = new List<string>();
            commands.Add("DatabaseUpgrader.exe~DatabaseUpgrader|DATACAMEL_ACTION|-u IS_SQLSERVER_USERNAME|-p IS_SQLSERVER_PASSWORD|-d DATACAMEL_DATABASES");
            commands.Add("GenericInstaller.exe RingtailProcessingFramework");
            commands.Add("ConfiguratorHelper.exe~RingtailConfigurator|--host HOST|--ntDomain NT_DOMAIN|--ntUser NT_USER|--ntPassword NT_PASSWORD|--dbserver IS_SQLSERVER_SERVER|--dbsauser IS_SQLSERVER_USERNAME|--dbsapassword IS_SQLSERVER_PASSWORD|--dbname IS_SQLSERVER_DATABASE|--dbusername CONFIG_USERNAME|--dbuserpassword CONFIG_PASSWORD");

            var expected = new List<string>();
            expected.Add("DatabaseUpgrader|DATACAMEL_ACTION");
            expected.Add("DatabaseUpgrader|IS_SQLSERVER_USERNAME");
            expected.Add("DatabaseUpgrader|IS_SQLSERVER_PASSWORD");
            expected.Add("DatabaseUpgrader|DATACAMEL_DATABASES");

            var actualRequiredParameters = RequiredConfigurationHelper.GetReqruiedConfigsByAppForCommands("DatabaseUpgrader", commands);
            actualRequiredParameters.ForEach(x => Console.WriteLine(x));
            Assert.AreEqual(expected.Count, actualRequiredParameters.Count);
            expected.ForEach(x => Assert.IsTrue(actualRequiredParameters.Exists(y => y == x)));
        }

        [TestMethod]
        public void GetRequiredConfigsTests_ReadonfigsFromMultipleInstallerCommands()
        {
            var templateFile = new List<string>();
            templateFile.Add("RingtailDatabaseUtility|INSTALL| \"RingtailSQLComponent(x64).exe\" /v\"IS_SQLSERVER_SERVER=172.30.206.22\\SQL2005,1440\" /v\"IS_SQLSERVER_USERNAME=user\" /v\"WEBUSER=webuser\" /v\"IS_SQLSERVER_PASSWORD=pwd\" /S /v/qn\"");
            templateFile.Add("RingtailLegalApplicationServer|INSTALL| \"RingtailLegalConfigurator.exe\" /v\"ADDLOCAL=All\" /v\"INSTALLDIR=\"\"\"c:\\Program Files (x86)\\Ringtail\"\"\"\" /v\"CONFIGURATORPORT=10000\" /S /v/qn\"");
            templateFile.Add("RingtailLegalApplicationServer|INSTALL| SomeOtherApp.exe");

            var filtered = RequiredConfigurationHelper.FilterInstallerCommandsbyApp("RingtailDatabaseUtility", templateFile);
            Assert.IsTrue(filtered.Count == 1);
            Assert.IsTrue(filtered[0].StartsWith("RingtailDatabaseUtility"));

            filtered = RequiredConfigurationHelper.FilterInstallerCommandsbyApp("RingtailLegalApplicationServer", templateFile);
            Assert.IsTrue(filtered.Count == 2);
            Assert.IsTrue(filtered[0].StartsWith("RingtailLegalApplicationServer"));
            Assert.IsTrue(filtered[1].StartsWith("RingtailLegalApplicationServer"));
        }

        [TestMethod]
        public void GetRequiredConfigsTests_ReadConfigsFromInstallerCommand()
        {
            string template = "RingtailDatabaseUtility|INSTALL| \"RingtailSQLComponent(x64).exe\" /v\"IS_SQLSERVER_SERVER=172.30.206.22\\SQL2005,1440\" /v\"IS_SQLSERVER_USERNAME=user\" /v\"WEBUSER=webuser\" /v\"IS_SQLSERVER_PASSWORD=pwd\" /S /v/qn\"";

            var requiredConfigs = new List<string>();
            requiredConfigs.Add("IS_SQLSERVER_SERVER");
            requiredConfigs.Add("IS_SQLSERVER_USERNAME");
            requiredConfigs.Add("WEBUSER");
            requiredConfigs.Add("IS_SQLSERVER_PASSWORD");

            var actualConfigs = RequiredConfigurationHelper.GetRequiredConfigsFromARowInTheInstallerTemplatesConfigFile(template);

            actualConfigs.ForEach(x => Console.WriteLine(x));

            Assert.AreEqual(requiredConfigs.Count, actualConfigs.Count);
            requiredConfigs.ForEach(x => Assert.IsTrue(actualConfigs.Exists(y => y == x)));
        }

        [TestMethod]
        public void GetRequiredConfigsTests_GetRequiredConfigsFromARowInTheCommandsConfigFile_Test()
        {
            string template = "InstallFetcher.exe~RingtailSQL|-f BUILD_FOLDER_ROOT|-b BRANCH_NAME|-a BUILD_FOLDER_APP|-o RingtailSQL|-s BUILD_FOLDER_SUFFIX";

            var requiredConfigs = new List<string>();
            requiredConfigs.Add("BUILD_FOLDER_ROOT");
            requiredConfigs.Add("BRANCH_NAME");
            requiredConfigs.Add("BUILD_FOLDER_APP");
            requiredConfigs.Add("BUILD_FOLDER_SUFFIX");

            var cannotInclude = new List<string>();
            cannotInclude.Add("RingtailSQL");

            var actualConfigs = RequiredConfigurationHelper.GetRequiredConfigsFromARowInTheCommandsConfigFile(template);

            actualConfigs.ForEach(x => Console.WriteLine(x));
            Assert.AreEqual(requiredConfigs.Count, actualConfigs.Count);
            requiredConfigs.ForEach(x => Assert.IsTrue(actualConfigs.Exists(y => y == x)));
            cannotInclude.ForEach(x => Assert.IsFalse(actualConfigs.Exists(y => y == x)));
        }

        [TestMethod]
        public void GetRequiredConfigsTests_GetAppsForRole()
        {
            var roles = new List<string>();
            roles.Add("A|RingtailDatabaseUtility");
            roles.Add("B|RingtailDatabaseUtility");
            roles.Add("C:B");
            roles.Add("D|RingtailProcessingFramework");
            roles.Add("E:B");
            roles.Add("E:D");

            var expectedApps = new List<string>() { "RingtailDatabaseUtility" };
            var actualApp = RequiredConfigurationHelper.GetRequiredAppsByRole("A", roles);
            Assert.AreEqual(expectedApps.Count, actualApp.Count);
            expectedApps.ForEach(exp => Assert.IsTrue(actualApp.Exists(act => act == exp)));

            actualApp = RequiredConfigurationHelper.GetRequiredAppsByRole("B", roles);
            Assert.AreEqual(expectedApps.Count, actualApp.Count);
            expectedApps.ForEach(exp => Assert.IsTrue(actualApp.Exists(act => act == exp)));

            actualApp = RequiredConfigurationHelper.GetRequiredAppsByRole("C", roles);
            Assert.AreEqual(expectedApps.Count, actualApp.Count);
            expectedApps.ForEach(exp => Assert.IsTrue(actualApp.Exists(act => act == exp)));

            expectedApps = new List<string>() { "RingtailDatabaseUtility", "RingtailProcessingFramework" };
            actualApp = RequiredConfigurationHelper.GetRequiredAppsByRole("E", roles);
            Assert.AreEqual(expectedApps.Count, actualApp.Count);
            expectedApps.ForEach(exp => Assert.IsTrue(actualApp.Exists(act => act == exp)));
        }

        [TestMethod]
        public void GetRequiredConfigsTests_GetRequiredCommandsByRole()
        {
            var commands = new List<string>();
            commands.Add("DatabaseUpgrader.exe~DatabaseUpgrader|DATACAMEL_ACTION|-u IS_SQLSERVER_USERNAME|-p IS_SQLSERVER_PASSWORD|-d DATACAMEL_DATABASES");
            commands.Add("GenericInstaller.exe RingtailProcessingFramework");
            commands.Add("ConfiguratorHelper.exe~RingtailConfigurator|--host HOST|--ntDomain NT_DOMAIN|--ntUser NT_USER|--ntPassword NT_PASSWORD|--dbserver IS_SQLSERVER_SERVER|--dbsauser IS_SQLSERVER_USERNAME|--dbsapassword IS_SQLSERVER_PASSWORD|--dbname IS_SQLSERVER_DATABASE|--dbusername CONFIG_USERNAME|--dbuserpassword CONFIG_PASSWORD");

            var actual = RequiredConfigurationHelper.FilterCommandsConfigbyApp("DatabaseUpgrader", commands);
            var expected = commands[0];
            Assert.AreEqual(expected, actual[0]);

            actual = RequiredConfigurationHelper.FilterCommandsConfigbyApp("RingtailProcessingFramework", commands);
            expected = commands[1];
            Assert.AreEqual(expected, actual[0]);

            actual = RequiredConfigurationHelper.FilterCommandsConfigbyApp("RingtailConfigurator", commands);
            expected = commands[2];
            Assert.AreEqual(expected, actual[0]);
        }
    }
}
