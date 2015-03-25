using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using System.Linq;
using RoleResolverUtility;
using RoleResolverUtility.Util;


namespace InstallerTests
{
    [TestClass]
    public class RoleResolverTests
    {
        public class ConsoleOut : IOutputEngine
        {
            public void Output(List<string> s)
            {
                foreach (var x in s)
                {
                    Console.WriteLine(x);
                }
            }
        }

        //[TestMethod]
        //public void ReadRoleFile()
        //{
        //    Options opts = new Options();
        //    opts.Role = "WEBSERVER";

        //    List<string> mockRoleFile = new List<string>();
        //    mockRoleFile.Add("WEBSERVER|RingtailLegalApplicationServer");
        //    mockRoleFile.Add("WEBSERVER|Ringtail8");
        //    mockRoleFile.Add("WEBSERVER|Deployer");

        //    List<string> masterCommands = new List<string>();
        //    masterCommands.Add("@echo Starting %time%");
        //    masterCommands.Add("clean.bat");
        //    masterCommands.Add("fetch-RingtailLegalApplicationServer.bat");
        //    masterCommands.Add("fetch-RingtailDatabaseUtility.bat");
        //    masterCommands.Add("--fetch-RingtailSQL.bat");
        //    masterCommands.Add("fetch-RingtailProcessingFramework.bat");
        //    masterCommands.Add("InstallNameTruncator.exe /r");
        //    masterCommands.Add("iisreset.exe");
        //    masterCommands.Add("--install-RingtailDatabaseUtility.bat");
        //    masterCommands.Add("--install-RingtailLegalApplicationServer.bat");
        //    masterCommands.Add("--install-RingtailProcessingFramework.bat");
        //    masterCommands.Add("--install-RingtailLegalAgentServer.bat");
        //    masterCommands.Add("--iisreset.exe");
        //    masterCommands.Add("--Deployer.exe");
        //    masterCommands.Add("--DataCamel.exe upgrade -u sa_Jake -p rs-101 -d localPortal,bullerEmail,bullerRPF");
        //    masterCommands.Add("@echo Complete %time%");

        //    Logger l = new Logger(new ConsoleOut());

        //    RoleResolver rr = new RoleResolver(opts);
        //    rr.Logger = l;
        //    var configs = ConfigDictionary.BuildConfigDictionary(mockRoleFile, l);
        //    if (configs == null)
        //    {
        //        l.Write(String.Empty);
        //    }

        //    Assert.IsNotNull(configs);

        //    int resultCode = rr.Go(configs, masterCommands);

        //    l.Write(String.Empty);
        //    Assert.AreEqual(0, resultCode);

        //}

        [TestMethod]
        public void CommandBlockReader_Test()
        {
            var masterCommands = SimpleFileReader.Read(@"D:\TestFile.config");

            var result = CommandBlock.BuildCommandBlockList(masterCommands);
            Assert.IsTrue(result.Count > 0);
        }

        [TestMethod]
        public void ValuesForRole_BuildValuesForRole_LowLevel_Test()
        {
            var roleFile = SimpleFileReader.Read(@"D:\roles.config");
            var values = ValuesForRole.BuildValuesForRole("DEV-FULL", roleFile);
            Assert.AreEqual(6, values.Count);
        }


        [TestMethod]
        public void ValuesForRole_BuildValuesForRole_Compositional_Test()
        {
            var roleFile = SimpleFileReader.Read(@"D:\roles.config");
            var values = ValuesForRole.BuildValuesForRole("SUPER", roleFile);
            Assert.AreEqual(19, values.Count);
        }

        [TestMethod]
        public void FilterCommandBlockByRole_LowLevelEverythingRole_Test()
        {
            var ROLE = "ALLINONE";
            var commands = CommandBlock.BuildCommandBlockList(SimpleFileReader.Read(@"D:\TestFile.config"));
            var values = ValuesForRole.BuildValuesForRole(ROLE, SimpleFileReader.Read(@"D:\roles.config"));

            var result = RoleResolver.FilterCommandsByRoles(values, commands);
            int expectedCount = 0;
            result.ForEach(x => Console.WriteLine(x));
            commands.ForEach(x => expectedCount += x.Commands.Count);

            Assert.AreEqual(expectedCount, result.Count);
        }


        [TestMethod]
        public void FilterCommandBlockByRole_HighLevelEverythingRole_Test()
        {
            var ROLE = "SUPER";
            var commands = CommandBlock.BuildCommandBlockList(SimpleFileReader.Read(@"D:\TestFile.config"));
            var values = ValuesForRole.BuildValuesForRole(ROLE, SimpleFileReader.Read(@"D:\roles.config"));

            var result = RoleResolver.FilterCommandsByRoles(values, commands);
            int expectedCount = 0;
            result.ForEach(x => Console.WriteLine(x));
            commands.ForEach(x => expectedCount += x.Commands.Count);

            Assert.AreEqual(expectedCount, result.Count);
        }

