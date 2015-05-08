using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using InstallerService.Helpers;
using System.IO;
using InstallerService;

namespace InstallerServiceTests.HelperTests
{
    [TestClass]
    public class FileHelpersTests
    {
        [TestMethod]
        public void TestWildcardReplacement()
        {
            var data = SimpleFileReader.Read(@"D:\Upgrade\fourServer\volitleData.config");
            string pattern = "webserver01";
            string newValue = "allinone";

            int hits = data.FindAll(x => x.Contains(pattern)).Count;
            Assert.IsTrue(hits > 0, "The original file should have some hits on the pattern.");

            var fileResult = FileHelperResult.UpsertWildcardKeys(data, "*" + pattern, newValue);

            Assert.IsTrue(fileResult.IsSuccessful, "Result should be successful");
            Assert.AreEqual(data.Count, fileResult.NewFile.Count, "Shouldn't add or remove keys");


            Assert.AreEqual(0, fileResult.NewFile.FindAll(x => x.Contains(pattern)).Count, "Shouldn't find the pattern in the data anymore");
            Assert.IsTrue(fileResult.NewFile.FindAll(x => x.Contains(newValue)).Count >= hits, "Should find the new value at least as many times as the pattern appeared originally");
        }

        [TestMethod]
        public void TestKeyInsertion()
        {
            var data = SimpleFileReader.Read(@"D:\Upgrade\fourServer\volitleData.config");
            string key = "someExistingApp|someNewKey";
            string newValue = "thisIsTheNewValue";
            string expectedNewLine = "someExistingApp|someNewKey=\"thisIsTheNewValue\"";

            int hits = data.FindAll(x => x.Contains(key)).Count;
            Assert.IsTrue(hits == 0, "The original file should have some no hits on the new key.");

            var fileResult = FileHelperResult.UpsertWildcardKeys(data, key, newValue);

            Assert.IsTrue(fileResult.IsSuccessful, "Result should be successful.");
            Assert.AreEqual(data.Count + 1, fileResult.NewFile.Count, "Should find one more key than originally.");

            Assert.AreEqual(1, fileResult.NewFile.FindAll(x => x.Contains(key)).Count, "Should find exactly 1 hit for the new key");
            Assert.AreEqual(expectedNewLine, fileResult.NewFile[fileResult.NewFile.Count - 1], "Should find the exact new value at the end of the file.");
        }

        [TestMethod]
        public void TestKeyChangeExistingKey()
        {
            var data = SimpleFileReader.Read(@"D:\Upgrade\fourServer\volitleData.config");
            string key = "RoleResolver|ROLE";
            string newValue = "dummyValue";
            string expectedChangedValue = "RoleResolver|ROLE=\"dummyValue\"";

            var hitsObj = data.FindAll(x => x.Contains(key));
            int hits = hitsObj.Count;
            Assert.IsTrue(hits == 1, "The original file should have exactly one hit.");

            var firstHit = hitsObj[0];

            //fileResult.NewFile.FindAll(x => x.Contains(expectedChangedValue))

            var fileResult = FileHelperResult.UpsertWildcardKeys(data, key, newValue);

            Assert.IsTrue(fileResult.IsSuccessful, "Result should be successful.");
            Assert.AreEqual(data.Count, fileResult.NewFile.Count, "Should find the same number of total rows.");

            Assert.AreEqual(1, fileResult.NewFile.FindAll(x => x.Contains(expectedChangedValue)).Count, "Should find exactly 1 hit for the new value");
            Assert.AreEqual(0, fileResult.NewFile.FindAll(x => x.Contains(firstHit)).Count, "Should not find the old value in the file");

        }

        //[TestMethod]
        //public void FileHelpers_ChangeConfigItemWithWildcards_Test()
        //{
        //    string pattern = "webserver01";
        //    string newValue = "allinone";

        //    var result = FileHelpers.ChangeFiles(@"_TEST.config", "*" + pattern, newValue, @"D:\tmp\");

        //    Assert.IsNotNull(result);
        //}


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
