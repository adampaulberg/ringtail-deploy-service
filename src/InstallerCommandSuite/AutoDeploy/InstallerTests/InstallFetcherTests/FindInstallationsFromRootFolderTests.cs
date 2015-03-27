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
    public class FindInstallationsFromRootFolderTests
    {

        //[TestMethod]
        //public void Test_FindInstallationsFromRootFolder()
        //{
        //    var options = new Options();
        //    options.FolderRoot = @"D:\tmp\test";
        //    options.BranchName = @"CONSOLIDATION";

        //    var file = FindInstallationsFromRootFolder.CreateFetchFile(options);

        //    Assert.IsTrue(file[0].ToLower().Contains(@"d:\tmp\test\consolidation\*"), file[0]);
        //    Assert.IsTrue(file[0].Length < 40, file[0]);

        //    options.BranchName = @"MAIN";
        //    file = FindInstallationsFromRootFolder.CreateFetchFile(options);
        //    Assert.IsTrue(file[0].ToLower().Contains(@"d:\tmp\test\main\somebuild\*"), file[0]);
        //    Assert.IsTrue(file[0].Length < 40, file[0]);

        //    ////List<string> scrubHKEy = FindIndexesOfItems();
        //    //var data = Test_FindInstallationsFromRootFolder.Read(@"D:\tmp\test.vbs");
        //    //int getIndexOfHKey = RegistryReaderScrubber.FindIndexesOfItems(data, "HKEY_LOCAL");

        //    //Assert.AreEqual(47, getIndexOfHKey);
        //    //int codeBlockIndex = RegistryReaderScrubber.GetIndexOfStartingBlock(data, getIndexOfHKey);
        //    //Assert.AreEqual(46, codeBlockIndex);
        //}

        [TestMethod]
        public void Test_FindInstallationVerionFromFile_SingleFolder()
        {
            var version = VersionByPath.BuildVersionFromPath(@"D:\build");

            Assert.AreEqual(@"D:\build", version.Path);
            Assert.AreEqual(@"8.4.100.59", version.VersionsByBuild.Values.ToList()[0]);
        }


        [TestMethod]
        public void Test_FindInstallationVerionFromFile_SkytapOptions()
        {
            var options = new Options();
            options.Output = "test";
            options.BranchName = "build";
            options.FolderRoot = @"D:\";
            var version = FindInstallationsFromRootFolder.CreateFetchCommand(options);

            Assert.IsNotNull(version);
        }

        [TestMethod]
        public void Test_FindInstallationVerionFromFile_Options()
        {
            var options = new Options();
            options.ApplicationName = "Classic";
            options.Output = "RingtailLegalApplicationServer";
            options.BranchName = "RESULTGRID";
            options.FolderRoot = @"\\sea550devbld04\builds\Ringtail\Dev";
            options.FolderSuffix = @"Deployment";
            var version = FindInstallationsFromRootFolder.CreateFetchCommand(options);

            Assert.IsNotNull(version);
        }


        [TestMethod]
        public void Test_FindInstallationVerionFromFile_Recursive()
        {
            var versions = VersionByPath.GetLatestPathForSubfolder(@"D:\build");
            Assert.IsTrue(versions.Count == 4,versions.Count.ToString());
        }

    }
}