        [TestMethod]
        public void FilterCommandBlockByRole_Resolution_Test()
        {
            var ROLE = "SKYTAP-ALLINONE";
            var commands = SimpleFileReader.Read(@"D:\Upgrade\fourServer\masterCommands.config");
            var roles = SimpleFileReader.Read(@"D:\Upgrade\fourServer\roles.config");

            var opts = new Options();
            opts.Role = ROLE;
            RoleResolver r = new RoleResolver(opts, new Logger());

            var result = r.FilterMasterCommandsByRole(commands, roles);
            //Assert.AreEqual(0, result.Count);
        }


        [TestMethod]
        public void FilterCommandBlockByRole_MULTIPLEROLES_Resolution_Test()
        {
            var opts = new Options();

            var commands = new List<string>();
            commands.Add("|ALWAYS");
            commands.Add("always");
            commands.Add("|1");
            commands.Add("a");
            commands.Add("|2");
            commands.Add("a");
            commands.Add("|3");
            commands.Add("b");
            commands.Add("|ALWAYS");
            commands.Add("always");

            var roles = new List<string>();
            roles.Add("A|1");
            roles.Add("A|2");
            roles.Add("C:A");
            roles.Add("C:B");
            roles.Add("B|3");


            var ROLE = "A";
            opts.Role = ROLE;
            var result = new RoleResolver(opts, new Logger()).FilterMasterCommandsByRole(commands, roles);
            Assert.AreEqual(4, result.Count);


            opts.Role = "B";
            result = new RoleResolver(opts, new Logger()).FilterMasterCommandsByRole(commands, roles);
            Assert.AreEqual(3, result.Count);


            opts.Role = "A,B";
            result = new RoleResolver(opts, new Logger()).FilterMasterCommandsByRole(commands, roles);
            Assert.AreEqual(5, result.Count);

            opts.Role = "C";
            result = new RoleResolver(opts, new Logger()).FilterMasterCommandsByRole(commands, roles);
            Assert.AreEqual(5, result.Count);

            opts.Role = "C,A";
            result = new RoleResolver(opts, new Logger()).FilterMasterCommandsByRole(commands, roles);
            Assert.AreEqual(5, result.Count);
        }

         [TestMethod]
        public void ValuesForRole_BuildValuesForRole_ListOfRoles()
        {
            var roles = new List<string>();
             roles.Add("A|1");
             roles.Add("B|2");
             roles.Add("C:A");
             roles.Add("C:B");
             roles.Add("D|3");

             var result = ValuesForRole.BuildValuesForRole("A,B", roles.AsEnumerable());
             Assert.AreEqual(result.Count, 2);

             result = ValuesForRole.BuildValuesForRole("C", roles.AsEnumerable());
             Assert.AreEqual(result.Count, 2);

             result = ValuesForRole.BuildValuesForRole("C,D", roles.AsEnumerable());
             Assert.AreEqual(result.Count, 3);
        }

         [TestMethod]
         public void ValuesForRole_WithSimilarRole_FindsOnlyExactMatch()
         {
             var roles = new List<string>();
             roles.Add("A|1");
             roles.Add("AA|2");             

             var result = ValuesForRole.BuildValuesForRole("A", roles.AsEnumerable());
             Assert.AreEqual(result.Count, 1);
         }

         [TestMethod]
         public void ValuesForRole_WithSimilarCompRole_FindsOnlyExactMatch()
         {
             var roles = new List<string>();
             roles.Add("A:B");
             roles.Add("AA:B");
             roles.Add("B|1");

             var result = ValuesForRole.BuildValuesForRole("A", roles.AsEnumerable());
             Assert.AreEqual(result.Count, 1);
         }

         [TestMethod]
         public void FilterCommandsByRoles_WithSimilarRole_FindsOnlyExactMatches()
         {
             var opts = new Options();

             var commands = new List<string>();
             commands.Add("|App1");
             commands.Add("App1");
             commands.Add("|App12");
             commands.Add("App12");             

             var roles = new List<string>();
             roles.Add("RoleA|App1");
             roles.Add("RoleB|App12");             

             var ROLE = "RoleA";
             opts.Role = ROLE;
             var result = new RoleResolver(opts, new Logger()).FilterMasterCommandsByRole(commands, roles);
             Assert.AreEqual(1, result.Count);
             Assert.AreEqual("App1", result[0]);
         }
    }
}
