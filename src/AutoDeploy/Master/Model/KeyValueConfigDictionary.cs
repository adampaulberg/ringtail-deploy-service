using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Master.Model
{

    public class KeyValueConfigDictionary
    {
        private Dictionary<string, Dictionary<string, string>> lookupKeys = new Dictionary<string, Dictionary<string, string>>();


        public KeyValueConfigDictionary(List<string> config)
        {
            lookupKeys = BuildConfigDictionary(config);
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


        private static Dictionary<string, Dictionary<string, string>> BuildConfigDictionary(List<string> config)
        {
            var lookupKeys = new Dictionary<string, Dictionary<string, string>>();

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

                        lookupKeys[applicationKey].Add(variableKeyValue[0], variableKeyValue[1]);
                    }
                }
            }
            return lookupKeys;
        }
    }
}
