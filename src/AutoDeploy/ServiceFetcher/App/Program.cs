using ServiceFetcher.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace ServiceFetcher.App
{
    class Program
    {
        static int Main(string[] args)
        {
            int exitCode = 0;
            try
            {
                var options = new Options();

                if (CommandLine.Parser.Default.ParseArguments(args, options))
                {
                    Cleanup(options);
                    var fetchFileContents = FindInstallationsFromRootFolder.CreateFetchCommand(options);
                    exitCode = WriteFetchFile(options, fetchFileContents);
                }
                else
                {
                    Console.WriteLine("NO FETCH FILE WRITTEN");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unknown error with the fetcher.");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                exitCode = 1;
            }

            return exitCode;
        }

        private static void Cleanup(Options options)
        {
            FileInfo fi = new FileInfo("omit-" + options.ApplicationName + ".log");
            if (fi.Exists)
            {
                fi.Delete();
            }
        }

        private static int WriteFetchFile(Options options, List<string> fetchFileContents)
        {
            int exitCode = 0;
            if (fetchFileContents != null && fetchFileContents.Count > 0)
            {
                var outFile = "fetch.bat";

                if (!String.IsNullOrEmpty(options.Output))
                {
                    outFile = "fetch-" + options.Output + ".bat";
                }

                SimpleFileWriter.Write(outFile, fetchFileContents);

                // *hack * this file gets written for real later on when ServiceInstaller.exe is called.
                //      However, without writing this as a dummy file now - that line is filtered out of master.bat by the Composer.
                 SimpleFileWriter.Write("deploy-" + options.ApplicationName + ".bat", new List<string>());  

                if (!new FileInfo(outFile).Exists)
                {
                    Console.WriteLine("Failed to write " + outFile);
                    exitCode = 1;
                }

                Console.WriteLine("Wrote out: " + outFile);
            }

            return exitCode;
        }
    }

}
