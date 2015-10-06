using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace InstallerService.Helpers
{
    public class SecurityModel
    {
        public bool UseSecurity { get; set; }
        public bool UseSSL { get; set; }
        public HttpClientCredentialType SecurityMode { get; set; }

        public static SecurityModel Create(Dictionary<string, string> envConfig)
        {
            var securityEnabledConfig = GetValueOrDefault(envConfig, EnvironmentInfo.KeySecurityEnabled);
            var securityModeConfig = GetValueOrDefault(envConfig, EnvironmentInfo.KeySecurityMode);
            var sslEnabledConfig = GetValueOrDefault(envConfig, EnvironmentInfo.KeySSLEnabled);

            var securityEnabled = securityEnabledConfig == "true" || securityEnabledConfig == "True";
            if (!securityEnabled)
                return new SecurityModel() { UseSecurity = false };

            var securityMode = GetSecurityMode(securityModeConfig);
            var useSSL = GetUseSSL(sslEnabledConfig);

            return new SecurityModel() { UseSecurity = true, SecurityMode = securityMode, UseSSL = useSSL };
        }

        static string GetValueOrDefault(Dictionary<string, string> dictionary, string key)
        {
            return dictionary.ContainsKey(key) ? dictionary[key] : null;
        }

        static HttpClientCredentialType GetSecurityMode(string securityModeConfig)
        {
            HttpClientCredentialType result;
            if (Enum.TryParse<HttpClientCredentialType>(securityModeConfig, out result))
                return result;
            else
                return HttpClientCredentialType.Basic; // defualt to basic for backwards compatibility
        }

        static bool GetUseSSL(string sslEnabledConfig)
        {
            bool result;
            if (bool.TryParse(sslEnabledConfig, out result))
                return result;
            else
                return true;    // default to true for backwards compatibility
        }
    }
}
