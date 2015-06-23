using InstallFetcher.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace InstallFetcher.App
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
                    var fetchFileContents = FindInstallationsFromRootFolder.CreateFetchCommand(options);
                    exitCode = WriteFetchFile(options, fetchFileContents);
                }
                else
                {
                    Console.WriteLine("NO FETCH FILE WRITTEN");
                }
            }
            catch( Exception ex)
            {
                Console.WriteLine("Unknown error with the fetcher.");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                exitCode = 1;
            }

            return exitCode;
        }

        private static int WriteFetchFile(Options options, List<string> fetchFileContents)
        {
            int exitCode = 0;
            if (fetchFileContents != null)
            {
                var outFile = "fetch.bat";

                if (!String.IsNullOrEmpty(options.Output))
                {

                    outFile = "fetch-" + options.Output + ".bat";
                }

                SimpleFileWriter.Write(outFile, fetchFileContents);

                if (!new FileInfo(outFile).Exists)
                {
                    Console.WriteLine("Failed to write " + outFile);
                    exitCode = 1;
                }
            }

            return exitCode;
        }
    }

}
