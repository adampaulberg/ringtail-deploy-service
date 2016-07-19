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

                files = files.ToList().FindAll(x => x.Extension != ".txt").ToArray();


                foreach(var x in files)
                {
                    Console.WriteLine("         found file: " + x.Name + " | " + x.Extension);
                }

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
                            
                            if (options.GetErrorLevel() > 0)
                            {
                                Console.WriteLine("Could not find a build for: " + finalFolder.FullName + " - " + installerName);
                                exiter.OnExit(1);
                            }
                            else
                            {
                                Console.WriteLine("Build not found - but this build marked as optional: " + finalFolder.FullName + " - " + installerName);
                                Console.WriteLine("Writing out omission file so that later steps know to ignore this optional installer: " +  "omit-" + installerName);
                                SimpleFileWriter.Write("omit-" + installerName + ".log", new List<string>());
                                return copyCommands;
                            }
                        }

                        if (options.Version == "2")  
                        {
                            // Not a great solution.... this works around some naming problems, but its horribly hard-coded and specific.
                            // TODO: Consider replacing with a check to find a file where the Product Details name matches, minus whitespace?
                            installerName += "_";
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
                    if (options.GetErrorLevel() == 0)
                    {
                        Console.WriteLine("Writing out omission file so that later steps know to ignore this optional installer: " + "omit-" + options.ApplicationName);
                        SimpleFileWriter.Write("omit-" + options.ApplicationName + ".log", new List<string>());
                        exiter.OnExit(0);
                    }
                    else
                    {
                        exiter.OnExit(1);
                    }
                }
            }
            else
            {
                Console.WriteLine("The target folder was not accessible or not found: " + root);

                if (options.GetErrorLevel() == 0)
                {
                    Console.WriteLine("Writing out omission file so that later steps know to ignore this optional installer: " + "omit-" + options.ApplicationName);
                    SimpleFileWriter.Write("omit-" + options.ApplicationName + ".log", new List<string>());
                    exiter.OnExit(0);
                }
                else
                {
                    exiter.OnExit(1);
                }
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
                    Console.WriteLine("Writing out omission file so that later steps know to ignore this optional installer: " + "omit-" + options.ApplicationName);
                    SimpleFileWriter.Write("omit-" + options.ApplicationName + ".log", new List<string>());
                    Console.WriteLine("This application isn't being used by the chosen role, so no fetch will be performed.");
                    Console.WriteLine("      If you think you got this in error, check to see if master.config has a line for this fetch-" + options.Output + ".bat");
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

            bool goDeeper = false;

            if (rootFolder.HasFiles())
            {
                foundFolder = rootFolder;

                if(rootFolder.GetFiles().ToList().FindAll(x => x.Extension != ".txt").Count == 0)
                {
                    goDeeper = true;
                }

            }
            
            if(goDeeper)
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
            Environment.Exit(exitCode);
        }
    }
}
