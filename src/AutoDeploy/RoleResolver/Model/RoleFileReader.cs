using RoleResolverUtility.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoleResolverUtility
{
    public class RoleFileReader
    {
        public static IEnumerable<string> GetRoleFile(string role)
        {
            var masterRoles = SimpleFileReader.Read("roles.config");

            foreach (var x in masterRoles)
            {
                if (x.ToLower().StartsWith(role))
                {
                    yield return x;
                }
            }
        }
    }

    public class ValuesForRole : List<string>
    {
        public static ValuesForRole BuildValuesForRole(string role, IEnumerable<string> config)
        {
            var split = role.Split(',').ToList();

            var valuesForRole = new ValuesForRole();
            foreach (var x in split)
            {
                valuesForRole.AddRange(ValueForSingleRole(x, config));
            }

            var myList = valuesForRole.Distinct().ToList();
            valuesForRole.Clear();
            valuesForRole.AddRange(myList);
            return valuesForRole;
        }

        private static ValuesForRole ValueForSingleRole(string role, IEnumerable<string> config)
        {
            var result = new ValuesForRole();

            foreach (var x in config)
            {
                var parts = x.Split('|', ':');
                if (parts.Length == 2)
                {
                    var configRole = parts[0];
                    if (configRole.ToLower() == role.ToLower())
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
            var result = new ValuesForRole();

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
