using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UninstallerHelper.App
{
    public class UninstallCommandGenerator
    {
        internal static string CreateUninstallString(RegistryKey app, string matchBy, string[] exclusions)
        {
            var uninstallString = string.Empty;
            var hasUninstallString = !String.IsNullOrEmpty(app.GetValueNames().ToList().Find(x => x == "UninstallString"));
            var hasAppName = !String.IsNullOrEmpty(app.GetValueNames().ToList().Find(x => x == "DisplayName"));

            if (hasUninstallString && hasAppName)
            {
                var appName = app.GetValue("DisplayName").ToString();
                var isExclusion = exclusions != null ? exclusions.Any(p => appName.Contains(p)) : false;

                if (!isExclusion && (appName.Contains(matchBy) || String.IsNullOrEmpty(matchBy)))
                {
                    var registryUnintallString = app.GetValue("UninstallString").ToString();
                    var type = appName.Contains("Configurator") ? "partial" : "complete";                    
                    uninstallString = AddArgumentsToUninstallString(registryUnintallString, type, appName);
                }
            }

            return uninstallString;
        }        

        private static string AddArgumentsToUninstallString(string uninstall, string type, string appName)
        {
            if (type == "wmic")
            {
                return "wmic product where name='" + appName + "'" + " call uninstall";
            }

            string[] parts = uninstall.Split('{');

            string uninstallString = "MsiExec.exe ";
            string arguments = string.Empty;

            foreach (string part in parts)
            {
                if (part.EndsWith("}"))
                {
                    string guid = "{" + part;

                    if (type == "complete")
                    {
                        arguments = "RT_REMOVEALL=1 " + "/x " + guid + " /qn " + "/l \"" + "u-" + appName + ".log\"";
                    }
                    else
                    {
                        arguments = "/x " + guid + " /qn";
                    }
                }
            }

            if (arguments != string.Empty)
            {
                uninstallString += arguments;
            }
            else
            {
                uninstallString = string.Empty;
            }

            return uninstallString;
        }


        public static List<RegistryKey> GetAllRingtailKeys()
        {
            var base64keys = GetAllRingtailKeysInBaseKey(RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64));
            var base32keys = GetAllRingtailKeysInBaseKey(RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32));

            var ret = new List<RegistryKey>();
            ret.AddRange(base64keys.Values);
            ret.AddRange(base32keys.Values);
            return ret;
        }

        private static Dictionary<string, RegistryKey> GetAllRingtailKeysInBaseKey(RegistryKey baseRegistryKey)
        {
            string installKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
            RegistryKey uninstallKey = baseRegistryKey.OpenSubKey(installKey);

            List<string> allApplications = uninstallKey.GetSubKeyNames().ToList();
            var applicationNamesByKey = new Dictionary<string, RegistryKey>();

            foreach (var x in allApplications)
            {
                var subKey = baseRegistryKey.OpenSubKey(installKey + "\\" + x); ;
                string name = (string)subKey.GetValue("DisplayName");
                string publisher = (string)subKey.GetValue("Publisher");
                string version = (string)subKey.GetValue("DisplayVersion");

                if (!String.IsNullOrEmpty(name) && !String.IsNullOrEmpty(publisher))
                {
                    if (publisher.ToUpper().Contains("FTI"))
                    {
                        applicationNamesByKey.Add(x, subKey);
                    }
                }
            }

            return applicationNamesByKey;
        }

    }
}
