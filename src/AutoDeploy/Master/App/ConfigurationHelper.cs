using Master.Model;
using Master.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Master.App
{
    public class ConfigurationHelper
    {

        public static string FillInParametersForCommand(string command, KeyValueConfigDictionary lookupKeys)
        {
            var commonKeys = lookupKeys.GetCommonKeys();
            var split = command.Split('|').ToList();
            var realCommand = split[0];

            var appLookupKeyList = command.Split('~').ToList();
            string appLookupKey = string.Empty;
            if (appLookupKeyList.Count > 1)
            {
                appLookupKey = appLookupKeyList[1].Split('|')[0].ToUpper();
                realCommand = split[0].Split('~')[0];
            }

            for (int i = 1; i < split.Count; i++)
            {
                var parameter = split[i];
                var key = " ";

                if (parameter.StartsWith("-"))
                {
                    var paramSplit = parameter.Split(' ');
                    key += paramSplit[0] + " ";
                    parameter = paramSplit[1];
                }

                var parameterValue = parameter;
                lookupKeys.TryGetValueForKey(appLookupKey, parameter, out parameterValue);
                Console.WriteLine("             replaced key: " + parameter + " with value: " + parameterValue);
                realCommand += key + parameterValue;
            }

            Console.WriteLine("          reading command as: " + realCommand);

            return realCommand;
        }
    }

    public class FileBasedRoleProvider : IRoleProvider
    {
        public List<string> GetRoles()
        {
            return SimpleFileReader.Read("roles.config");
        }
    }

    public  interface IRoleProvider
    {
        List<string> GetRoles();
    }


    public class ConfigurationValidator
    {
        public static bool ValidateConfiguration(List<string> configuration, out List<string> problems)
        {
            var okay = true;
            problems = new List<string>();

            try
            {
                var configDictionary = new KeyValueConfigDictionary(configuration);
            }
            catch (Exception ex)
            {
                problems.Add("ERROR: Could not parse the configuration data.");
                problems.Add("     " + ex.Message);
                return false;
            }

            var buildPathOk = ValidateBuildRoot(configuration, problems);
            okay = okay ? buildPathOk : okay;

            var branchOk = buildPathOk && ValidateBranch(configuration, problems);
            okay = okay ? branchOk : okay;

            var rolesOk = ValidateRoles(configuration, new FileBasedRoleProvider(), problems);
            okay = okay ? rolesOk : okay;

            var networkOk = ValidateNetworkConnectivity(configuration, problems);
            okay = okay ? networkOk : okay;

            return okay;
        }

        private static bool ValidateNetworkConnectivity(List<string> configuration, List<string> problems)
        {
            var okay = true;
            Dictionary<string, bool> validatedNetworkKeys = new Dictionary<string, bool>();

            foreach (var x in configuration)
            {
                if (!x.Contains("\""))
                {
                    continue;
                }
                string value = x.Split('\"')[1];
                if (value.Contains("http"))
                {
                    var split = x.Split('/');

                    if (split.Length > 2)
                    {
                        var host = split[2];
                        if (!validatedNetworkKeys.ContainsKey(host))
                        {
                            var hostReachable = PingHost(host);
                            validatedNetworkKeys.Add(host, hostReachable);

                            if (!hostReachable)
                            {
                                //okay = false;
                                problems.Add("WARNING: Could not reach host - " + host + " from key: " + x);
                            }
                        }
                    }
                }

                if (value.Contains(@"\\"))
                {
                    var split = value.Split('\\');
                    if (split.Length > 2)
                    {
                        try
                        {
                            DirectoryInfo di = new DirectoryInfo(value);
                            if (!di.Exists)
                            {
                                problems.Add("WARNING: Could not reach path - " + value + " from key: " + x);
                            }
                        }
                        catch
                        {
                        }
                    }
                }

            }
            return okay;
        }

        private static bool TryConnectToPortal(List<string> configuration, List<string> problems)
        {
            throw new NotImplementedException();
        }  

        /// <summary>
        /// http://stackoverflow.com/questions/5152647/how-to-quickly-check-if-unc-path-is-available
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static bool QuickBestGuessAboutAccessibilityOfNetworkPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            string pathRoot = Path.GetPathRoot(path);
            if (string.IsNullOrEmpty(pathRoot)) return false;
            ProcessStartInfo pinfo = new ProcessStartInfo("net", "use");
            pinfo.CreateNoWindow = true;
            pinfo.RedirectStandardOutput = true;
            pinfo.UseShellExecute = false;
            string output;
            using (Process p = Process.Start(pinfo))
            {
                output = p.StandardOutput.ReadToEnd();
            }
            foreach (string line in output.Split('\n'))
            {
                if (line.Contains(pathRoot) && (line.Contains("OK") || line.ToLower().Contains("disconnected")))
                {
                    return true; // shareProbablyExists
                }
            }
            return false;
        }

        internal static bool PingHost(string nameOrAddress)
        {
            bool pingable = false;
            Ping pinger = new Ping();

            try
            {
                PingReply reply = pinger.Send(nameOrAddress);

                pingable = reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {
                // Discard PingExceptions and return false;
            }

            return pingable;
        }

        public static bool ValidateRoles(List<string> configuration, IRoleProvider roleProvider, List<string> problems)
        {
            //problems.Add("VERBOSE: about to check roles");
            bool okay = true;
            if (!KeyValueConfigDictionary.DoesConfigContainKey("RoleResolver|Role", configuration))
            {
                problems.Add("ERROR: There is no RoleResolver|Role key defined....");
                okay = false;
            }
            else
            {
                List<string> userRoles = KeyValueConfigDictionary.GetConfigItemsByKey("*|Role", configuration, true);
                //userRoles.ForEach(x => problems.Add("VERBOSE: Found role: " + x));

                var availableRoles = roleProvider.GetRoles();

                if (availableRoles.Count == 0)
                {
                    problems.Add("WARNING: Could not verify roles");
                }

                var justTheRolesPlease = new List<string>();

                foreach (var x in availableRoles)
                {
                    if (x.Contains(':'))
                    {
                        justTheRolesPlease.Add(x.Split(':')[0]);
                    }
                    else if (x.Contains('|'))
                    {
                        justTheRolesPlease.Add(x.Split('|')[0]);
                    }
                }

                foreach (var role in userRoles)
                {
                    if (!justTheRolesPlease.Exists(x => role.Contains(x)))
                    {
                        problems.Add("ERROR: There is no Role in the roles.config for role: " + role);
                        okay = false;
                    }
                }
            }

            return okay;
        }

        private static bool ValidateBuildRoot(List<string> configuration, List<string> problems)
        {
            bool okay = true;
            if (!KeyValueConfigDictionary.DoesConfigContainKey("Common|BUILD_FOLDER_ROOT", configuration))
            {
                problems.Add("ERROR: There is no Common|BUILD_FOLDER_ROOT key defined....");
                okay = false;
            }
            if (!KeyValueConfigDictionary.DoesConfigContainKey("Common|BUILD_FOLDER_SUFFIX", configuration))
            {
                problems.Add("ERROR: There is no Common|BUILD_FOLDER_SUFFIX key defined....");
                okay = false;
            }

            return okay;
        }

        private static bool ValidateBranch(List<string> configuration, List<string> problems)
        {
            bool okay = true;

            if (!KeyValueConfigDictionary.DoesConfigContainKey("Common|BRANCH_NAME", configuration))
            {
                problems.Add("ERROR: There is no Common|BRANCH_NAME key defined....");
                okay = false;
            }
            else
            {
                var buildFolderRoot = GetValueForConfigEntry(KeyValueConfigDictionary.GetConfigItemsByKey("Common|BUILD_FOLDER_ROOT", configuration, false)[0]).Replace("\"", "");
                var branch = GetValueForConfigEntry(KeyValueConfigDictionary.GetConfigItemsByKey("Common|BRANCH_NAME", configuration, false)[0]).Replace("\"", "");
                var path = buildFolderRoot + "\\" + branch;
                if (!Directory.Exists(path))
                {
                    problems.Add(string.Format("ERROR: The build drop folder '{0}' could not be reached....", path));
                    okay = false;
                }
            }


            return okay;
        }

        public static string GetValueForConfigEntry(string configEntry)
        {
            var parts = configEntry.Split('=');
            if (parts.Length != 2)
                throw new ApplicationException("Invalid config entry, cannot read value: " + configEntry);
            return parts[1];
        }
    }

}
