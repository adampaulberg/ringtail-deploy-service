using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfiguratorHelper.App
{
    public class RegistryHelper
    {
        public static string GetCBVersion()
        {
            var base32keys = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey("SOFTWARE");
            var subKey = base32keys.OpenSubKey("Ringtail Solutions\\CodeBase\\Casebook\\1.0.1\\ApplicationOptions\\cbversion");
            return (string)subKey.GetValue("value");
        }

        public static string GetDBVersion()
        {
            var base32keys = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey("SOFTWARE");
            var subKey = base32keys.OpenSubKey("Ringtail Solutions\\CodeBase\\Casebook\\1.0.1");
            return (string) subKey.GetValue("currentDBModel");
        }
    }

}
