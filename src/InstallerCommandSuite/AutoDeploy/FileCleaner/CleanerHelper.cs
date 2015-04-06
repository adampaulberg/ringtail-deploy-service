using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileCleaner
{
    public class CleanerHelper
    {
        public Options Options { get; set; }
        public StringBuilder Log { get; set; }
        public StringBuilder Output { get; set; }

        public CleanerHelper(Options options)
        {
            this.Options = options;
            this.Log = new StringBuilder();
            this.Output = new StringBuilder();
        }

        public int Process()
        {
            var exitCode = 0 | ProcessPaths() | ProcessSubs();

            if (this.Output.Length > 0)
            {
                this.Output.AppendLine("IF ERRORLEVEL 1 SET ERRORLEV=0");
                this.Output.AppendLine("IF ERRORLEVEL 2 SET ERRORLEV=0");
                this.Output.AppendLine("IF ERRORLEVEL 3 SET ERRORLEV=0");
                this.Output.AppendLine("IF ERRORLEVEL 4 SET ERRORLEV=0");
                this.Output.AppendLine("IF ERRORLEVEL 5 SET ERRORLEV=0");
            }
            else
            {
                this.Output.AppendLine("@echo NOTHING TO CLEAN");
            }

            return exitCode;
        }

        int ProcessPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return 0;            
            
            var exitCode = 0;
            path = path.Trim();
            this.Log.AppendLine(string.Format("Processing '{0}'", path));
            try
            {
                var attr = File.GetAttributes(path);
                if (attr.HasFlag(FileAttributes.Directory))
                    this.Output.AppendLine(string.Format("rd /S /Q \"{0}\"", path));
                else
                    this.Output.AppendLine(string.Format("del /F /Q \"{0}\"", path));
                exitCode = 0;
            }
            catch (Exception ex)
            {
                this.Log.AppendLine("ERROR");
                this.Log.AppendLine("  -> " + ex.Message);
                exitCode = 2;
            }            

            return exitCode;
        }

        int ProcessPaths()
        {
            if (this.Options.Paths == null)
                return 0;            

            var exitCode = 0;
            foreach (var rawPath in this.Options.Paths)
            {
                if (rawPath == "FILE_DELETIONS")
                    return exitCode;

                exitCode = exitCode | ProcessPath(rawPath);
            }
            return exitCode;
        }

        int ProcessSubs()
        {
            if (this.Options.Subs == null)
                return 0;

            var exitCode = 0;
            foreach (var rawRoot in this.Options.Subs)
            {
                if (rawRoot == "FILE_DELETIONS")
                    return exitCode;

                if (string.IsNullOrWhiteSpace(rawRoot))
                    continue;

                var root = rawRoot.Trim();
                this.Log.AppendFormat("Finding subdirectories for '{0}'", root);
                try
                {
                    // find the directories
                    var paths = Directory.GetDirectories(root);

                    // output a deletion line for each directory
                    foreach (var path in paths)                                            
                        ProcessPath(path);                    
                }
                catch (Exception ex)
                {
                    this.Log.AppendLine("ERROR");
                    this.Log.AppendLine("  -> " + ex.Message);
                    exitCode = exitCode | 2;
                }
            }
            return exitCode;
        }

        public string WriteLog()
        {
            using (StreamWriter sw = new StreamWriter(this.Options.LogFile))
            {
                var text = this.Log.ToString();
                sw.Write(text);
                return text;
            }
        }

        public string WriteOutput()
        {
            using (StreamWriter sw = new StreamWriter(this.Options.OutFile))
            {
                var text = this.Output.ToString();
                sw.Write(text);
                return text;
            }
        }
    }
}
