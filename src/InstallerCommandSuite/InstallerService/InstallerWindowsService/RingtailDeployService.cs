using InstallerService;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace InstallerWindowsService
{
    public partial class RingtailDeployService : ServiceBase
    {
        public Options Options { get; set; }

        public RingtailDeployService(Options options)
        {
            InitializeComponent();
            Options = options;
        }

        protected override void OnStart(string[] args)
        {
            // overrides the binPath options if the user
            // uses "sc start" with arguments
            if(args.Length > 0)
                CommandLine.Parser.Default.ParseArguments(args, Options);

            Runner.StartDaemon(Options);
        }

        protected override void OnStop()
        {
            Runner.StopDaemon();
        }
    }
}
