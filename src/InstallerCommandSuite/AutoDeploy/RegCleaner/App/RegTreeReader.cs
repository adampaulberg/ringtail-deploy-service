using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegistryReader.App
{

    public class RegistryReaderHelper
    {
        public static RegTree GetKeys(string match)
        {
            return new RegTree();
        }

        public List<RegTree> GetAllKeysInBaseKey()
        {
            var base64keys = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey("SOFTWARE");
            var base32keys = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey("SOFTWARE");

            var ret = new List<RegTree>();
            ret.AddRange(GetAllKeysInBaseKeyRecursive(base64keys));
            ret.AddRange(GetAllKeysInBaseKeyRecursive(base32keys));
            return ret;
        }

        public List<RegTree> GetAllKeysInBaseKeyRecursive(RegistryKey currentKey)
        {
            List<string> allApplications = currentKey.GetSubKeyNames().ToList();
            List<RegTree> masterKeys = new List<RegTree>();

            foreach (var x in allApplications)
            {
                RegistryKey subKey = currentKey.OpenSubKey(x);

                if (currentKey.Name.Contains("CodeBase") || currentKey.Name.Contains("caseportal7"))
                {
                    break;
                }

                if (subKey.Name.Contains("Ringtail") || subKey.Name.Contains("FTI"))
                {
                    if (subKey.Name.Contains("CodeBase") || subKey.Name.Contains("caseportal7"))
                    {
                        break;
                    }

                    RegTree rt = new RegTree();
                    rt.RegPath = subKey.Name;
                    rt.KeyValues = new Dictionary<string, string>();
                    foreach (var valueKey in subKey.GetValueNames())
                    {
                        rt.KeyValues.Add(valueKey, (string)subKey.GetValue(valueKey));
                    }

                    masterKeys.Add(rt);
                    masterKeys.AddRange(GetAllKeysInBaseKeyRecursive(subKey));
                }
            }


            return masterKeys;
        }

        private static List<string> GetAllKeysInBaseKey(RegistryKey baseRegistryKey)
        {
            string installKey = "SOFTWARE";
            RegistryKey keys = baseRegistryKey.OpenSubKey(installKey, true);

            List<string> allApplications = keys.GetSubKeyNames().ToList();
            //var applicationNamesByKey = new Dictionary<string, RegistryKey>();

            List<RegistryKey> newKeys = new List<RegistryKey>();

            var filtered = allApplications.FindAll(x => x.Contains("Ringtail"));

            //newKeys.AddRange(GetSubKeys(keys));
            return filtered;
        }

        private static List<RegistryKey> GetSubKeys(RegistryKey key)
        {
            List<RegistryKey> keys = new List<RegistryKey>();
            foreach (var x in key.GetSubKeyNames())
            {
                var subKey = key.OpenSubKey(x, true);
                keys.Add(subKey);

                keys.AddRange(GetSubKeys(subKey));
            }
            return keys;
        }
    }

    public class RegTree
    {
        public string RegPath { get; set; }
        public Dictionary<string, string> KeyValues { get; set; }
    }
}
