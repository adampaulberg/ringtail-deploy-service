using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseUpgrader.App
{
    public class BatchWriter
    {
        public static void Write(Options opts)
        {
            string str = string.Empty;
            if (opts.Actions[0].ToLower() == "upgradeportal")
            {
                str = "DataCamel.exe " + opts.Actions[0] + " " + Options.ConvertOptionListToSingle(opts.Databases) + " -u " + opts.Username + " -p " + opts.Password + " -m " + opts.Max;
            }
            else
            {
                str = "DataCamel.exe " + opts.Actions[0] + " -u " + opts.Username + " -p " + opts.Password + " -d " + Options.ConvertOptionListToSingle(opts.Databases) + " -m " + opts.Max;
            }

            var commands = new List<string>();
            commands.Add(str);

            SimpleFileWriter.Write("dbUp.bat", commands);
        }
    }
}
