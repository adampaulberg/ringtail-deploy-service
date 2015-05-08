using RequiredConfigurationsGenerator.App;
using RequiredConfigurationsGenerator.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RequiredConfigurationsGenerator
{
    class Program
    {
        public static int Main(string[] args)
        {
            int exitCode = 0;

            Logger l = new Logger();
            l.fileName = "RequiredConfigurationsGenerator.log";

            l.AddToLog("RequiredConfigurationsGenerator ... starting");
            l.AddToLog(" About:");
            l.AddToLog("  this tool scans for the configurations you'll need based on ");
            l.AddAndWrite("  the role that you've set in volitleData.config");
            try
            {
                var configs = RequiredConfigurationGeneratorRunner.GenerateAllRequiredConfigurations(Environment.CurrentDirectory);
                configs.ForEach(x => l.AddToLog(configs));
                l.AddAndWrite("...configs generated.");

                SimpleFileWriter.Write("requiredConfigs.config", configs);

                if (!new FileInfo("requiredConfigs.config").Exists)
                {
                    l.AddAndWrite("...unable to write configs.");
                    exitCode = 1;
                }
                else
                {
                    l.AddAndWrite("...configs written to requiredConfigs.config");
                }
            }
            catch (Exception ex)
            {
                l.AddToLog(ex.Message);
                l.AddToLog(ex.StackTrace);
                l.AddAndWrite("RequiredConfigurationsGenerator FAILED");
                exitCode = 1;
            }

            return exitCode;
        }
    }
}
