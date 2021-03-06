﻿using DeployToIIS.Util;
using Microsoft.Web.Administration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeployToIIS.App
{
    class Program
    {

        static int Main(string[] args)
        {
            Console.WriteLine("DeployToIIS starting ...");

            int exitCode = 0;
            var options = new Options();

            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                options.InstallPath = options.InstallPath.Replace("\"", "");

                string volitileData = "volitleData.config";
                var volitileDataList = SimpleFileReader.Read(volitileData);
                ConfigHelper.TryApplyCredentialsToOptions(volitileDataList, options);


                DirectoryInfo di = new DirectoryInfo(options.InstallPath);

                if(!di.Exists)
                {
                    Console.WriteLine("Error: the path provided does not exist - " + options.InstallPath);
                    return 1;
                }

                try
                {
                    ServerManager iisManager = new ServerManager();
                    bool exists = false;
                    foreach (var appPool in iisManager.ApplicationPools)
                    {
                        if (appPool.Name == options.AppName)
                        {
                            SetIdentityToAppPool(options, appPool);
                            appPool.ManagedRuntimeVersion = options.ManagedRuntimeVersion;
                            exists = true;
                            Console.WriteLine("...updating app pool.");
                            iisManager.CommitChanges();
                        }
                    }

                    if (!exists)
                    {
                        Console.WriteLine("...adding app pool.");
                        iisManager.ApplicationPools.Add(options.AppName);


                        foreach (var appPool in iisManager.ApplicationPools)
                        {
                            if (appPool.Name == options.AppName)
                            {
                                SetIdentityToAppPool(options, appPool);
                                appPool.ManagedRuntimeVersion = options.ManagedRuntimeVersion;
                            }
                        }

                        iisManager.CommitChanges();
                    }
                    Console.WriteLine("...app pool added: " + options.AppName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR: app pool creation: ");
                    Console.WriteLine(ex.Message);
                    return 1;
                }

                try
                {

                    ServerManager iisManager = new ServerManager();
                    Site site = null;
                    foreach (var x in iisManager.Sites)
                    {
                        if (x.Name == "Default Web Site")
                        {
                            site = x;
                        }
                    }

                    if (site == null)
                    {
                        Console.WriteLine("...adding site.");
                        iisManager.Sites.Add("Default Web Site", options.InstallPath, 80);
                        iisManager.CommitChanges();
                        Console.WriteLine("...site added: " + options.AppName);
                    }

                    try
                    {
                        
                        var path = "/" + options.AppName;
                        bool exists = false;

                        foreach (var app in site.Applications)
                        {
                            if (app.Path == path)
                            {
                                exists = true;
                            }
                        }

                        if (!exists)
                        {
                            Console.WriteLine("...adding application.");
                            site.Applications.Add(path, options.InstallPath);

                            foreach (var app in site.Applications)
                            {
                                if (app.Path == path)
                                {
                                    app.ApplicationPoolName = options.AppName;
                                }
                            }

                            iisManager.CommitChanges();
                        }
                        Console.WriteLine("...application added: " + options.AppName);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ERROR: application creation: ");
                        Console.WriteLine(ex.Message);
                        return 3;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR: site creation: ");
                    Console.WriteLine(ex.Message);
                    return 2;
                }


                return exitCode;
            }
            else
            {
                Console.WriteLine(options.GetUsage());

            }

            return exitCode;
        }



        private static void SetIdentityToAppPool(Options options, ApplicationPool appPool)
        {
            if (!String.IsNullOrEmpty(options.Username))
            {
                appPool.ProcessModel.UserName = options.Username;
                appPool.ProcessModel.Password = options.Password;
                appPool.ProcessModel.IdentityType = ProcessModelIdentityType.SpecificUser;
            }
            else
            {
                appPool.ProcessModel.IdentityType = ProcessModelIdentityType.ApplicationPoolIdentity;
            }
        }
    }
}
