using InstallerService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace InstallerWindowsService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            // arguments supplied via binPath that can be set with "sc create"
            var options = new Options();
            CommandLine.Parser.Default.ParseArguments(args, options);

            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] 
            { 
                new RingtailDeployService(options)
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
