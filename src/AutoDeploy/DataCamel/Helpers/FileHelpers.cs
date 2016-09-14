using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;

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
        public static List<string> GetLaunchKeys(string config=@"C:\upgrade\AutoDeploy\volitleData.config")
        {
            var userData = SimpleFileReader.Read(config);
            return userData.FindAll(x => x.StartsWith("LAUNCHKEY"));
        }

        public static void WriteLaunchKeysAsJson(string writeLocation, string configLocation=null)
        {
            var launchKeysAsConfig = string.IsNullOrEmpty(configLocation) == true ? GetLaunchKeys() : GetLaunchKeys(configLocation);
            var launchKeysAsJson = Helpers.ConfigHelper.ConvertToKeysfileJson(launchKeysAsConfig);
            var asList = new List<string>();
            asList.Add(launchKeysAsJson);
            Helpers.SimpleFileWriter.Write(writeLocation, asList);
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
            // FROM:        
            //      LAUNCHKEY|MyKey="someFeature"
            //      LAUNCHKEY|MyKey2="someFeature2"
            // TO:
            //      [{"Description": "someFeature", "FeatureKey":"MyKey", "MinorKey":"nokey"},{"Description": "someFeature2", "FeatureKey":"MyKey2", "MinorKey":"nokey2"}]
            //
            List<object> keysList = new List<object>();
            
            
            foreach (var config in configs)
            {
                string configBase = config.Substring(10, config.Length - 10);
                string key = configBase.Split('=')[0];
                string descr = configBase.Split('=')[1];
                descr = descr.Substring(1, descr.Length - 2);

                var obj = new
                {
                    Description = descr,
                    FeatureKey = key
                };
                keysList.Add(obj);
            }

            if(keysList.Any())
            { 
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                return serializer.Serialize(keysList);
            }

            return string.Empty;
        }

    }
}
