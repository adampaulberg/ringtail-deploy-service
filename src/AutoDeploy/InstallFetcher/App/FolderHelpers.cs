using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InstallFetcher.Util;

namespace InstallFetcher.App
{
    public class FindInstallationsFromRootFolder
    {
        public static List<string> CreateFetchFile_OLD(Options options)
        {
            List<string> s = new List<string>();

            var root = options.FolderRoot;
            if (!String.IsNullOrEmpty(options.BranchName))
            {
                root += @"\" + options.BranchName;
            }

            if (!String.IsNullOrEmpty(options.ApplicationName))
            {
                root += @"\" + options.ApplicationName;
            }

            DirectoryInfo di = new DirectoryInfo(root);

            if (di.Exists)
            {
                Console.WriteLine("    ....folder exists: " + root);
                var folders = di.GetDirectories();
                if (folders.Length > 0)
                {
                    var realPath = root;
                    var orderedFolders = folders.ToList().OrderBy(x => x.CreationTimeUtc).ToList();

                    var files = di.GetFiles().ToList();

                    if (!files.Any(x => x.Extension == "exe"))
                    {
                        var specificBuildFolder = orderedFolders[orderedFolders.Count - 1];
                        realPath += @"\" + specificBuildFolder;
                    }

                    if (!String.IsNullOrEmpty(options.FolderSuffix))
                    {
                        realPath += @"\" + options.FolderSuffix;
                    }

                    di = new DirectoryInfo(realPath);
                    if (!di.Exists || di.GetFiles().Length == 0)
                    {
                        var specificBuildFolder = orderedFolders[orderedFolders.Count - 2];
                        realPath = root + @"\" + specificBuildFolder;

                        if (!String.IsNullOrEmpty(options.FolderSuffix))
                        {
                            realPath += @"\" + options.FolderSuffix;
                        }
                    }

                    di = new DirectoryInfo(realPath);
                    if (!di.Exists || di.GetFiles().Length == 0)
                    {
                        Console.WriteLine("Could not find a build folder with builds in this location:" + root);
                        //Environment.Exit(1);
                        return null;
                    }

                    realPath += @"\" + "*.exe";
                    var command = "xcopy \"" + realPath + "\"" + " /d";
                    s.Add(command);

                    return s;

                }
                else
                {
                    Console.WriteLine("The target folder was accessible, but no builds have been made yet: " + options.FolderRoot);
                    Environment.Exit(1);
                }
            }
            else
            {
                Console.WriteLine("The target folder was not accessible or not found: " + options.FolderRoot);
                Environment.Exit(1);
            }

            return s;
        }

        public static List<string> CreateFetchCommand(Options options)
        {
            List<string> copyCommands = null;
            List<string> log = new List<string>();

            string root = BuildRootFolderFromOptions(options);
            DirectoryInfo rootFolder = new DirectoryInfo(root);

            if (rootFolder.Exists)
            {
                Console.WriteLine("    ....folder exists: " + root);
                var folders = rootFolder.GetDirectories();
                var files = rootFolder.GetFiles(); 

                if (folders.Length > 0 || files.Length > 0)
                {                       
                    DirectoryInfo finalFolder = GetFirstFolderWithFiles(rootFolder, options.FolderSuffix);

                    if (finalFolder != null)
                    {
                        string realPath = finalFolder.FullName;
                        var command = "robocopy \"" + realPath + "\"" + " . /V /NFL";
                        copyCommands = new List<string>();
                        copyCommands.Add(command);

                        copyCommands.Add("IF ERRORLEVEL 1 SET ERRORLEV=0");
                        copyCommands.Add("IF ERRORLEVEL 2 SET ERRORLEV=0");
                        copyCommands.Add("IF ERRORLEVEL 3 SET ERRORLEV=0");
                        copyCommands.Add("IF ERRORLEVEL 4 SET ERRORLEV=0");
                        copyCommands.Add("IF ERRORLEVEL 4 SET ERRORLEV=0");

                        Console.WriteLine("Found a good folder: " + realPath);
                    }
                    else
                    {
                        Console.WriteLine("Could not find a build folder with builds in this location:" + root);
                    }
                }
                else
                {
                    Console.WriteLine("The target folder was accessible, but no builds have been made yet: " + root);
                    Environment.Exit(1);
                }
            }
            else
            {
                Console.WriteLine("The target folder was not accessible or not found: " + root);
                Environment.Exit(1);
            }

            return copyCommands;
        }

        #region Subroutines

        private static string BuildRootFolderFromOptions(Options options)
        {
            var root = options.FolderRoot;
            if (!String.IsNullOrEmpty(options.BranchName))
            {
                root += @"\" + options.BranchName;
            }

            if (!String.IsNullOrEmpty(options.ApplicationName))
            {
                root += @"\" + options.ApplicationName;
            }
            return root;
        }

        private static DirectoryInfo GetFirstFolderWithFiles(DirectoryInfo rootFolder, string folderSuffix)
        {
            DirectoryInfo foundFolder = null;

            if (rootFolder.HasFiles())
            {
                foundFolder = rootFolder;
            }
            else
            {
                var orderedFolders = rootFolder.GetDirectories().ToList().ToList().OrderBy(x => x.CreationTimeUtc).ToList();
                orderedFolders.Reverse();

                foreach( var itm in orderedFolders) 
                {
                    DirectoryInfo working = itm.NormalizeFolderBySuffix(folderSuffix);
                    if (working.HasFiles())
                    {
                        foundFolder = working;
                        break;
                    }
                }
            }

            return foundFolder;
        }

        #endregion
    }
}
