using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using UninstallerHelper.Util;
using System.Text;
using System.Threading.Tasks;

namespace UninstallerHelper.App
{
    public class UninstallCommandGenerator
    {

        internal static string CreateUninstallString(Logger l, RegistryFacade app, string matchBy, string[] exclusions)
        {
            var uninstallString = string.Empty;
            var appName = app.AppName;

            if (app.Ok)
            {
                var altAppName = RemoveWhiteSpaceFromString(appName);
                var isExclusion = exclusions != null ? exclusions.Any(p => appName == p) : false;

                l.AddToLog("  Creating uninstall string for:");
                l.AddToLog("        " + "app:" + appName + "|alt:" + altAppName + "|" + isExclusion.ToString());

                if (!isExclusion)
                {
                    string truncated = appName.Split('-')[0].TrimEnd();
                    isExclusion = exclusions != null ? exclusions.Any(p => truncated == p) : false;
                    l.AddToLog("        " + "after truncatedAppName == p|" + isExclusion.ToString() + "|" + truncated);

                    if (!isExclusion)
                    {
                        truncated = RemoveWhiteSpaceFromString(truncated);
                        isExclusion = exclusions != null ? exclusions.Any(p => truncated == p) : false;
                        l.AddToLog("        " + "after truncatedAppName == p|" + isExclusion.ToString() + "|" + truncated);
                    }
                }

                if (!isExclusion)
                {
                    isExclusion = exclusions != null ? exclusions.Any(p => altAppName == p) : false;
                    l.AddToLog("        " + "after altAppName == p|" + isExclusion.ToString());
                }
                if (!isExclusion)
                {
                    if (altAppName.StartsWith("Ringtail"))
                    {
                        altAppName = altAppName.Replace("Ringtail", "RingtailLegal");
                    }
                    isExclusion = exclusions != null ? exclusions.Any(p => altAppName == p) : false;
                    l.AddToLog("        " + "after altAppName == p|altAppName:" + altAppName + "|" + isExclusion.ToString());
                }


                if (!isExclusion && (appName.Contains(matchBy) || String.IsNullOrEmpty(matchBy)))
                {
                    var registryUnintallString = app.UninstallString;
                    var type = appName.Contains("Configurator") ? "partial" : "complete";
                    uninstallString = AddArgumentsToUninstallString(registryUnintallString, type, appName);
                }

                if (!isExclusion)
                {
                    l.AddToLog(" going to uninstall: " + appName);
                }
            }

            l.AddAndWrite("  finished creating uninstall string for " + appName);
            //l.AddAndWrite("  created uninstall string: " + uninstallString);

            return uninstallString;
        }

        private static string RemoveWhiteSpaceFromString(string str)
        {
            return str.Replace(" ", string.Empty);
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
