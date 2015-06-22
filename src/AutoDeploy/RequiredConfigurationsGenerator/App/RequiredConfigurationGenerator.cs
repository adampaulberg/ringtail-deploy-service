using RequiredConfigurationsGenerator.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RequiredConfigurationsGenerator.App
{
    public class RequiredConfigurationGeneratorRunner
    {
        public static List<string> GenerateAllRequiredConfigurations(string filePath)
        {
            if (!filePath.EndsWith(@"\"))
            {
                filePath = filePath + @"\";
            }
            var roles = SimpleFileReader.Read(filePath + "roles.config");
            var userData = SimpleFileReader.Read(filePath + "volitleData.config");
            var installerTemlpate = SimpleFileReader.Read(filePath + "installerTemplate.config");
            var commands = SimpleFileReader.Read(filePath + "commands.config");

            var role = GetRoleFromUserData(userData);

            var apps = RequiredConfigurationHelper.GetRequiredAppsByRole(role, roles);

            var requiredConfigs = RequiredConfigurationHelper.GetAllRequiredConfigs(apps, installerTemlpate, commands);
            return requiredConfigs;
        }

        private static string GetRoleFromUserData(List<string> userData)
        {
            var key = "roleresolver|role";
            var role = userData.Find(x => x.ToLower().StartsWith(key));

            if (role == null)
            {
                throw new ApplicationException("Error: Expected to find a key in volitleData.config of the form  '" + key + "' (not case sensitive)");
            }

            role = role.Split('\"')[1];
            return role;
        }
    }

}
