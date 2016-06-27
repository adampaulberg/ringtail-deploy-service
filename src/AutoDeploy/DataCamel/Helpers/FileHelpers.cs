using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCamel.Helpers
{
    public class SimpleFileReader
    {
        public static List<string> Read(string fileName)
        {
            List<string> s = new List<string>();

            FileInfo fi = new FileInfo(fileName);
            if (fi.Exists)
            {

                using (StreamReader stream = new StreamReader(fileName))
                {
                    string input = null;
                    while ((input = stream.ReadLine()) != null)
                    {
                        s.Add(input);
                    }
                }
            }

            return s;
        }
    }


    public class SimpleFileWriter
    {
        public static void Write(string fileName, List<string> s)
        {
            using (StreamWriter wr = new StreamWriter(fileName))
            {
                foreach (string str in s)
                {
                    wr.WriteLine(str);
                }
            }
        }
    }


    public class ConfigHelper
    {
        public static List<string> GetLaunchKeys()
        {
            var userData = SimpleFileReader.Read(@"C:\upgrade\AutoDeploy\volitleData.config");
            return userData.FindAll(x => x.StartsWith("LAUNCHKEY"));
        }

        public static void WriteLaunchKeysAsJson(string writeLocation)
        {
            var launchKeysAsConfig = Helpers.ConfigHelper.GetLaunchKeys();
            var launchKeysAsJson = Helpers.ConfigHelper.ConvertToKeysfileJson(launchKeysAsConfig);
            var asList = new List<string>();
            asList.Add(launchKeysAsJson);
            Helpers.SimpleFileWriter.Write(writeLocation, asList);
        }

        public static string ConvertToKeysfileJson(List<string> configs)
        {
            string front = "[";
            string back = "]";

            string body = "";

            // FROM:        
            //      LAUNCHKEY|MyKey="someFeature"
            //      LAUNCHKEY|MyKey2="someFeature2"
            // TO:
            //      [{"Description": "someFeature", "FeatureKey":"MyKey", "MinorKey":"nokey"},{"Description": "someFeature2", "FeatureKey":"MyKey2", "MinorKey":"nokey2"}]
            //

            foreach (var config in configs)
            {
                string configBase = config.Substring(10, config.Length - 10);
                string key = configBase.Split('=')[0];
                string descr = configBase.Split('=')[1];
                descr = descr.Substring(1, descr.Length - 2);


                string item = "{\"Description\":\"{0}\", \"FeatureKey\":\"{1}\"}";
                item = item.Replace("{0}", descr);
                item = item.Replace("{1}", key);

                body += item + ",";
            }

            body = body.Length > 0 ? body.Substring(0, body.Length - 1) : string.Empty;

            return front + body + back;
        }

    }
}
