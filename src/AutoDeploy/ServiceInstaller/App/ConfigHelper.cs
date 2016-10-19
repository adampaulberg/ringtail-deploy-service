using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceInstaller.App
{
    public class ConfigHelper
    {
        /// <summary>
        /// Returns a copy of 'config' with configuration from volitleData applied to the config as appropriate.
        /// </summary>
        /// <param name="appName">the application name (used as a key for volitleData)</param>
        /// <param name="config">the webconfig file contents</param>
        /// <param name="volitleData">the volitledata file contents</param>
        /// <returns></returns>
        public static List<string> ApplyVolitleDataToConfig(string appName, List<string> config, List<string> volitleData)
        {
            //<add key="PortalDBServer" value="localhost" />
            //<add key="PortalDBName" value="Portal" />
            //<add key="PortalDBUser" value="webuser" />
            //<add key="PortalDBPassword" value="password" />
            //<add key="RpfDBServer" value="localhost" />
            //<add key="RpfDBName" value="RPF" />
            //<add key="RpfDBUser" value="webuser" />
            //<add key="RpfDBPassword" value="password" />
            //<add key="RpfDBPort" value="1433" />

            KeyValueConfigDictionary kvCd = new KeyValueConfigDictionary();
            kvCd.Read(volitleData);

            List<string> newDoc = new List<string>();

            int replacementCount = 0;

            foreach (var x in config)
            {
                string newString = TryReplaceKeyFromFile(appName, x, kvCd);

                if (newString != x)
                {
                    replacementCount++;
                }
                newDoc.Add(newString);
            }

            Console.WriteLine("...applied " + replacementCount + " configurations.");

            return newDoc;
        }

        public static string TryReplaceKeyFromFile(string appName, string config, KeyValueConfigDictionary volitleData)
        {
            string newConfig = config;

            if (config.Contains(@"<add key="))
            {
                appName = appName.ToUpper();
                var valueIndex = config.IndexOf("value=");
                var keyPart = config.Substring(0, valueIndex);
                var substring = config.Substring(valueIndex);

                var key = keyPart.Split('\"')[1];

                var tryValue = string.Empty;
                if (volitleData.TryGetValueForKey(appName, key, out tryValue))
                {
                    var value = tryValue;
                    var valuePart = "value=" + value + " />";

                    newConfig = keyPart + valuePart;
                }
            }


            return newConfig;
        }
    }

    public class KeyValueConfigDictionary
    {
        private Dictionary<string, Dictionary<string, string>> lookupKeys = new Dictionary<string, Dictionary<string, string>>();
        public List<string> ErrorMessages = new List<string>();

        public KeyValueConfigDictionary()
        {
        }

        public void Read(List<string> config)
        {
            lookupKeys = BuildConfigDictionary(config, out ErrorMessages);
        }

        public Dictionary<string, string> GetCommonKeys()
        {
            return lookupKeys["COMMON"];
        }

        public bool TryGetValueForKey(string appName, string parameterName, out string parameterValue)
        {
            bool found = false;
            parameterValue = parameterName;
            var commonKeys = GetCommonKeys();

            if (lookupKeys.ContainsKey(appName) && lookupKeys[appName].ContainsKey(parameterName))
            {
                found = true;
                parameterValue = lookupKeys[appName][parameterName];
            }
            else if (commonKeys.ContainsKey(parameterName))
            {
                found = true;
                parameterValue = commonKeys[parameterName];
            }
            else
            {
                parameterValue = "\"" + parameterName + "\"";
            }

            return found;
        }

        private static Dictionary<string, Dictionary<string, string>> BuildConfigDictionary(List<string> config, out List<string> errorMessages)
        {
            var lookupKeys = new Dictionary<string, Dictionary<string, string>>();

            errorMessages = new List<string>();
            foreach (var x in config)
            {
                if (x.Length > 0)
                {
                    if (!x.StartsWith("--"))
                    {
                        string[] split = x.Split('|');
                        if (split.Length != 2)
                            throw new ApplicationException(string.Format("Config '{0}' is not in the format App|ConfigKey=\"Value\"", x));

                        string applicationKey = split[0].ToUpper();
                        string[] variableKeyValue = split[1].Split('=');
                        if (variableKeyValue.Length != 2)
                            throw new ApplicationException(string.Format("Config '{0}' is not in the format App|ConfigKey=\"Value\"", x));

                        if (!lookupKeys.ContainsKey(applicationKey))
                        {
                            lookupKeys.Add(applicationKey, new Dictionary<string, string>());
                        }

                        try
                        {
                            lookupKeys[applicationKey].Add(variableKeyValue[0], variableKeyValue[1]);
                        }
                        catch (Exception ex)
                        {
                            errorMessages.Add("Duplicate Key Found: " + applicationKey + "|" + variableKeyValue[0]);
                            throw ex;
                        }
                    }
                }
            }
            return lookupKeys;
        }
    }
}
