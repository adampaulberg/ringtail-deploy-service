using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UninstallerHelper.App
{
    internal class DynamicExclusionDetector
    {
        public static List<string> DetectExclusions()
        {
            var list = new List<string>();

            var di = new DirectoryInfo(Environment.CurrentDirectory);

            var files = di.GetFiles();

            foreach (var f in files.ToList())
            {
                try
                {
                    var fileName = f.Name;
                    if (fileName.StartsWith("omit-"))
                    {
                        var omission = fileName.Split('-')[1];
                        omission = omission.Split('.')[0];

                        list.Add(omission);
                        Console.WriteLine("found omission: " + omission);
                    }
                }
                catch
                {
                    Console.WriteLine("Minor problem reading omission files.  Continuing.");
                }
            }

            return list;
        }
    }
}
