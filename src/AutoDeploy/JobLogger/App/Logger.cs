using JobLogger.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobLogger.App
{
    public class Logger
    {
        public static bool WriteLog(Options o)
        {
            try
            {
                if(String.IsNullOrEmpty(o.FolderRoot))
                {
                    Console.WriteLine("   No folder given, looking in volitleData for JobLogger|DROP_FOLDER_ROOT");
                    try
                    {
                        var dropFolder = "drop";
                        var data = SimpleFileReader.Read("volitleData.config");

                        dropFolder = data.Find(x => x.StartsWith("JobLogger|DROP_FOLDER_ROOT"));

                        if (!String.IsNullOrEmpty(dropFolder))
                        {
                            Console.WriteLine(" found: " + dropFolder);
                            var split = dropFolder.Split('=');
                            Console.WriteLine(" found: " + split[0]);
                            Console.WriteLine(" found: " + split[1]);
                            dropFolder = split[1].Replace("\"", "");
                            o.FolderRoot = dropFolder;
                        }

                        Console.WriteLine("   Drop folder found: " + o.FolderRoot);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("   no drop folder specified, and something went wrong trying to get it: " + ex.Message);
                    }

                }

                if (!String.IsNullOrEmpty(o.FolderRoot))
                {
                    Console.WriteLine("   Logging to: " + o.FolderRoot);





                    if (String.IsNullOrEmpty(o.Tag))
                    {
                        Console.WriteLine("   making tag");
                        var machineInfo = System.Environment.MachineName;;
                        Console.WriteLine("   found role: " + machineInfo);
                        o.Tag = MakeTag(machineInfo);

                    }
                    else
                    {
                        Console.WriteLine("   using tag: " + o.Tag);
                    }

                    o.Tag += "-" + o.Version;

                    if(!o.FolderRoot.EndsWith(@"\"))
                    {
                        o.FolderRoot = o.FolderRoot + @"\";
                    }

                    var start = string.Empty;

                    int messageMax = 10;

                    if (o.Message.Length > messageMax)
                    {
                        start = o.Message.Substring(0, messageMax);
                    }
                    else
                    {
                        start = o.Message;

                        for (int i = 0; i < messageMax - o.Message.Length; i++)
                        {
                            start += "_";
                        }
                    }

                    var stamp = DateTime.Now;
                    var fileNamePart = stamp.Month + "-" + stamp.Day + "-" + stamp.Hour + stamp.Minute + "-" + stamp.Second + "-" + stamp.Millisecond;
                    var logSuffix = start + "-" + o.Tag + "-" + fileNamePart + ".log";
                    var log = o.FolderRoot + logSuffix;
                    var contents = new List<string>();
                    contents.Add(o.Message + "," + o.Tag + "," + stamp.ToUniversalTime());
                    Console.WriteLine("   About to write log: " + log);
                    SimpleFileWriter.Write(log, contents);

                    try
                    {
                        var masterLog = o.FolderRoot + "masterLog.txt";
                        Console.WriteLine("   About to write to master log: " + masterLog);
                        var masterContents = SimpleFileReader.Read(masterLog);
                        masterContents.Add(contents[0]);
                        SimpleFileWriter.Write(masterLog, masterContents);

                        if (masterContents.Count > 10000)
                        {
                            Console.WriteLine("   About to archive old master log: " + masterLog);
                            FileInfo fi = new FileInfo(masterLog);
                            fi.CopyTo(o.FolderRoot + "masterLog_archive" + stamp.Ticks + ".log");
                            fi.Delete();
                        }

                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine("   could not write to master log - attempting to log the failure: " + ex.Message);
                        var logFail = o.FolderRoot + "MasterLogFail-" + logSuffix;
                        SimpleFileWriter.Write(logFail, new List<string>());

                    }

                }
                return true;
            }
            catch(Exception ex)
            {
                Console.WriteLine("   eating exception: " + ex.Message);
                return true;
            }

        }

        private static string MakeTag(string tag)
        {
            Console.WriteLine("   no tag specified - reading from disk");
            var fi = new FileInfo("tag.txt");
            string newTag = string.Empty;
            if (!fi.Exists)
            {
                var guid = Guid.NewGuid().ToString();
                guid = guid.Replace("-", "");
                newTag = tag + "-" + guid.Substring(0, 16);
                Console.WriteLine("   no tag found - logger making tag: " + newTag);
                var contents = new List<string>();
                contents.Add(newTag);
                SimpleFileWriter.Write("tag.txt", contents);
            }
            else
            {
                newTag = SimpleFileReader.Read("tag.txt")[0];
                Console.WriteLine("   tag found: " + newTag);
            }
            

            return newTag;
        }
    }
}
