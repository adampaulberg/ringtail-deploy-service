using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using UninstallerHelper.Util;
using System.Text;
using System.Threading.Tasks;

namespace UninstallerHelper.App
{

    /// <summary>
    /// Functions for reading the Windows System Registry.   ...it's hard coded for use with FTI for now.
    /// </summary>
    public class RegistryHelper
    {
        public string RPF_Version { get; private set; }
        public string RPF_Worker_Version { get; private set; }
        public string Ringtail_Application_Version { get; private set; }
        public string Ringtail_DatabaseTools_Version { get; private set; }

        public Dictionary<string, RegistryKey> RingtailRegistryKeysByApplicationName { get; private set; }


        public static List<RegistryKey> GetAllRingtailKeys(Logger l)
        {
            var base64keys = GetAllRingtailKeysInBaseKey(RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64), l);
            var base32keys = GetAllRingtailKeysInBaseKey(RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32), l);

            var ret = new List<RegistryKey>();
            ret.AddRange(base64keys.Values);
            ret.AddRange(base32keys.Values);
            return ret;
        }

        private static Dictionary<string, RegistryKey> GetAllRingtailKeysInBaseKey(RegistryKey baseRegistryKey, Logger l)
        {
            string installKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
            RegistryKey uninstallKey = baseRegistryKey.OpenSubKey(installKey);

            List<string> allApplications = uninstallKey.GetSubKeyNames().ToList();
            var applicationNamesByKey = new Dictionary<string, RegistryKey>();

            l.AddAndWrite("  Found existing installed applications: ");

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
                        l.AddAndWrite("    " + name + "|" + publisher + "|" + version + "|" + x);
                        applicationNamesByKey.Add(x, subKey);
                    }
                }
            }

            return applicationNamesByKey;
        }
    }

    /// <summary>
    /// This decouples querying the Registry object from other things you might want to do with that information.
    /// </summary>
    public class RegistryFacade
    {
        public string AppName { get; private set; }
        public string UninstallString { get; private set; }

        public bool Ok { get; private set; }

        public RegistryFacade(RegistryKey app, Logger logger)
        {
            logger.AddAndWrite(" Extracting Registry Info");

            List<string> valueNames = new List<string>();
            if (app != null)
            {
                var valueNamesAsArray = app.GetValueNames();
                logger.AddAndWrite(" Extracted valueNames");
                valueNames = valueNamesAsArray.ToList();
            }
            var hasUninstallString = !String.IsNullOrEmpty(valueNames.Find(x => x == "UninstallString"));
            var hasAppName = !String.IsNullOrEmpty(valueNames.Find(x => x == "DisplayName"));

            if (hasUninstallString && hasAppName)
            {
                Ok = true;
                AppName = app.GetValue("DisplayName").ToString();
                UninstallString = app.GetValue("UninstallString").ToString();
                logger.AddAndWrite(" Extracted Registry Info");
                logger.AddAndWrite("   app Name: " + AppName);
                logger.AddAndWrite("   uninstall String: " + UninstallString);
            }
            else
            {
                logger.AddAndWrite(" This was a valid uninstallation key, which is okay");
                Ok = false;
            }

            logger.AddAndWrite(" Extracting Registry Info - Exiting");
        }
    }
}
