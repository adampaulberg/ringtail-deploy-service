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
            
            if(args.Length == 0)
            {
                GetUsage();
                return 2;
            }

            Console.WriteLine("ServiceInstaller starting for " + args[0]);

            string appName = args[0];

            try
            {
                exitCode = ServiceInstallerHelper.RunIt(appName);
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
            Console.WriteLine("");
            Console.WriteLine("... this will create a batch file that calls the DeployToIIS.exe to unpack a zip file and install an IIS website.");
            Console.WriteLine("... It uses a convention based approach - only param is the service name.");
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
                        var STR_OMIT = "omit-";
                        if (fileName.StartsWith(STR_OMIT))
                        {
                            var omission = fileName.Substring(STR_OMIT.Length, fileName.Length - STR_OMIT.Length);
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
            public static int RunIt(string appName)
            {
                List<string> exclusions = DynamicExclusionDetector.DetectExclusions(appName);

                if (exclusions.Contains(appName))
                {
                    Console.WriteLine("ServiceInstaller found exclusion for: " + appName);
                    var noOpFile = new List<string>();
                    noOpFile.Add("@echo SKIPPING");
                    SimpleFileWriter.Write("deploy-" + appName + ".bat", noOpFile);
                    return 0;
                }


                Console.WriteLine("ServiceInstaller Reading.... " + appName);

                string volitileData = "volitleData.config";
                var volitileDataList = SimpleFileReader.Read(volitileData);
                var filledInParameters = new List<string>();

                NormalizeZipName(appName);
                var unzipResult = ExtractZip(appName);

                if (unzipResult == false)
                {
                    return 2;
                }

                var cleanResult = CleanupTarget(appName, GetInstallPath(appName));

                if (cleanResult != 0)
                {
                    return cleanResult;
                }

                var moveResult = MoveZipToProgramFiles(appName);

                if (moveResult != 0)
                {
                    return moveResult;
                }


                var iisInstallCmd = String.Empty;


                try
                {
                    iisInstallCmd = GenerateIISInstallCommand(volitileDataList, appName);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    return 1;
                }
                filledInParameters.Add(iisInstallCmd);


                if(String.IsNullOrEmpty(iisInstallCmd))
                {
                    Console.WriteLine("Unknown error - the deploy to iis command is blank, so this won't create an IIS website correctly.");
                    return 1;
                }

                Console.WriteLine("ServiceInstaller Writing to file.... " + "deploy-" + appName + ".bat");
                SimpleFileWriter.Write("deploy-" + appName + ".bat", filledInParameters);

                return 0;
            }

            public static bool ExtractZip(string applicationName)
            {
                try
                {
                    Console.WriteLine("Unzipping");
                    string zipPath = @"C:\Upgrade\AutoDeploy\" + applicationName + @"\" + applicationName + ".zip";
                    string extractPath = GetExtractPath(applicationName);

                    DirectoryInfo unzipFolder = new DirectoryInfo(extractPath);

                    if(unzipFolder.Exists)
                    {
                        CleanupTarget(applicationName, extractPath);
                    }

                    System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractPath);


                    unzipFolder = new DirectoryInfo(extractPath);

                    if (!unzipFolder.Exists)
                    {
                        Console.WriteLine("Failed to unzip - " + zipPath);
                    }

                    return unzipFolder.Exists;
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    return false;
                }
            }

            public static string GetExtractPath(string applicationName)
            {
                return @"C:\Upgrade\AutoDeploy\" + applicationName + @"\" + applicationName;
            }

            public static string GetInstallPath(string applicationName)
            {
                return @"C:\Program Files\FTI Technology\" + applicationName;
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

            public static int CleanupTarget(string applicationName, string path)
            {
                Console.WriteLine("...starting CleanupInstallTarget");
                try
                {
                    DirectoryInfo di = new DirectoryInfo(path);

                    if (di.Exists)
                    {
                        di.Delete(true);
                    }
                    else
                    {
                        return 0;
                    }

                    di = new DirectoryInfo(path);

                    if(di.Exists)
                    {
                        Console.WriteLine("...warning - failed to fully clean up existing application");
                        return 0;
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine("...ERROR trying to cleanup existing application - " + ex.Message);
                    return 1;
                }

                return 0;

            }

            public static int MoveZipToProgramFiles(string applicationName)
            {
                Console.WriteLine("...starting MoveZipToProgramFiles");
                string installPath = GetInstallPath(applicationName);
                string extractPath = GetExtractPath(applicationName);
                try
                {
                    DirectoryInfo di = new DirectoryInfo(@"C:\Program Files\FTI Technology\");
                    if (!di.Exists)
                    {
                        di.Create();
                        Console.WriteLine("...created folder: " + di.FullName);
                    }

                    for (int i = 0; i < 5; i++)
                    {
                        try
                        {
                            System.IO.Directory.Move(extractPath, installPath);
                            break;
                        }
                        catch
                        {
                            System.Threading.Thread.Sleep(5000);  // wait 5 seconds.  the unzip may still be holding onto resources.
                        }
                    }

                }
                catch(Exception ex)
                {
                    Console.WriteLine("...failed to copy zip to " + installPath);
                    Console.WriteLine("..error:" + ex.Message);
                    return 1;
                }

                return 0;
            }

            public static string GenerateIISInstallCommand(List<string> volitileData, string applicationName)
            {
                Console.WriteLine("...starting to generate the IIS installation command");

                var appConfigs = volitileData.FindAll(x => x.StartsWith(applicationName));


                //Ringtail-Svc-ContentSearch|Username="DOMAIN\user"
                //Ringtail-Svc-ContentSearch|Password="PASSWORD"
                //Ringtail-Svc-ContentSearch|Version="1"
                var userKeyValue = appConfigs.Find(x => x.Contains(applicationName + "|SERVICEUSERNAME="));
                var pwdKeyValue = appConfigs.Find(x => x.Contains(applicationName + "|SERVICEPASSWORD="));
                var installPath = GetInstallPath(applicationName);

                var user = "";
                var pwd = "";


                bool valid = true;
                if (String.IsNullOrEmpty(userKeyValue))
                {
                    Console.WriteLine(" Missing a required entry in volitleData.config for the key: " + " Ringtail-Svc-ContentSearch|SERVICEUSERNAME");
                    valid = false;
                }

                if (String.IsNullOrEmpty(pwdKeyValue))
                {
                    Console.WriteLine(" Missing a required entry in volitleData.config for the key: " + " Ringtail-Svc-ContentSearch|SERVICEPASSWORD");
                    valid = false;
                }

                if (!valid)
                {
                    Console.WriteLine("One of the required configurations was missing.");
                    throw new ArgumentNullException();
                }

                try
                {
                    //Console.WriteLine("...userkeyvalue: " + userKeyValue);
                    //Console.WriteLine("...pwdKeyValue: " + pwdKeyValue);
                    user = userKeyValue.Split('=')[1];
                    user = user.Substring(1, user.Length - 2);

                    pwd = pwdKeyValue.Split('=')[1];
                    pwd = pwd.Substring(1, pwd.Length - 2);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("...error reading configurations: " + ex.Message);
                    throw ex;
                }

                //DeployToIIS.exe -u DOMAIN\user -p PASSWORD! -a RingtailSearchSvc -i "C:\Program Files\FTI Technology\Ringtail Search Service"

                string iisInstall = String.Format("DeployToIIS.exe -u {0} -p {1} -a {2} -i \"{3}\"", user, pwd, applicationName, installPath);
                return iisInstall;
            }
        }

    }
}
