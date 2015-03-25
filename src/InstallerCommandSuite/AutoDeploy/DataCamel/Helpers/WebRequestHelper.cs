using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DataCamel.Helpers
{
    public class WebRequestHelper
    {
        public static ICredentials GetWebServiceCredentials(string username, string password, string webServiceUrl, bool decrypt)
        {
            ICredentials webServiceCredentials;
            if (!string.IsNullOrEmpty(username))
            {
                // Decrypt the user credentials.
                string finalusername;
                string finalpassword;

                if (decrypt)
                {
                    finalusername = SecurityHelper.Decrypt(username).ToString();
                    finalpassword = SecurityHelper.Decrypt(password).ToString();
                }
                else
                {
                    finalusername = username;
                    finalpassword = password;

                }
                // Get the domain and username from the decryptedUsername string.
                string[] splitUsernameArray = finalusername.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                string domain;
                string splitUsername;
                if (splitUsernameArray.Length == 1)
                {
                    domain = ".";
                    splitUsername = splitUsernameArray[0];
                }
                else
                {
                    domain = splitUsernameArray[0];
                    splitUsername = splitUsernameArray[1];
                }

                // Set credentials for both basic and NTLM, to support all use cases.
                CredentialCache credentialCache = new CredentialCache();
                credentialCache.Add(new Uri(webServiceUrl), "Basic", new NetworkCredential(splitUsername, finalpassword, domain));
                credentialCache.Add(new Uri(webServiceUrl), "Negotiate", new NetworkCredential(splitUsername, finalpassword, domain));
                webServiceCredentials = credentialCache;
            }

            else
            {
                // No username, so set default credentials.
                webServiceCredentials = CredentialCache.DefaultCredentials;
            }

            return webServiceCredentials;
        }
    }
}
