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
            var role = userData.Find(x => x.ToLower().StartsWith("roleresolver|role"));
            role = role.Split('\"')[1];
            return role;
        }
    }

}
