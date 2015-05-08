using RoleResolverUtility.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoleResolverUtility
{
    public class Program
    {
        static int Main(string[] args)
        {
            int exitCode = 0;
            Logger log = new Logger();
            var options = new Options();
            log.AddToLog(" RoleResolver............. " + DateTime.Now);

            Console.WriteLine("Role resolver starting...");

            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                try
                {
                    RoleResolver rr = new RoleResolver(options, log);

                    // make sure that the prerequisite files are there.
                    FileInfo fi = new FileInfo("masterCommands.config");
                    if (!fi.Exists)
                    {
                        log.AddToLog("Couldn't find: " + fi.FullName);
                        exitCode = 1;
                    }
                    fi = new FileInfo("roles.config");
                    if (!fi.Exists)
                    {
                        log.AddToLog("Couldn't find: " + fi.FullName);
                        exitCode = 1;
                    }

                    // kick off the construction of master.config
                    var output = rr.FilterMasterCommandsByRole(SimpleFileReader.Read(@"masterCommands.config"), SimpleFileReader.Read(@"roles.config"));

                    if (output.Count > 0)
                    {
                        SimpleFileWriter.Write(@"master.config", output);
                    }
                    else
                    {
                        // don't write master.config if it's going to be empty - instead return an error.
                        exitCode = 1;
                        log.AddToLog("mastser.config would be empty....");
                        log.Write("RoleResolver.txt");
                    }

                    if (exitCode == 0)
                    {
                        log.AddToLog("Success");
                    }
                    else
                    {
                        log.AddToLog("ERROR");
                    }
                }
                catch (Exception ex)
                {
                    log.AddToLog(ex.Message);
                    log.AddToLog(ex.StackTrace);
                }
            }

            log.Write("RoleResolver.txt");

            return exitCode;
        }
    }
}
