using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceInstaller.App
{
    public class WebConfigConfigurator
    {
        // look to see if this thing has a webconfig.
        // parse out its keys.
        // replace those keys with values from volitleData.config
        // then move it.

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

        /// <summary>
        /// Combines configurations.   Duplicates among the two sets are resolved such that the 1st parameter, aka, trump wins over config in the secondary one.
        /// </summary>
        /// <param name="trumpConfig"></param>
        /// <param name="secondaryConfig"></param>
        /// <returns></returns>
        public static List<string> CombineConfigurations(List<string> trumpConfig, List<string> secondaryConfig)
        {
            var masterConfig = trumpConfig;

            foreach (var configItem in secondaryConfig)
            {
                var configKey = configItem.Split('=')[0];
                bool isKeyUniqueToSecondary = !DoesConfigContainKey(configKey, trumpConfig);

                if (isKeyUniqueToSecondary)
                {
                    masterConfig.Add(configItem);
                }
            }
            return masterConfig;
        }

        public static bool DoesConfigContainKey(string targetKey, List<string> config)
        {
            string targetKeyToLower = targetKey.ToLower().Trim();

            foreach (var configItem in config)
            {
                var candidateKey = configItem.Split('=')[0];

                if (candidateKey.ToLower().Trim() == targetKeyToLower)
                {
                    return true;
                }
            }

            return false;
        }

        public static List<string> GetConfigItemsByKey(string targetKey, List<string> config, bool ignoreOuterKey)
        {
            string targetKeyToLower = targetKey.ToLower().Trim();
            var foundItems = new List<string>();

            if (ignoreOuterKey)
            {
                var split = targetKey.Split('|');
                if (split.Length > 1)
                {
                    targetKeyToLower = split[1];
                }
                else
                {
                    return foundItems;
                }
            }

            foreach (var configItem in config)
            {
                var candidateKey = configItem.Split('=')[0];

                if (ignoreOuterKey)
                {
                    var split = candidateKey.Split('|');
                    if (split.Length > 1)
                    {
                        candidateKey = split[1];
                    }
                }

                if (candidateKey.ToLower().Trim() == targetKeyToLower.ToLower().Trim())
                {
                    foundItems.Add(configItem);
                }

            }

            return foundItems;
        }

        /// <summary>
        /// Compares configurations.   Returns the set of any shared KEYS, where the values DIFFER.
        /// </summary>
        /// <param name="trumpConfig"></param>
        /// <param name="secondaryConfig"></param>
        /// <returns></returns>
        public static List<string> CompareConfigurations(List<string> aConfig, List<string> bConfig)
        {
            var masterConfig = new List<string>();

            foreach (var configItem in aConfig)
            {
                var configKey = configItem.Split('=')[0];

                var potentialConfigItem = GetConfigItemsByKey(configKey, bConfig, false);

                if (potentialConfigItem.Count == 1)
                {
                    bool completelyEqual = potentialConfigItem[0].ToLower().Trim().Equals(configItem.ToLower().Trim());
                    if (!completelyEqual)
                    {
                        masterConfig.Add("Difference Detected: ");
                        masterConfig.Add("\tA: " + configItem);
                        masterConfig.Add("\tB: " + potentialConfigItem[0]);
                    }
                }
                else
                {
                    var partialKeyMatches = GetConfigItemsByKey(configKey, bConfig, true);

                    foreach (var matchingConfigFound in partialKeyMatches)
                    {
                        string matchingValue = matchingConfigFound.Split('=')[1];
                        string configItemValue = configItem.Split('=')[1];
                        bool completelyEqual = matchingValue.ToLower().Trim().Equals(configItemValue.ToLower().Trim());

                        if (!completelyEqual)
                        {
                            masterConfig.Add("Same key - different app: ");
                            masterConfig.Add("\tA: " + configItem);
                            masterConfig.Add("\tB: " + matchingConfigFound);
                        }
                    }
                }
            }

            return masterConfig;
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
