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

         [TestMethod]
        public void CommandBlockReader_Test()
        {
            var testFileConfig = new List<string>();
            testFileConfig.Add("|ALWAYS");
            testFileConfig.Add("clean.bat");
            testFileConfig.Add("|AWESOME");
            testFileConfig.Add("doSomethingAwesome.bat");
            testFileConfig.Add("doSomethingEvenAwesomer.bat");
            testFileConfig.Add("|COOL");
            testFileConfig.Add("doSomethinCool.bat");
            testFileConfig.Add("doSomethingEvenCooler.bat");
            testFileConfig.Add("|ALWAYS");
            testFileConfig.Add("iisreset.exe");

            var result = CommandBlock.BuildCommandBlockList(testFileConfig);
            Assert.IsTrue(result.Count > 0);
        }


        [TestMethod]
        public void FilterCommandBlockByRole_LowLevelEverythingRole_Test()
        {
            var ROLE = "EVERYTHING";

            var testFileConfig = new List<string>();
            testFileConfig.Add("|ALWAYS");
            testFileConfig.Add("clean.bat");
            testFileConfig.Add("|AWESOME");
            testFileConfig.Add("doSomethingAwesome.bat");
            testFileConfig.Add("doSomethingEvenAwesomer.bat");
            testFileConfig.Add("|COOL");
            testFileConfig.Add("doSomethinCool.bat");
            testFileConfig.Add("doSomethingEvenCooler.bat");
            testFileConfig.Add("|ALWAYS");
            testFileConfig.Add("iisreset.exe");


            var rolesConfig = new List<string>();
            rolesConfig.Add("EVERYTHING|AWESOME");
            rolesConfig.Add("EVERYTHING|COOL");
            rolesConfig.Add("SUPER:EVERYTHING");

            var commands = CommandBlock.BuildCommandBlockList(testFileConfig);
            var values = ValuesForRole.BuildValuesForRole(ROLE, rolesConfig);

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

            var testFileConfig = new List<string>();
            testFileConfig.Add("|ALWAYS");
            testFileConfig.Add("clean.bat");
            testFileConfig.Add("|AWESOME");
            testFileConfig.Add("doSomethingAwesome.bat");
            testFileConfig.Add("doSomethingEvenAwesomer.bat");
            testFileConfig.Add("|COOL");
            testFileConfig.Add("doSomethinCool.bat");
            testFileConfig.Add("doSomethingEvenCooler.bat");
            testFileConfig.Add("|ALWAYS");
            testFileConfig.Add("iisreset.exe");


            var rolesConfig = new List<string>();
            rolesConfig.Add("EVERYTHING|AWESOME");
            rolesConfig.Add("EVERYTHING|COOL");
            rolesConfig.Add("SUPER:EVERYTHING");

            var commands = CommandBlock.BuildCommandBlockList(testFileConfig);
            var values = ValuesForRole.BuildValuesForRole(ROLE, rolesConfig);

            var result = RoleResolver.FilterCommandsByRoles(values, commands);
            int expectedCount = 0;
            result.ForEach(x => Console.WriteLine(x));
            commands.ForEach(x => expectedCount += x.Commands.Count);

            Assert.AreEqual(expectedCount, result.Count);
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
