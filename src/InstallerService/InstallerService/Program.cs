using InstallerService.Daemon.Controllers;
using InstallerService.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.SelfHost;
using System.Web.Http.SelfHost.Channels;
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
        static EnvironmentInfo()
        {
            // use AppDomain.CurrentDomain.BaseDirectory instead of Environment.CurrentDirectory
            // since this can be running from a Windows Service which will change the current directory
            // to c:\windows\system32.  
            INSTALLER_SERVICE_WORKING_FOLDER = AppDomain.CurrentDomain.BaseDirectory;
            CONFIG_LOCATION = Path.Combine(INSTALLER_SERVICE_WORKING_FOLDER, "config.config");            
        }

        public static string INSTALLER_SERVICE_WORKING_FOLDER;
        public static string CONFIG_LOCATION;

        public static string KeyDeployPath = "DeployPath";
        public static string KeyMasterRunnerUser = "MasterRunnerUser";
        public static string KeyMasterRunnerPass = "MasterRunnerPass";
        public static string KeyAuthMode = "AuthMode";

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
            var envConfig = EnvironmentInfo.InstallerServiceConfig();

            if(envConfig.ContainsKey(EnvironmentInfo.KeyAuthMode) && envConfig[EnvironmentInfo.KeyAuthMode] == "Basic")
                config.ClientCredentialType = HttpClientCredentialType.Basic;

            config.Properties["Options"] = options;

            var logger = new ApiLogger(EnvironmentInfo.INSTALLER_SERVICE_WORKING_FOLDER + "apilog.txt");
            logger.Log("Starting API");
            config.Properties["ApiLogger"] = logger;

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
