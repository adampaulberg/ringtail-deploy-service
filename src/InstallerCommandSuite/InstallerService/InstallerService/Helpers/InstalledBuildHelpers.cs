//using InstallerService.Daemon.Controllers;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace InstallerService.Helpers
//{
//    public class InstalledBuildHelpers
//    {

//        public static IEnumerable<string> ReadBuildFile(string filePath)
//        {
//            var current = SimpleFileReader.Read(filePath);
//            var matching = current.FindAll(x => x.Contains("truncating:"));


//            foreach (var x in matching)
//            {
//                var split = x.Split(':');

//                yield return split[1].Substring(0, split[1].Length - 2);
//            }
//        }
//    }
//}
