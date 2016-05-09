using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using UninstallerHelper.Util;
using System.Text;
using System.Threading.Tasks;

namespace UninstallerHelper.App
{

    public class RegistryHelper
    {
        public string RPF_Version { get; private set; }
        public string RPF_Worker_Version { get; private set; }
        public string Ringtail_Application_Version { get; private set; }
        public string Ringtail_DatabaseTools_Version { get; private set; }

        public Dictionary<string, RegistryKey> RingtailRegistryKeysByApplicationName { get; private set; }

        //public RegistryHelper()
        //{
        //    var ringtailRegistryKeys = RegistryHelper.GetAllRingtailKeys();
        //    RingtailRegistryKeysByApplicationName = new Dictionary<string, RegistryKey>();

        //    foreach (var x in ringtailRegistryKeys)
        //    {
        //        string currentName = (string)x.GetValue("DisplayName");
        //        this.RingtailRegistryKeysByApplicationName.Add(currentName, x);

        //        if (currentName.Contains("Ringtail Processing Framework"))
        //        {
        //            RPF_Version = (string)x.GetValue("DisplayVersion");
        //        }
        //        if (currentName.Contains("Framework Workers"))
        //        {
        //            RPF_Worker_Version = (string)x.GetValue("DisplayVersion");
        //        }
        //        if (currentName.Contains("Ringtail Application"))
        //        {
        //            Ringtail_Application_Version = (string)x.GetValue("DisplayVersion");
        //        }
        //        if (currentName.Contains("Ringtail Database Utility"))
        //        {
        //            Ringtail_DatabaseTools_Version = (string)x.GetValue("DisplayVersion");
        //        }
        //    }
        //}

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
}
