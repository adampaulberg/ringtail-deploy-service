using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeployToIIS.App
{
    public class ConfigHelper
    {
        public static void TryApplyCredentialsToOptions(List<string> volitileData, Options options)
        {
            string applicationName = options.AppName;
            var appConfigs = volitileData.FindAll(x => x.StartsWith(applicationName));


            //Ringtail-Svc-ContentSearch|Username="DOMAIN\user"
            //Ringtail-Svc-ContentSearch|Password="PASSWORD"
            //Ringtail-Svc-ContentSearch|Version="1"
            var userKeyValue = appConfigs.Find(x => x.Contains(applicationName + "|SERVICEUSERNAME="));
            var pwdKeyValue = appConfigs.Find(x => x.Contains(applicationName + "|SERVICEPASSWORD="));

            var user = "";
            var pwd = "";

            bool includeUserPassword = false;
            if (!String.IsNullOrEmpty(userKeyValue))
            {
                includeUserPassword = true;
            }

            if (!String.IsNullOrEmpty(pwdKeyValue))
            {
                includeUserPassword = true;
            }

            if (!includeUserPassword)
            {
                Console.WriteLine(" Did not find a service username or password for " + " " + applicationName + "|SERVICEPASSWORD=" + " will deploy with ApplicationPoolIdentity.");
            }


            try
            {
                if (!String.IsNullOrEmpty(userKeyValue))
                {
                    user = userKeyValue.Split('=')[1];
                    user = user.Substring(1, user.Length - 2);

                    options.Username = user;
                }

                if (!String.IsNullOrEmpty(pwdKeyValue))
                {
                    pwd = pwdKeyValue.Split('=')[1];
                    pwd = pwd.Substring(1, pwd.Length - 2);

                    options.Password = pwd;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("...error reading configurations: " + ex.Message);
                throw ex;
            }
        }
    }
}
