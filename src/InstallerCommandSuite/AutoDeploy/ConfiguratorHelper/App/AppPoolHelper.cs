using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Web.Administration;
using System.DirectoryServices;
using Microsoft.Win32;

namespace ConfiguratorHelper.App
{
    public class AppPoolHelper
    {
        /// <summary>
        /// Finds out of the app pool you send it already exists, and returns an emptry string if it does exist, otherwise, sends back what you sent it.
        /// </summary>
        /// <param name="applicationPoolName"></param>
        /// <returns></returns>
        public static string GetNewApplicationPoolName(string applicationPoolName)
        {
            return AppPoolAlreadyExists(applicationPoolName) ? string.Empty : applicationPoolName;
        }

        public static int GetIisMajorVersion()
        {
            int majorVersion = 8;

            try
            {
                using (RegistryKey componentsKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\InetStp", false))
                {
                    if (componentsKey != null)
                    {
                        majorVersion = (int)componentsKey.GetValue("MajorVersion", -1);
                    }
                }
            }
            catch
            {
                majorVersion = 8;
            }

            return majorVersion;
        }

        public static bool AppPoolAlreadyExists(string name)
        {
            int iisVersion = GetIisMajorVersion();
            bool result = iisVersion >= 8 ? DoesApplicationPoolExist_IIS8_OrHigher(name) : DoesApplicationPoolExist_IIS7_Or_Lower(name);
            return result;
        }

        private static bool DoesApplicationPoolExist_IIS8_OrHigher(string name)
        {
            try
            {
                ServerManager serverManager = new ServerManager();
                ApplicationPoolCollection applicationPoolCollection = serverManager.ApplicationPools;

                foreach (ApplicationPool applicationPool in applicationPoolCollection)
                {
                    if (applicationPool.Name == name)
                        return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private static bool DoesApplicationPoolExist_IIS7_Or_Lower(string name)
        {
            try
            {
                DirectoryEntries appPools =
                new DirectoryEntry("IIS://localhost/W3SVC/AppPools").Children;

                foreach (DirectoryEntry appPool in appPools)
                {
                    if (appPool.Name == name)
                        return true;
                }
            }
            catch
            {
                return DoesApplicationPoolExist_IIS8_OrHigher(name);
            }

            return false;
        }


    }
}
