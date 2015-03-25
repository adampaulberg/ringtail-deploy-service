using InstallerService.Daemon.Controllers;
using InstallerService.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.SelfHost;
using System.Web.Script.Serialization;


namespace InstallerService
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                Runner.StartDaemon(options);
            }   
        }
    }


    public class EnvironmentInfo
    {
        public static string INSTALLER_SERVICE_WORKING_FOLDER = @"C:\Upgrade\InstallerService\";
        public static string CONFIG_LOCATION = @"C:\Upgrade\InstallerService\config.config";

        public static string KeyDeployPath = "DeployPath";
        public static string KeyMasterRunnerUser = "MasterRunnerUser";
        public static string KeyMasterRunnerPass = "MasterRunnerPass";

        public static string GetAutoDeploySuiteFolder()
        {
            var workingDirectory = string.Empty;
            var configs = InstallerServiceConfig();
            if (configs.ContainsKey(KeyDeployPath))
                workingDirectory = configs[KeyDeployPath];

            return workingDirectory;
        }

        public static Dictionary<string, string> InstallerServiceConfig()
        {
            var filepath = EnvironmentInfo.CONFIG_LOCATION;
            var rows = SimpleFileReader.SafeRead(filepath);
            return ConfigParser.Parse(rows);
        }
    }

    public class Runner
    {
        static HttpSelfHostServer ServiceHandle;

        public static void StartDaemon(Options options)
        {
            if (String.IsNullOrEmpty(options.Host))
                GetHost(options);

            if (options.Port == default(uint))
                GetPort(options);
            
            var address = "http://" + options.Host + ":" + options.Port.ToString();
            Log("Binding to " + address);

            var config = new HttpSelfHostConfiguration(address);
            config.Properties["Options"] = options;
            config.Formatters.JsonFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html"));
            config.Routes.MapHttpRoute(
                name: "Default",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            ServiceHandle = new HttpSelfHostServer(config);

            ServiceHandle.OpenAsync().Wait();
            Console.WriteLine("Listening on " + address);
            Console.WriteLine("Press any key to exit...");

            if(options.ConsoleMode)
                Console.ReadKey();
        }

        public static void StopDaemon()
        {
            if (ServiceHandle != null)
            {
                ServiceHandle.CloseAsync();
                ServiceHandle.Dispose();
            }
        }

        private static void GetHost(Options options)
        {
            options.Host = Environment.MachineName;

            if (Environment.MachineName.Contains('.'))
                options.Host = Environment.MachineName.Split('.')[0];
        }

        private static void GetPort(Options options)
        {
            var serviceConfig = EnvironmentInfo.InstallerServiceConfig();
            if (serviceConfig.ContainsKey("PORT"))
                options.Port = uint.Parse(serviceConfig["PORT"]);
            else
                options.Port = 8080;
        }

        private static void Log(string message)
        {
            System.Diagnostics.EventLog.WriteEntry("RingtailDeployService", message);
        }


    }
}
