using ServiceInstaller.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceInstaller
{
    class Program
    {
        static int Main(string[] args)
        {
            var exitCode = 0;
            Console.WriteLine("ServiceInstaller starting for " + args[0]);


            if(args.Length == 0)
            {
                GetUsage();
                return 2;
            }

            string appName = args[0];

            try
            {
                ServiceInstallerHelper.RunIt(appName);
                List<string> s = new List<string>();
                s.Add(args[0]);
                s.Add("Ok");
                SimpleFileWriter.Write("ServiceInstallerLog-" + appName + ".txt", s);
            }
            catch (Exception ex)
            {
                List<string> s = new List<string>();
                s.Add(ex.Message);
                s.Add(ex.StackTrace);
                SimpleFileWriter.Write("ServiceInstallerLog-" + appName + ".txt", s);

                Console.WriteLine("ServiceInstaller error");
                s.ForEach(x => Console.WriteLine(s));

                exitCode = 1;
            }

            return exitCode;
        }

        public static void GetUsage()
        {
            Console.WriteLine("ServiceInstaller - ");
            Console.WriteLine("  Usage:    ServiceInstaller.exe [appName]");
            Console.WriteLine("  This will create a batch file that calls the DeployToIIS.exe to unpack a zip file and install an IIS website for it.");
            Console.WriteLine("     It uses a convention based approach - so all you need to pass in is the service name.");
        }

        internal class DynamicExclusionDetector
        {
            public static List<string> DetectExclusions(string appName)
            {
                var list = new List<string>();

                var di = new DirectoryInfo(Environment.CurrentDirectory);

                var files = di.GetFiles();

                foreach (var f in files.ToList())
                {
                    try
                    {
                        var fileName = f.Name;
                        if (fileName.StartsWith("omit-"))
                        {
                            var omission = fileName.Split('-')[1];
                            omission = omission.Split('.')[0];

                            list.Add(omission);

                            if (omission.Contains(appName))
                            {
                                Console.WriteLine("found omission: " + omission);
                            }
                        }
                    }
                    catch
                    {
                        Console.WriteLine("Minor problem reading omission files.  Continuing.");
                    }
                }

                return list;
            }
        }

        public class ServiceInstallerHelper
        {
            public static void RunIt(string appName)
            {
                List<string> exclusions = DynamicExclusionDetector.DetectExclusions(appName);

                if (exclusions.Contains(appName))
                {
                    Console.WriteLine("ServiceInstaller found exclusion for: " + appName);
                    var noOpFile = new List<string>();
                    noOpFile.Add("@echo SKIPPING");
                    SimpleFileWriter.Write("install-" + appName + ".bat", noOpFile);
                    return;
                }

                string volitileData = "volitleData.config";
                string applicationName = appName;

                Console.WriteLine("ServiceInstaller Reading.... " + applicationName);

                var volitileDataList = SimpleFileReader.Read(volitileData);
                var filledInParameters = new List<string>();

                NormalizeZipName(applicationName);
                var unzipCmd = GenerateUnzipCommand(applicationName);

                var iisInstallCmd = GenerateIISInstallCommand(volitileDataList, applicationName);
                filledInParameters.Add(iisInstallCmd);


                Console.WriteLine("ServiceInstaller Writing to file.... " + "deploy-" + applicationName + ".bat");
                SimpleFileWriter.Write("deploy-" + applicationName + ".bat", filledInParameters);
            }

            public static bool GenerateUnzipCommand(string applicationName)
            {
                try
                {
                    Console.WriteLine("Unzipping");
                    string zipPath = @"C:\Upgrade\AutoDeploy\" + applicationName + @"\" + applicationName + ".zip";
                    string extractPath = @"C:\Upgrade\AutoDeploy\" + applicationName + @"\" + applicationName;

                    System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractPath);
                    return true;
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    return false;
                }
            }

            public static void NormalizeZipName(string applicationName)
            {
                var path = @"C:\Upgrade\AutoDeploy\" + applicationName;
                DirectoryInfo di = new DirectoryInfo(path);

                if(di.Exists)
                {
                    var files = di.GetFiles().ToList();

                    foreach (var f in files)
                    {
                        if (f.Name.Contains(applicationName) && f.Extension.ToLower().Contains("zip"))
                        {
                            Console.WriteLine("....copying with normalized name");

                            var target = path + @"\" + applicationName + ".zip";
                            FileInfo fi = new FileInfo(target);

                            if (!fi.Exists)
                            {
                                f.CopyTo(target);
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("....WARNING: folder did not exist: " + path);
                }
            }

            public static string GenerateIISInstallCommand(List<string> volitileData, string applicationName)
            {
                Console.WriteLine("...starting to generate the IIS installation command");

                var appConfigs = volitileData.FindAll(x => x.StartsWith(applicationName));

                foreach(var x in appConfigs)
                {
                    Console.WriteLine("... kv: " + x);
                }

                //Ringtail-Svc-ContentSearch|Username="LM\#rpfdev_account"
                //Ringtail-Svc-ContentSearch|Password="RPFd3v!"
                //Ringtail-Svc-ContentSearch|Version="1"
                var userKeyValue = appConfigs.Find(x => x.Contains(applicationName + "|Username="));
                var pwdKeyValue = appConfigs.Find(x => x.Contains(applicationName + "|Password="));
                var installPath = @"C:\Program Files\FTI Technology\" + applicationName;

                var user = "";
                var pwd = "";

                try
                {
                    Console.WriteLine("...userkeyvalue: " + userKeyValue);
                    Console.WriteLine("...pwdKeyValue: " + pwdKeyValue);
                    user = userKeyValue.Split('=')[1];
                    user = user.Substring(1, user.Length - 2);

                    pwd = pwdKeyValue.Split('=')[1];
                    pwd = pwd.Substring(1, pwd.Length - 2);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("...error reading configurations" + ex.Message);
                    throw ex;
                }

                //TestIISDeploy.exe -u LM\#rpfdev_account -p RPFd3v! -a RingtailSearchSvc -i "C:\Program Files\FTI Technology\Ringtail Search Service"

                string iisInstall = String.Format("DeployToIIS.exe -u {0} -p {1} -a {2} -i {3}", user, pwd, applicationName, installPath);
                return iisInstall;
            }
        }

    }
}
