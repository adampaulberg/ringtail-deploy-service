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

        public static List<string> CreateFetchCommand_SingleDropFolder(Options options, IExiter exiter)
        {
            List<string> copyCommands = null;
            List<string> log = new List<string>();

            string applicationName = options.ApplicationName;
            options.ApplicationName = null;
            string root = BuildRootFolderFromOptions(options);
            options.ApplicationName = applicationName;
            DirectoryInfo rootFolder = new DirectoryInfo(root);

            Console.WriteLine("Looking in root folder: " + rootFolder.FullName);

            if (rootFolder.Exists)
            {
                Console.WriteLine("    ....folder exists: " + root);
                var folders = rootFolder.GetDirectories();
                var files = rootFolder.GetFiles();

                Console.WriteLine("Looking in root folder: " + rootFolder.FullName);

                if (folders.Length > 0 || files.Length > 0)
                {
                    DirectoryInfo finalFolder = GetFirstFolderWithFiles(rootFolder, options.FolderSuffix);

                    Console.WriteLine("Looking in final folder: " + finalFolder.FullName);

                    if (finalFolder != null)
                    {
                        copyCommands = new List<string>();

                        var installerName = options.ApplicationName;
                        var folderFiles = finalFolder.GetFiles().ToArray();
                        if (!folderFiles.Any(x => x.Name.StartsWith(installerName)))
                        {
                            Console.WriteLine("Could not find a build for: " + finalFolder.FullName + " - " + installerName);
                            exiter.OnExit(1);
                        }

                        string realPath = finalFolder.FullName;
                        string iName = installerName + "*.exe";
                        var command = "robocopy \"" + realPath + "\"" + " . " + "\"" + iName + "\"" + " /V /NFL";
                        copyCommands.Add(command);
                        copyCommands.Add("IF ERRORLEVEL 1 SET ERRORLEV=0");
                        copyCommands.Add("IF ERRORLEVEL 2 SET ERRORLEV=0");
                        copyCommands.Add("IF ERRORLEVEL 3 SET ERRORLEV=0");
                        copyCommands.Add("IF ERRORLEVEL 4 SET ERRORLEV=0");
                        copyCommands.Add("IF ERRORLEVEL 4 SET ERRORLEV=0");
                    }
                    else
                    {
                        Console.WriteLine("Could not find a build folder with builds in this location:" + root);
                    }
                }
                else
                {
                    Console.WriteLine("The target folder was accessible, but no builds have been made yet: " + root);
                    exiter.OnExit(1);
                }
            }
            else
            {
                Console.WriteLine("The target folder was not accessible or not found: " + root);
                exiter.OnExit(1);
            }

            return copyCommands;
        }
  
        public static List<string> CreateFetchCommand(Options options, IExiter exiter)
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
                    exiter.OnExit(1);
                }
            }
            else
            {
                Console.WriteLine("The target folder was not accessible or not found: " + root);
                exiter.OnExit(1);
            }

            return copyCommands;
        }

        public static List<string> CreateFetchCommand(Options options)
        {
            var fetchFile = new List<string>();
           
            if (options.Version == "1")
            {
                Console.WriteLine("...v1");
                fetchFile = CreateFetchCommand(options, new Exiter());
            }
            else if (options.Version == "2")
            {
                Console.WriteLine("...v2");

                var x = SimpleFileReader.Read("master.config");
                if (x.Contains("fetch-" + options.Output + ".bat"))
                {
                    fetchFile = CreateFetchCommand_SingleDropFolder(options, new Exiter());
                }
                else
                {
                    Console.WriteLine("This application isn't being used by the chosen role, so no fetch will be performed.");
                }
            }
            else
            {
                Console.WriteLine("Version not supported: " + options.Version);
                new Exiter().OnExit(1);
            }

            return fetchFile;
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


    public interface IExiter
    {
        void OnExit(int exitCode);
    }

    internal class Exiter : IExiter
    {
        public void OnExit(int exitCode)
        {
            Environment.Exit(1);
        }
    }
}
