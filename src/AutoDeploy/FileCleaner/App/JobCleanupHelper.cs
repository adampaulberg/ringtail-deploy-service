using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FileCleaner.App
{

    public class JobCleanupOptions
    {
        public string Path { get; set; }
        public string FilterStartsWithCriteria { get; set; }
        public string FilterExtensionsCriteria { get; set; }

    }


    public class JobCleanupHelper
    {

        public static int RunCleanup(JobCleanupOptions jobCleanupoptions)
        {
            DirectoryInfo di = new DirectoryInfo(jobCleanupoptions.Path);

            var files = di.GetFiles().ToList();
            var filteredFiles = GetfilteredFiles(files, jobCleanupoptions);

            if (filteredFiles.Count == 0)
            {
                Console.WriteLine("No files found that needed to be cleaned up.");
                return 0;
            }

            var exitCode = 0;

            for (int i = 0; i < 5; i++)
            {
                exitCode = DeleteFiles(filteredFiles);

                if (exitCode == 0)
                {
                    break;
                }

                System.Threading.Thread.Sleep(4000);
            }

            if (exitCode == 0)
            {
                System.Threading.Thread.Sleep(4000);
                files = di.GetFiles().ToList();
                filteredFiles = GetfilteredFiles(files, jobCleanupoptions);

                if (filteredFiles.Count > 0)
                {
                    exitCode = 1;
                    Console.WriteLine("Some files that were marked for deletion still exist after the delete appeared to succeed.");
                    filteredFiles.ForEach(x => Console.WriteLine(" file that was undeletable: " + x.Name));
                }
            }

            if (exitCode != 0)
            {
                Console.WriteLine("Some files that were marked for deletion could not be deleted.\nA system reboot and then a retry is recommended.\n");
            }
            else
            {
                Console.WriteLine("OK");
            }

            return exitCode;

        }

        public static List<FileInfo> GetfilteredFiles(List<FileInfo> unfilteredfiles, JobCleanupOptions options)
        {
            var filteredFiles = new List<FileInfo>();

            if (String.IsNullOrEmpty(options.FilterStartsWithCriteria) && String.IsNullOrEmpty(options.FilterExtensionsCriteria))
            {
                Console.WriteLine("No filtering criteria provided.  This tool will not attempt to delete all files.");
                return filteredFiles;
            }

            foreach (var x in unfilteredfiles)
            {
                if (!String.IsNullOrEmpty(options.FilterStartsWithCriteria))
                {
                    if (!x.Name.StartsWith(options.FilterStartsWithCriteria))
                    {
                        continue;
                    }
                }
                if (!String.IsNullOrEmpty(options.FilterExtensionsCriteria))
                {
                    if (!(x.Extension.Contains(options.FilterExtensionsCriteria)))
                    {
                        continue;
                    }
                }

                filteredFiles.Add(x);
            }
            return filteredFiles;
        }

        private static int DeleteFiles(List<FileInfo> files)
        {
            int exitCode = 0;

            foreach (var x in files)
            {
                try
                {
                    x.Delete();
                }
                catch (Exception ex)
                {
                    exitCode = 1;
                    Console.WriteLine("");
                    Console.WriteLine("Could not delete file: " + x.Name);
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("");
                    break;
                }
            }

            return exitCode;
        }
    }
}
