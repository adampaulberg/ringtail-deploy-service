using Microsoft.Web.Administration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeployToIIS
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
                try
                {
                    ServerManager iisManager = new ServerManager();
                    bool exists = false;
                    foreach (var appPool in iisManager.ApplicationPools)
                    {
                        if (appPool.Name == options.AppName)
                        {
                            exists = true;
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
                                appPool.ProcessModel.UserName = options.Username;
                                appPool.ProcessModel.Password = options.Password;
                                appPool.ProcessModel.IdentityType = ProcessModelIdentityType.SpecificUser;
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
    }
}
