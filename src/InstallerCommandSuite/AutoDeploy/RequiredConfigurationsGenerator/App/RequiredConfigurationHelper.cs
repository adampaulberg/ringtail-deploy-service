using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RequiredConfigurationsGenerator.App
{
    public class RequiredConfigurationHelper
    {
        public static List<string> GetAllRequiredConfigs(List<string> apps, List<string> template, List<string> commands)
        {
            var requiredConfigs = new List<string>();

            apps.ForEach(x => requiredConfigs.AddRange(GetReqruiedConfigsByAppForCommands(x, commands)));
            apps.ForEach(x => requiredConfigs.AddRange(GetReqruiedConfigsByAppForTemplates(x, template)));

            return requiredConfigs.Distinct().ToList();

        }

        /// <summary>
        /// Returns the list of apps [right side of the roles file] for a given role.
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="roles"></param>
        /// <returns></returns>
        public static List<string> GetRequiredAppsByRole(string roleName, List<string> roles)
        {
            List<string> matchingRoles = new List<string>();
            return AppsForRole.BuildValuesForRole(roleName, roles);
        }

        /// <summary>
        /// Returns a de-duplicated set of required parameters given an installer template input, and an application name.
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="templates"></param>
        /// <returns></returns>
        public static List<string> GetReqruiedConfigsByAppForCommands(string appName, List<string> commands)
        {
            var filtered = FilterCommandsConfigbyApp(appName, commands);

            var master = new List<string>();

            filtered.ForEach(x => master.AddRange(GetRequiredConfigsFromARowInTheCommandsConfigFile(x)));

            for (int i = 0; i < master.Count; i++)
            {
                master[i] = appName + "|" + master[i];
            }

            return master.Distinct().ToList();
        }

        /// <summary>
        /// Returns a de-duplicated set of required parameters given an installer template input, and an application name.
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="templates"></param>
        /// <returns></returns>
        public static List<string> GetReqruiedConfigsByAppForTemplates(string appName, List<string> templates)
        {
            var filtered = FilterInstallerCommandsbyApp(appName, templates);

            var master = new List<string>();

            filtered.ForEach(x => master.AddRange(GetRequiredConfigsFromARowInTheInstallerTemplatesConfigFile(x)));

            for (int i = 0; i < master.Count; i++)
            {
                master[i] = appName + "|" + master[i];
            }

            return master.Distinct().ToList();
        }

        /// <summary>
        /// Simply filters down the templates by app.
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="templates"></param>
        /// <returns></returns>
        public static List<string> FilterCommandsConfigbyApp(string appName, List<string> commands)
        {
            return commands.FindAll(x => x.Contains(appName));
        }

        /// <summary>
        /// Simply filters down the templates by app.
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="templates"></param>
        /// <returns></returns>
        public static List<string> FilterInstallerCommandsbyApp(string appName, List<string> templates)
        {
            return templates.FindAll(x => x.StartsWith(appName));
        }

        /// <summary>
        /// Parses a command.config row and returns all of the configs needed to run it.
        /// </summary>
        /// <param name="commandConfig"></param>
        /// <returns></returns>
        public static List<string> GetRequiredConfigsFromARowInTheCommandsConfigFile(string commandConfig)
        {
            var returnValues = new List<string>();

            var split = commandConfig.Split('|');
            var tokens = split.ToList();
            tokens.RemoveAt(0);

            foreach (var token in tokens)
            {
                if (token.StartsWith("-"))
                {
                    var valueOfFlag = token.Split(' ')[1];

                    // BY CONVENTION - only UPPERCASE things should be volitle data in this file.
                    bool hasAnyLowerCaseChars = valueOfFlag.ToList().Any(x => Char.IsLower(x));
                    if (!hasAnyLowerCaseChars)
                    {
                        returnValues.Add(valueOfFlag);
                    }
                }
                else
                {
                    returnValues.Add(token);
                }

            }

            return returnValues;
        }

        /// <summary>
        /// Parses an installer template row and returns all of the configs needed to run that installation.
        /// </summary>
        /// <param name="installerTemplate"></param>
        /// <returns></returns>
        public static List<string> GetRequiredConfigsFromARowInTheInstallerTemplatesConfigFile(string installerTemplate)
        {
            var returnValues = new List<string>();

            var split = installerTemplate.Split('|');
            var tokens = split[2].Split(new string[] { "/v\"" }, StringSplitOptions.None).ToList();

            foreach (var x in tokens)
            {
                var key = x.Split('=');

                if (key.Length > 1)
                {
                    if (key[0] == "ADDLOCAL")
                    {
                        continue;
                    }

                    returnValues.Add(key[0]);
                }

            }


            return returnValues;
        }
    }

    public class AppsForRole : List<string>
    {
        public static AppsForRole BuildValuesForRole(string role, IEnumerable<string> config)
        {
            var split = role.Split(',').ToList();

            var valuesForRole = new AppsForRole();
            foreach (var x in split)
            {
                valuesForRole.AddRange(ValueForSingleRole(x, config));
            }

            var myList = valuesForRole.Distinct().ToList();
            valuesForRole.Clear();
            valuesForRole.AddRange(myList);
            return valuesForRole;
        }

        private static AppsForRole ValueForSingleRole(string role, IEnumerable<string> config)
        {
            var result = new AppsForRole();

            foreach (var x in config)
            {
                if (x.ToLower().StartsWith(role.ToLower()))
                {
                    var value = TryGetValue(x, '|');
                    if (value != null)
                    {
                        result.Add(value);
                    }
                    else
                    {
                        result.AddRange(TryFillinSuperKey(x, config));
                    }

                }
            }

            return result;
        }

        private static string TryGetValue(string config, char delimiter)
        {
            if (config.Contains(delimiter))
            {
                var split = config.Split(delimiter);

                var key = split[0];
                var value = split[1];

                return value;
            }

            return null;
        }

        private static List<string> TryFillinSuperKey(string config, IEnumerable<string> mastserRoleList)
        {
            if (config.Contains(':'))
            {
                var subKey = TryGetValue(config, ':');
                return GetLowLevelKeysByParentKey(subKey, mastserRoleList);
            }

            return new List<string>();
        }

        private static List<string> GetLowLevelKeysByParentKey(string parentKey, IEnumerable<string> config)
        {
            var result = new AppsForRole();

            foreach (var x in config)
            {
                if (x.StartsWith(parentKey))
                {
                    result.Add(TryGetValue(x, '|'));
                }
            }

            return result;
        }
    }
}
